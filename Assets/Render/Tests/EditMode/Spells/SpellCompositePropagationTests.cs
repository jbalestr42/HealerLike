using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class SpellCompositePropagationTests
    {
        GameObject _host;
        GameObject _source;
        readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Composite ownership");
            _source = new GameObject("Live source");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_source);
            foreach (Object obj in _created) if (obj) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        static EffectRecipe Recipe(EffectCount count = EffectCount.Fixed)
        {
            var parts = new LookPart[4];
            for (int i = 0; i < parts.Length; i++)
                parts[i] = new LookPart { id = "part" + i, primitive = Primitive.Sphere,
                    role = PartRole.Body, size = Vector3.one * .1f, position = Vector3.right * i * .2f };
            var entry = new ElementEntry { parts = parts, count = count, cycleSeconds = 1f,
                motion = EffectMotionKind.Orbit, socket = EffectSocket.Body,
                presentation = new EffectPresentation { avoidHead = false } };
            return new EffectRecipe { entry = entry, element = EffectElement.Orbit,
                motion = entry.motion, socket = entry.socket, tempo = EffectTempo.ForDuration,
                family = EffectFamily.Boon, cycleSeconds = 1f, count = 1, colour = Color.white };
        }

        [Test]
        public void Clear_RemovalTailWithNestedChildren_DoesNotReadDestroyedComponents()
        {
            SpellVisualSink sink = _host.AddComponent<SpellVisualSink>();
            EffectRecipe recipe = Recipe();
            recipe.additions = new[] { Recipe() };
            recipe.additions[0].additions = new[] { Recipe() };
            SpellEffect tail = SpellEffect.Create(recipe, _host.transform, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), null);
            tail.BeginRemoval();
            Assert.AreEqual(3, _host.GetComponentsInChildren<SpellEffect>().Length);
            Assert.DoesNotThrow(() => sink.Clear());
            Assert.AreEqual(0, _host.GetComponentsInChildren<SpellEffect>(true).Length);
            Assert.DoesNotThrow(() => sink.Clear());
        }

        [Test]
        public void StatusRefresh_AppliesEachNestedLayersOwnCountPolicy()
        {
            SpellLooks looks = ScriptableObject.CreateInstance<SpellLooks>();
            EffectRecipeAsset asset = ScriptableObject.CreateInstance<EffectRecipeAsset>();
            _created.Add(looks);
            _created.Add(asset);
            BuffHandlerFactory factory = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
            asset.recipe = Recipe();
            asset.recipe.additions = new[] { Recipe(EffectCount.Stacks) };
            asset.recipe.additions[0].additions = new[] { Recipe(EffectCount.Charges) };
            looks.buffs[factory] = new SpellLook { recipe = asset };
            var pool = new StatusPool();
            pool.Init(_host.transform, RenderTestAssets.LoadEffectVocabulary(), looks,
                RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadLookMaterial());
            try
            {
                pool.Set(null, _source, factory, 3, 0f, 5f);
                SpellEffect effect = pool.Get(_source, factory);
                SpellEffect[] layers = effect.GetComponentsInChildren<SpellEffect>(true);
                Assert.AreEqual(4, layers[0].count);
                Assert.AreEqual(3, layers[1].count);
                Assert.AreEqual(3, layers[2].count);
                effect.RefreshCount(2, 4f);
                Assert.AreEqual(4, layers[0].count);
                Assert.AreEqual(2, layers[1].count);
                Assert.AreEqual(4, layers[2].count);
                pool.Set(null, _source, factory, 1, 1f, 5f);
                Assert.AreEqual(1, layers[1].count);
                Assert.AreEqual(1, layers[2].count);
                Assert.AreEqual(1, asset.recipe.additions[0].count);
            }
            finally { pool.Clear(); }
        }

        [Test]
        public void MovingCastSource_UpdatesEveryNestedLinkBeforeItsOwnPose()
        {
            EffectRecipe recipe = Recipe();
            EffectRecipe child = Recipe();
            recipe.socket = child.socket = EffectSocket.Link;
            recipe.additions = new[] { child };
            SpellEffect effect = SpellEffect.Create(recipe, _host.transform, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), null);
            effect.SetEndpoints(Vector3.zero, Vector3.right * 5f, false);
            effect.SetCastSource(_source);
            _source.transform.position = new Vector3(2f, 1f, 3f);
            effect.Advance(.1f);
            Vector3 expected = EffectPlacement.Anchors(_source).castPoint;
            foreach (SpellEffect layer in effect.GetComponentsInChildren<SpellEffect>(true))
                Assert.That(Vector3.Distance(expected, layer.castOrigin), Is.LessThan(.00001f));
        }
    }
}
