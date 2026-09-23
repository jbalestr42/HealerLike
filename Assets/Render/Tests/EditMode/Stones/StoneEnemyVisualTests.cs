using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneEnemyVisualTests
{
    class MotionSource : IStoneMotionSource
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

    static StoneEffects CreateEffects()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
        return Object.Instantiate(prefab).GetComponent<StoneEffects>();
    }

    static StoneEnemyVisual CreateVisual(GameObject target)
    {
        Transform pivot = new GameObject("BodyPivot").transform;
        pivot.SetParent(target.transform, false);
        Transform presentation = new GameObject("StonePresentation").transform;
        presentation.SetParent(pivot, false);
        StoneEnemyVisual visual = target.AddComponent<StoneEnemyVisual>();
        TestHelpers.SetPrivateField(visual, "_bodyPivot", pivot);
        TestHelpers.SetPrivateField(visual, "_presentation", presentation);
        return visual;
    }

    GameObject _target;
    GameObject _source;
    GameObject _projectile;
    GameObject _fxObject;
    ResourceAttribute _health;
    StoneEnemyVisual _visual;
    StoneEffects _fx;

    int visibleCount { get { return _visual.parts.Count(part => part.transform.gameObject.activeSelf); } }

    [SetUp]
    public void SetUp()
    {
        _target = new GameObject("Target");
        _source = new GameObject("Source");
        _projectile = new GameObject("Projectile");
        _health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
        TestHelpers.CreateAttributeManager(_source);
        _fx = CreateEffects();
        _fxObject = _fx.gameObject;
        _visual = CreateVisual(_target);
        _visual.Init(_health, 15, _fx);
    }

    [TearDown]
    public void TearDown()
    {
        if (_visual != null)
        {
            TestHelpers.InvokePrivate(_visual, "OnDestroy");
        }
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_projectile);
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
    public void LateUpdate_LookAtTargetAtInit_LetsItDriveThePivot()
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
    public void CompleteHealthBatch_HalfHealth_ShedsOncePartAndSurvivorsStay()
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
    public void OnEnable_AfterShedding_RestoresPresentationWithoutShedPart()
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
    public void CompleteHealthBatch_NetZeroOrMaxOnlyChange_DoesNotShed()
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
    public void RecordImpact_MatchingModifier_EmitsAtContactAndZeroDamageOnlyDust()
    {
        ResourceModifier modifier = Queue(-1);
        Vector3 point = new Vector3(23f, 7f, 4f);
        _visual.RecordImpact(modifier, new StoneImpact(point, Vector3.up, Vector3.zero, false));
        Drain();
        Assert.AreEqual(0, _visual.pendingImpactCount);
        Assert.AreEqual(14, _fx.liveCount);
        foreach (MeshFilter filter in _fxObject.GetComponentsInChildren<MeshFilter>())
        {
            Vector3 expected = filter.sharedMesh.name == "Pyramid" ? point + Vector3.up * 0.005f : point;
            Assert.That(Vector3.Distance(filter.transform.position, expected), Is.LessThan(0.00001));
        }

        _fx.Advance(1f);
        ResourceModifier zero = Queue(0);
        _visual.RecordImpact(zero, new StoneImpact(point, Vector3.up, Vector3.zero, false));
        Drain();
        Assert.AreEqual(0, _visual.pendingImpactCount);
        Assert.AreEqual(5, _fx.liveCount);
    }

    [Test]
    public void Init_AfterExpiryDisableAndReenable_KeepsOneListener()
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
    public void CompleteHealthBatch_Lethal_CollapsesOnceAndDebrisOutlivesOwner()
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
    public void OnDestroy_DisabledLivingVisual_EmitsNoDeath()
    {
        _visual.enabled = false;
        TestHelpers.InvokePrivate(_visual, "OnDestroy");
        Object.DestroyImmediate(_target);
        _target = null;
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void AdvancePresentation_AimAnticipationAndLaunch_RotateOnlyThePresentation()
    {
        _visual.Init(_health, 17, _fx);
        Transform pivot = _visual.parts[0].transform.parent;

        _visual.AdvancePresentation(Vector3.forward, 1f, 1f);
        Assert.Greater((pivot.rotation * Vector3.up).z, 0);

        _visual.AdvancePresentation(Vector3.forward, 0f, 1f);
        Assert.Less((pivot.rotation * Vector3.up).z, -0.2f);

        Assert.True(_visual.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.forward * 5f));
        Assert.Greater((pivot.rotation * Vector3.up).z, 0.3f);
        Assert.AreEqual(Quaternion.identity, _target.transform.rotation);
        Assert.AreEqual(Quaternion.identity, pivot.parent.localRotation);
        Assert.AreEqual(Vector3.zero, _target.transform.position);
    }

    [TestCase(StonePreset.Cairn)]
    [TestCase(StonePreset.Monolith)]
    public void AdvancePresentation_RigidPreset_IgnoresTargetCooldownAndLaunch(StonePreset preset)
    {
        TestHelpers.SetPrivateField(_visual, "_preset", preset);
        _visual.Init(_health, 17, _fx);
        Transform pivot = _visual.parts[0].transform.parent;
        _visual.AdvancePresentation(Vector3.forward, 0f, 0.2f);
        Quaternion first = pivot.localRotation;

        _visual.Init(_health, 17, _fx);
        _visual.AdvancePresentation(Vector3.left, 1f, 0.2f);
        Assert.AreEqual(first, pivot.localRotation);

        _visual.BeginDelivery(1, DeliveryStyle.Direct, _projectile.transform, Vector3.back);
        Assert.AreEqual(first, pivot.localRotation);

        _visual.AdvancePresentation(Vector3.forward, 0f, 10f);
        Assert.Less(Quaternion.Angle(Quaternion.identity, pivot.localRotation), 0.001f);
    }

    [TestCase(DeliveryStyle.Thrown)]
    [TestCase(DeliveryStyle.Direct)]
    [TestCase(DeliveryStyle.Rigid)]
    public void ContactDelivery_SupportedStyle_FollowsTheProjectileAndCleansUp(DeliveryStyle style)
    {
        _visual.Init(_health, 17, _fx);
        Assert.True(_visual.BeginDelivery(1, style, _projectile.transform, Vector3.forward));
        Assert.False(_visual.BeginDelivery(1, style, _projectile.transform, Vector3.forward));

        _projectile.transform.position = new Vector3(4f, 3f, 2f);
        TestHelpers.InvokePrivate(_visual, "LateUpdate");
        Transform shard = _fxObject.GetComponentInChildren<MeshFilter>().transform;
        Assert.AreEqual(_projectile.transform.position, shard.position);

        _visual.ContactDelivery(1, Vector3.one * 7f, null);
        Assert.AreEqual(0, _visual.liveDeliveryCount);
        Assert.That(_fx.liveCount, Is.InRange(8, 10));
        foreach (MeshFilter filter in _fxObject.GetComponentsInChildren<MeshFilter>())
        {
            Assert.AreEqual(Vector3.one * 7f, filter.transform.position);
        }

        int count = _fx.liveCount;
        _visual.ContactDelivery(1, Vector3.zero, null);
        _visual.EndDelivery(1);
        Assert.AreEqual(count, _fx.liveCount);
    }

    [Test]
    public void BeginDelivery_UnsupportedStylesThenDisable_LeaksNothing()
    {
        _visual.Init(_health, 17, _fx);
        DeliveryStyle[] unsupported =
        {
            DeliveryStyle.Arc, DeliveryStyle.Swarm, DeliveryStyle.Bounce, DeliveryStyle.ChainSync
        };
        foreach (DeliveryStyle style in unsupported)
        {
            Assert.False(_visual.BeginDelivery(1, style, _projectile.transform, Vector3.one));
        }

        _visual.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        _visual.BeginDelivery(2, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        _visual.enabled = false;
        TestHelpers.InvokePrivate(_visual, "OnDisable");

        Assert.AreEqual(0, _visual.liveDeliveryCount);
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void LateUpdate_LateAddedSkill_PollsItsCooldownAndTheFirstTarget()
    {
        Entity owner = null;
        TargetProvider provider = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            owner = _target.AddComponent<Entity>();
            provider = _target.AddComponent<TargetProvider>();
        });
        TestHelpers.SetPrivateField(owner, "_health", _health);
        TestHelpers.SetPrivateField(provider, "_targets", new List<GameObject> { _projectile });
        _projectile.transform.position = Vector3.forward * 8f;
        _visual.Init(owner, (StoneEffects)null);

        // Entity creates skills after model Init. Verify they are discovered on a later poll.
        ShootProjectileSkill skill = _target.AddComponent<ShootProjectileSkill>();
        TestHelpers.SetPrivateField(skill, "_cooldownDuration", new Attribute(1));
        skill.isEnabled = true;
        BindingFlags privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        System.Type cooldownSkill = typeof(ACooldownSkill<ShootProjectileSkillData>);
        FieldInfo cooldown = cooldownSkill.GetField("_cooldown", privateInstance);
        cooldown.SetValue(skill, 1f);
        TestHelpers.InvokePrivate(_visual, "LateUpdate");
        MethodInfo read = typeof(StoneEnemyVisual).GetMethod("ReadCooldown", privateInstance);
        Assert.AreEqual(1f, (float)read.Invoke(_visual, null));

        System.Func<float> poll = (System.Func<float>)System.Delegate.CreateDelegate(typeof(System.Func<float>),
            _visual, read);
        poll();
        _visual.CompleteHealthBatch();
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            poll();
            _visual.CompleteHealthBatch();
        }
        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);

        cooldown.SetValue(skill, 0.05f);
        Assert.AreEqual(0.05f, (float)read.Invoke(_visual, null));

        skill.isEnabled = false;
        Assert.True(float.IsNaN((float)read.Invoke(_visual, null)));

        skill.isEnabled = true;
        TestHelpers.SetPrivateField(owner, "_isDraggable", true);
        Assert.True(float.IsNaN((float)read.Invoke(_visual, null)));
    }

    [Test]
    public void LateUpdate_DestroyedProjectile_ReleasesTheDeliveryWithoutContact()
    {
        _visual.Init(_health, 17, _fx);
        _visual.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        Object.DestroyImmediate(_projectile);
        TestHelpers.InvokePrivate(_visual, "LateUpdate");

        Assert.AreEqual(0, _visual.liveDeliveryCount);
        Assert.AreEqual(0, _fx.liveCount);
    }
}

}
