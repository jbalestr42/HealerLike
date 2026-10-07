using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class StatusMorphologyTests
    {
        readonly List<Object> _created = new List<Object>();
        GameObject _host;
        GameObject _target;
        StatusPool _pool;
        FakeEffectAnchors _anchors;
        BuffHandlerFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("StatusMorphologyHost");
            _target = new GameObject("StatusMorphologyTarget");
            _anchors = _target.AddComponent<FakeEffectAnchors>();
            _anchors.anchors = new EffectAnchors
            {
                bodyRadius = 0.3f, bodyCentre = Vector3.up * 0.3f, headRadius = 0.15f,
                headCentre = Vector3.up * 0.8f, neck = Vector3.up * 0.6f, foot = Vector3.zero
            };
            _pool = new StatusPool();
            _pool.Init(_host.transform, RenderTestAssets.LoadEffectVocabulary(),
                AssetDatabase.LoadAssetAtPath<SpellLooks>(SpellSinkFixture.LooksPath),
                RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadLookMaterial());
            _factory = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Clear();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_target);
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }
        }

        static float Age(SpellEffect effect) => (float)typeof(SpellEffect).GetField("_age",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(effect);

        [Test]
        public void Tick_MorphologyChangeRefitsExistingStatusWithoutResettingState()
        {
            _pool.Set(null, _target, _factory, 3, 0.75f, 8f);
            SpellEffect effect = _pool.Get(_target, _factory);
            Vector3 oldScale = effect.transform.localScale;
            effect.Advance(0.2f);
            float age = Age(effect);
            _anchors.anchors.bodyRadius *= 2f;
            _anchors.anchors.headRadius *= 2f;
            _anchors.anchors.headCentre += Vector3.up * 0.5f;
            _pool.Tick();
            Assert.AreSame(effect, _pool.Get(_target, _factory));
            Assert.Greater(effect.transform.localScale.magnitude, oldScale.magnitude * 1.5f);
            Assert.AreEqual(3, effect.stacks);
            Assert.AreEqual(0.75f, effect.elapsedSeconds);
            Assert.AreEqual(8f, effect.durationSeconds);
            Assert.AreEqual(age, Age(effect));
        }

        [Test]
        public void Tick_TranslationDoesNotRedoPlacement()
        {
            _pool.Set(null, _target, _factory, 2, 0.5f, 8f);
            SpellEffect effect = _pool.Get(_target, _factory);
            // A sentinel offset is lost if the expensive placement pass runs again.
            effect.transform.localPosition += Vector3.right * 0.13f;
            Vector3 local = effect.transform.localPosition;
            Vector3 move = Vector3.right * 5f;
            _target.transform.position += move;
            _anchors.anchors.bodyCentre += move;
            _anchors.anchors.headCentre += move;
            _anchors.anchors.neck += move;
            _anchors.anchors.foot += move;
            _anchors.anchors.castPoint += move;
            _pool.Tick();
            Assert.That(Vector3.Distance(local, effect.transform.localPosition), Is.LessThan(0.0001f));
            Assert.AreEqual(2, effect.stacks);
        }
    }
}
