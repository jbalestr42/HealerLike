using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class EffectRecipeAssetTests
    {
        EffectRecipeAsset _asset;
        GameObject _host;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<EffectRecipeAsset>();
            _asset.recipe = Recipe(0.4f, 0.15f);
            _host = new GameObject("Whole composition fixture");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_asset);
        }

        static LookPart[] Parts(string id) => new[]
        {
            new LookPart { id = id, primitive = Primitive.Sphere, role = PartRole.Body,
                colour = ColourRole.Accent, size = Vector3.one * 0.2f }
        };

        static EffectRecipe Recipe(float cycle, float release)
        {
            return new EffectRecipe
            {
                element = EffectKey.Orbit, motion = EffectMotionKind.Orbit,
                socket = EffectSocket.Body, family = EffectFamily.Boon, tempo = EffectTempo.Once,
                cycleSeconds = cycle, colour = Color.yellow, count = 1, scale = 1f,
                palette = RenderTestAssets.LoadPalette(),
                entry = new ElementEntry
                {
                    parts = Parts("core"), stackBeads = Parts("bead"),
                    criticalRings = Parts("critical"), sideRim = Parts("rim"),
                    cycleSeconds = cycle, motion = EffectMotionKind.Orbit, socket = EffectSocket.Body,
                    presentation = new EffectPresentation { enabled = true, releaseSeconds = release },
                    ground = new GroundEffect { kick = 0.2f, light = 0.3f }
                }
            };
        }

        static void AssertOwnedEntry(ElementEntry authored, ElementEntry instance)
        {
            Assert.AreNotSame(authored, instance);
            Assert.AreNotSame(authored.parts, instance.parts);
            Assert.AreNotSame(authored.stackBeads, instance.stackBeads);
            Assert.AreNotSame(authored.criticalRings, instance.criticalRings);
            Assert.AreNotSame(authored.sideRim, instance.sideRim);
            Assert.AreNotSame(authored.presentation, instance.presentation);
            Assert.AreNotSame(authored.ground, instance.ground);
            CollectionAssert.AreEqual(authored.parts, instance.parts);
        }

        [Test]
        public void InstantiateRecipe_DeepCopiesEveryMutableLayerAndKeepsSharedAssetReferences()
        {
            EffectRecipe child = Recipe(0.8f, 0.3f);
            _asset.recipe.additions = new[] { child };
            EffectRecipe copy = _asset.InstantiateRecipe();
            Assert.IsNotNull(copy);
            Assert.AreNotSame(_asset.recipe, copy);
            Assert.AreNotSame(_asset.recipe.additions, copy.additions);
            Assert.AreNotSame(child, copy.additions[0]);
            AssertOwnedEntry(_asset.recipe.entry, copy.entry);
            AssertOwnedEntry(child.entry, copy.additions[0].entry);
            Assert.AreSame(_asset.recipe.palette, copy.palette);
            copy.entry.parts[0].size = Vector3.one * 4f;
            copy.entry.stackBeads[0].position = Vector3.one;
            copy.entry.criticalRings[0].glow = 5f;
            copy.entry.sideRim[0].euler = Vector3.up * 90f;
            copy.entry.presentation.scale = 3f;
            copy.entry.ground.light = -0.7f;
            copy.additions[0].entry.presentation.releaseSeconds = 2f;
            copy.additions[0].entry.ground.kick = 4f;
            Assert.AreEqual(Vector3.one * 0.2f, _asset.recipe.entry.parts[0].size);
            Assert.AreEqual(Vector3.zero, _asset.recipe.entry.stackBeads[0].position);
            Assert.AreEqual(0f, _asset.recipe.entry.criticalRings[0].glow);
            Assert.AreEqual(Vector3.zero, _asset.recipe.entry.sideRim[0].euler);
            Assert.AreEqual(1f, _asset.recipe.entry.presentation.scale);
            Assert.AreEqual(0.3f, _asset.recipe.entry.ground.light);
            Assert.AreEqual(0.3f, child.entry.presentation.releaseSeconds);
            Assert.AreEqual(0.2f, child.entry.ground.kick);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InstantiateRecipe_CyclicCompositionsAreRejectedBeforeCopying(bool indirect)
        {
            EffectRecipe root = _asset.recipe;
            if (indirect)
            {
                EffectRecipe child = Recipe(0.5f, 0.2f);
                root.additions = new[] { child };
                child.additions = new[] { root };
            }
            else
            {
                root.additions = new[] { root };
            }

            Assert.IsFalse(EffectValidator.TryValidate(root, out string error));
            StringAssert.Contains("cycle", error);
            Assert.IsNull(_asset.InstantiateRecipe());
        }

        [Test]
        public void InstantiateRecipe_SharedChildIsLegalAndEachOccurrenceOwnsItsSnapshot()
        {
            EffectRecipe child = Recipe(0.6f, 0.3f);
            _asset.recipe.additions = new[] { child, child };
            EffectRecipe copy = _asset.InstantiateRecipe();
            Assert.IsNotNull(copy);
            Assert.AreNotSame(copy.additions[0], copy.additions[1]);
            Assert.AreNotSame(copy.additions[0].entry.parts, copy.additions[1].entry.parts);
            copy.additions[0].entry.parts[0].size = Vector3.one;
            Assert.AreEqual(Vector3.one * 0.2f, copy.additions[1].entry.parts[0].size);
            Assert.AreEqual(Vector3.one * 0.2f, child.entry.parts[0].size);
        }

        [Test]
        public void CompositeLifecycle_LongestNestedChildControlsLifetimeAndRemovalTail()
        {
            EffectRecipe child = Recipe(0.7f, 0.35f);
            child.additions = new[] { Recipe(1.4f, 0.8f) };
            _asset.recipe.additions = new[] { child };
            SpellEffect effect = _host.AddComponent<SpellEffect>();
            effect.Init(_asset.InstantiateRecipe(), RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), LookSide.Plant);
            SpellEffect[] layers = effect.GetComponentsInChildren<SpellEffect>(true);
            Assert.AreEqual(3, layers.Length);
            Assert.AreEqual(1.4f, effect.lifetime);
            Assert.IsFalse(layers[1].enabled, "Only the owner advances child clocks.");
            Assert.IsFalse(layers[2].enabled);
            effect.SetStatus(2, 0f, 6f);
            effect.BeginRemoval();
            effect.Advance(0.2f);
            Assert.IsFalse(effect.removalComplete, "The parent finished, but its children are still resolving.");
            effect.Advance(0.2f);
            Assert.IsFalse(effect.removalComplete, "A nested child still owns its longer tail.");
            effect.Advance(0.41f);
            foreach (SpellEffect layer in layers)
            {
                Assert.IsTrue(layer.removalComplete);
            }
        }
    }
}
