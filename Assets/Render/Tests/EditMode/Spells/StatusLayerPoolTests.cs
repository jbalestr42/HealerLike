using System.Collections.Generic;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class StatusLayerPoolTests
    {
        readonly List<Object> _created = new List<Object>();
        GameObject _host;
        GameObject _target;
        SpellLooks _looks;
        EffectVocabulary _vocabulary;
        StatusPool _pool;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Layer pool");
            _target = new GameObject("Layer holder");
            _looks = ScriptableObject.CreateInstance<SpellLooks>();
            _created.Add(_looks);
            _vocabulary = RenderTestAssets.LoadEffectVocabulary();
            _pool = new StatusPool();
            _pool.Init(_host.transform, _vocabulary, _looks, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial());
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Clear();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_target);
            foreach (Object obj in _created) if (obj) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        BuffHandlerFactory Compound()
        {
            BuffHandlerFactory positive = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
            BuffHandlerFactory negative = SpellSinkFixture.Modifier(AttributeType.AttackRate, 1f, _created);
            positive.data.buffFactoryList.Add(negative.data.buffFactoryList[0]);
            return positive;
        }

        [Test]
        public void CompoundHandler_OpensEveryPolarityAndRemovesEveryLayerTogether()
        {
            BuffHandlerFactory factory = Compound();
            _pool.Set(null, _target, factory, 2, 0.5f, 5f);
            Assert.AreEqual(2, _pool.count);
            SpellEffect boon = _pool.Get(_target, EffectKey.Orbit);
            SpellEffect bane = _pool.Get(_target, EffectKey.Press);
            Assert.IsNotNull(boon);
            Assert.IsNotNull(bane);
            Assert.AreEqual(2, boon.stacks);
            Assert.AreEqual(2, bane.stacks);
            Assert.AreEqual(0.5f, boon.elapsedSeconds);
            Assert.AreEqual(0.5f, bane.elapsedSeconds);
            _pool.Set(null, _target, factory, 3, 1f, 5f);
            Assert.AreEqual(2, _pool.count);
            Assert.AreEqual(3, bane.stacks);
            _pool.Remove(_target, factory);
            _pool.Remove(_target, factory);
            Assert.AreEqual(0, _pool.count);
            Assert.IsNull(_pool.Get(_target, factory));
            boon.Advance(1f);
            bane.Advance(1f);
            Assert.IsTrue(boon.removalComplete);
            Assert.IsTrue(bane.removalComplete);
        }

        [Test]
        public void EquivalentLayer_SharesStacksWhileItsOtherLayerRemainsIndependent()
        {
            BuffHandlerFactory compound = Compound();
            BuffHandlerFactory single = SpellSinkFixture.Modifier(AttributeType.Damage, 3f, _created);
            _pool.Set(null, _target, compound, 2, 0f, 5f);
            _pool.Set(null, _target, single, 1, 0f, 5f);
            Assert.AreEqual(2, _pool.count);
            Assert.AreSame(_pool.Get(_target, compound), _pool.Get(_target, single));
            Assert.AreEqual(3, _pool.Get(_target, single).stacks);
            _pool.Remove(_target, compound);
            Assert.AreEqual(1, _pool.count);
            Assert.AreEqual(1, _pool.Get(_target, single).stacks);
            Assert.IsNull(_pool.Get(_target, EffectKey.Press));
        }

        [TestCase(0.4f)]
        [TestCase(2f)]
        public void IndependentPeriodicClocks_DoNotShareTheirAnimationOrRemoval(float secondPeriod)
        {
            BuffHandlerFactory fast = SpellSinkFixture.Consumer(2f, 0.4f, _created);
            BuffHandlerFactory slow = SpellSinkFixture.Consumer(2f, secondPeriod, _created);
            _pool.Set(null, _target, fast, 1, 0.2f, 4f);
            _pool.Set(null, _target, slow, 2, 1.2f, 8f);
            SpellEffect first = _pool.Get(_target, fast);
            SpellEffect second = _pool.Get(_target, slow);
            Assert.AreEqual(2, _pool.count);
            Assert.AreNotSame(first, second);
            Assert.AreEqual(0.4f, first.recipe.cycleSeconds);
            Assert.AreEqual(secondPeriod, second.recipe.cycleSeconds);
            Assert.AreEqual(0.2f, first.elapsedSeconds);
            Assert.AreEqual(1.2f, second.elapsedSeconds);
            _pool.Remove(_target, fast);
            Assert.AreEqual(1, _pool.count);
            Assert.AreEqual(1.2f, second.elapsedSeconds);
            Assert.AreEqual(8f, second.durationSeconds);
        }

        [Test]
        public void DifferentMagnitudes_DoNotLoseTheLargerAuthoredScale()
        {
            BuffHandlerFactory light = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
            BuffHandlerFactory heavy = SpellSinkFixture.Modifier(AttributeType.Damage, 80f, _created);
            _pool.Set(null, _target, light, 1, 0f, 5f);
            _pool.Set(null, _target, heavy, 1, 0f, 5f);
            Assert.AreEqual(2, _pool.count);
            Assert.Greater(_pool.Get(_target, heavy).recipe.scale, _pool.Get(_target, light).recipe.scale);
        }

        [Test]
        public void WholeRecipeOverride_ReplacesBuffLayersAndKeepsAnOwnedSnapshot()
        {
            BuffHandlerFactory factory = Compound();
            EffectRecipeAsset asset = ScriptableObject.CreateInstance<EffectRecipeAsset>();
            _created.Add(asset);
            asset.recipe = EffectComposer.Compose(_vocabulary, EffectKey.Orbit, EffectFamily.Boon,
                EffectTempo.ForDuration, 0f, 1, 0f, 0f);
            asset.recipe.additions = new[] { EffectComposer.Compose(_vocabulary, EffectKey.Plates,
                EffectFamily.Boon, EffectTempo.ForDuration, 0f, 1, 0f, 0f) };
            int authoredCount = asset.recipe.count;
            _looks.buffs[factory] = new SpellLook { recipe = asset };
            _pool.Set(null, _target, factory, 3, 1f, 8f);
            SpellEffect effect = _pool.Get(_target, factory);
            Assert.AreEqual(1, _pool.count);
            Assert.AreEqual(2, effect.GetComponentsInChildren<SpellEffect>(true).Length);
            Assert.IsNull(_pool.Get(_target, EffectKey.Press));
            Assert.AreNotSame(asset.recipe, effect.recipe);
            Assert.AreNotSame(asset.recipe.entry, effect.recipe.entry);
            Assert.AreNotSame(asset.recipe.additions[0].entry, effect.recipe.additions[0].entry);
            Assert.AreEqual(authoredCount, asset.recipe.count);
            foreach (SpellEffect layer in effect.GetComponentsInChildren<SpellEffect>(true))
            {
                Assert.AreEqual(3, layer.stacks);
                Assert.AreEqual(1f, layer.elapsedSeconds);
            }
            _pool.Remove(_target, factory);
            effect.Advance(1f);
            foreach (SpellEffect layer in effect.GetComponentsInChildren<SpellEffect>(true))
                Assert.IsTrue(layer.removalComplete);
        }

        [Test]
        public void DestroyedFactory_ReleasesEveryLayerWithoutDictionaryErrors()
        {
            BuffHandlerFactory factory = Compound();
            _pool.Set(null, _target, factory, 1, 0f, 5f);
            Object.DestroyImmediate(factory);
            Assert.DoesNotThrow(() => _pool.Tick());
            Assert.AreEqual(0, _pool.count);
            Assert.DoesNotThrow(() => _pool.Tick());
        }

        [Test]
        public void DestroyedTarget_ReleasesEveryLayerWithoutDictionaryErrors()
        {
            BuffHandlerFactory factory = Compound();
            _pool.Set(null, _target, factory, 1, 0f, 5f);
            Object.DestroyImmediate(_target);
            Assert.DoesNotThrow(() => _pool.Tick());
            Assert.AreEqual(0, _pool.count);
            Assert.DoesNotThrow(() => _pool.Tick());
        }
    }
}
