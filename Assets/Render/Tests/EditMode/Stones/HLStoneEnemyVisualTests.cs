using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneEnemyVisualTests
    {
        class MotionSource : IHLStoneMotionSource
        {
            public bool TrySample(out Vector3 velocityWS, out Quaternion facingWS)
            {
                velocityWS = Vector3.right;
                facingWS = Quaternion.Euler(0f, 90f, 0f);
                return true;
            }
        }

        class Consumer : AConsumer
        {
            readonly float _amount;

            public Consumer(float amount)
            {
                _amount = amount;
            }

            public override float GetValue()
            {
                return _amount;
            }

            public override bool ignoreDamageReduction { get { return true; } }

            public override bool ignoreConsumerPrevention { get { return false; } }
        }

        static HLStoneEffects CreateEffects()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
            return Object.Instantiate(prefab).GetComponent<HLStoneEffects>();
        }

        static HLStoneEnemyVisual CreateVisual(GameObject target)
        {
            Transform pivot = new GameObject("BodyPivot").transform;
            pivot.SetParent(target.transform, false);
            Transform presentation = new GameObject("HLStonePresentation").transform;
            presentation.SetParent(pivot, false);
            HLStoneEnemyVisual visual = target.AddComponent<HLStoneEnemyVisual>();
            TestHelpers.SetPrivateField(visual, "_bodyPivot", pivot);
            TestHelpers.SetPrivateField(visual, "_presentation", presentation);
            return visual;
        }

        GameObject _target;
        GameObject _source;
        GameObject _fxObject;
        ResourceAttribute _health;
        HLStoneEnemyVisual _visual;
        HLStoneEffects _fx;

        int visibleCount { get { return _visual.parts.Count(part => part.transform.gameObject.activeSelf); } }

        [SetUp]
        public void Setup()
        {
            _target = new GameObject("HLTarget");
            _source = new GameObject("HLSource");
            _health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
            TestHelpers.CreateAttributeManager(_source);
            _fx = CreateEffects();
            _fxObject = _fx.gameObject;
            _visual = CreateVisual(_target);
            _visual.Init(_health, 15, _fx);
        }

        [TearDown]
        public void Teardown()
        {
            if (_visual != null)
            {
                TestHelpers.InvokePrivate(_visual, "OnDestroy");
            }
            TestHelpers.InvokePrivate(_fx, "OnDestroy");
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_fxObject);
        }

        ResourceModifier Queue(float delta)
        {
            ResourceModifier modifier = new ResourceModifier { source = _source };
            modifier.consumers.Add(new Consumer(delta));
            _health.AddResourceModifier(modifier);
            return modifier;
        }

        void Drain()
        {
            TestHelpers.InvokePrivate(_health, "Update");
            _visual.CompleteHealthBatch();
        }

        [Test]
        public void MotionUsesLookAtTargetCachedDuringInitialization()
        {
            Transform pivot = _target.transform.Find("BodyPivot");
            LookAtTarget look = pivot.gameObject.AddComponent<LookAtTarget>();
            _visual.Init(_health, 15, _fx);
            TestHelpers.SetPrivateField(_visual, "_motion", new MotionSource());
            TestHelpers.InvokePrivate(_visual, "LateUpdate");
            Assert.AreEqual(Quaternion.identity, pivot.rotation);

            Object.DestroyImmediate(look);
            TestHelpers.InvokePrivate(_visual, "LateUpdate");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), pivot.rotation), 0.001f);

            // New components are picked up only on initialization, never by the moving frame path.
            pivot.gameObject.AddComponent<LookAtTarget>();
            pivot.rotation = Quaternion.identity;
            TestHelpers.InvokePrivate(_visual, "LateUpdate");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), pivot.rotation), 0.001f);

            _visual.Init(_health, 15, _fx);
            pivot.rotation = Quaternion.identity;
            TestHelpers.SetPrivateField(_visual, "_motion", new MotionSource());
            TestHelpers.InvokePrivate(_visual, "LateUpdate");
            Assert.AreEqual(Quaternion.identity, pivot.rotation);
        }

        [Test]
        public void FakeHealthShedsOnceAtFiftyAndSurvivorsNeverMove()
        {
            Matrix4x4 first = _visual.parts[0].transform.localToWorldMatrix;
            Matrix4x4 second = _visual.parts[1].transform.localToWorldMatrix;
            Queue(-49);
            Drain();
            Assert.AreEqual(3, visibleCount);

            Queue(-1);
            Drain();
            Assert.AreEqual(2, visibleCount);
            Assert.AreEqual(first, _visual.parts[0].transform.localToWorldMatrix);
            Assert.AreEqual(second, _visual.parts[1].transform.localToWorldMatrix);

            Queue(50);
            Drain();
            Queue(-70);
            Drain();
            Assert.AreEqual(2, visibleCount);

            _visual.Init(_health, 15, _fx);
            Assert.AreEqual(3, visibleCount);
        }

        [Test]
        public void ReenableRestoresPresentationWithoutResurrectingShedParts()
        {
            Queue(-60);
            Drain();
            Assert.AreEqual(2, visibleCount);

            _visual.enabled = false;
            TestHelpers.InvokePrivate(_visual, "OnDisable");
            Assert.IsFalse(_visual.parts[0].transform.gameObject.activeInHierarchy);

            _visual.enabled = true;
            TestHelpers.InvokePrivate(_visual, "OnEnable");
            Assert.AreEqual(2, visibleCount);
            Assert.IsTrue(_visual.parts[0].transform.gameObject.activeInHierarchy);
        }

        [Test]
        public void DamageAndHealingInSameBatchAndMaxOnlyChangesDoNotShed()
        {
            Queue(-80);
            Queue(80);
            Drain();
            Assert.AreEqual(3, visibleCount);

            Attribute max = _target.GetComponent<AttributeManager>().Get(AttributeType.HealthMax);
            max.BaseValue = 200;
            max.Update();
            _visual.CompleteHealthBatch();
            Assert.AreEqual(3, visibleCount);

            // No final value event is emitted when damage and healing return to the previous value.
            Queue(-160);
            Queue(160);
            Drain();
            TestHelpers.InvokePrivate(_visual, "LateUpdate");
            Assert.AreEqual(3, visibleCount);
        }

        [Test]
        public void RecordedContactConsumedOnlyByExactModifierAndZeroDamageEmitsOnlyDust()
        {
            ResourceModifier modifier = Queue(-1);
            Vector3 point = new Vector3(23f, 7f, 4f);
            _visual.RecordImpact(modifier, new HLStoneImpact(point, Vector3.up, Vector3.zero, false));
            Drain();
            Assert.AreEqual(0, _visual.pendingImpactCount);
            Assert.AreEqual(14, _fx.liveCount);
            foreach (MeshFilter filter in _fxObject.GetComponentsInChildren<MeshFilter>())
            {
                Vector3 expected = filter.sharedMesh.name == "Pyramid" ? point + Vector3.up * 0.005f : point;
                Assert.That(Vector3.Distance(filter.transform.position, expected), Is.LessThan(1e-5));
            }

            _fx.Advance(1f);
            ResourceModifier zero = Queue(0);
            _visual.RecordImpact(zero, new HLStoneImpact(point, Vector3.up, Vector3.zero, false));
            Drain();
            Assert.AreEqual(0, _visual.pendingImpactCount);
            Assert.AreEqual(5, _fx.liveCount);
        }

        [Test]
        public void ExpiryUnbindReenableAndReinitDoNotDuplicateListeners()
        {
            _visual.RecordImpact(new ResourceModifier(), default);
            TestHelpers.InvokePrivate(_visual, "LateUpdate");
            Assert.AreEqual(1, _visual.pendingImpactCount);

            TestHelpers.InvokePrivate(_visual, "LateUpdate");
            Assert.AreEqual(0, _visual.pendingImpactCount);

            _visual.enabled = false;
            TestHelpers.InvokePrivate(_visual, "OnDisable");
            _visual.RecordImpact(new ResourceModifier(), default);
            Assert.AreEqual(0, _visual.pendingImpactCount);

            _fx.Advance(1f);
            _visual.enabled = true;
            _visual.Init(_health, 15, _fx);
            Queue(-1);
            Drain();
            Assert.AreEqual(14, _fx.liveCount);
        }

        [Test]
        public void LethalBatchCollapsesOnlyOnceAndDebrisSurvivesOwner()
        {
            Queue(-100);
            Drain();
            Assert.AreEqual(0, visibleCount);
            Assert.AreEqual(31, _fx.liveCount);

            _visual.Collapse();
            Assert.AreEqual(31, _fx.liveCount);

            TestHelpers.InvokePrivate(_visual, "OnDestroy");
            Object.DestroyImmediate(_target);
            _target = null;
            Assert.AreEqual(31, _fx.liveCount);

            _fx.Advance(0.81f);
            Assert.AreEqual(0, _fx.liveCount);
        }

        [Test]
        public void DisableOrLivingRemovalDoesNotEmitDeath()
        {
            _visual.enabled = false;
            TestHelpers.InvokePrivate(_visual, "OnDestroy");
            Object.DestroyImmediate(_target);
            _target = null;
            Assert.AreEqual(0, _fx.liveCount);
        }
    }
}
