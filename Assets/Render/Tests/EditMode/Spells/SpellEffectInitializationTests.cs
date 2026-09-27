using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public class SpellEffectInitializationTests
    {
        GameObject _owner;
        SpellEffect _effect;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("EffectInitialization");
            _effect = _owner.AddComponent<SpellEffect>();
        }

        [TearDown]
        public void TearDown() => SpellEffect.Dispose(_owner);

        static EffectRecipe Recipe() => new EffectRecipe
        {
            count = 1, colour = Color.white,
            entry = new ElementEntry
            {
                parts = new[] { new LookPart { id = "Shape", primitive = Primitive.Sphere,
                    shape = ShapeProfile.Bulb(), colour = ColourRole.Accent, size = Vector3.one } },
                cycleSeconds = 1.25f
            }
        };

        [Test]
        public void Init_InvalidEntryOrCyclicRecipeAllocatesNoParts()
        {
            EffectRecipe recipe = Recipe();
            recipe.additions = new[] { recipe };
            TestHelpers.WithLoggingDisabled(() =>
                _effect.Init(recipe, RenderTestAssets.LoadMeshes(), null, LookSide.Plant));
            Assert.IsNull(_effect.recipe);
            Assert.AreEqual(0, _owner.transform.childCount);
            recipe.additions = null;
            recipe.entry = null;
            TestHelpers.WithLoggingDisabled(() =>
                _effect.Init(recipe, RenderTestAssets.LoadMeshes(), null, LookSide.Plant));
            Assert.IsNull(_effect.recipe);
            Assert.AreEqual(0, _owner.transform.childCount);
        }

        [Test]
        public void Init_UnspecifiedCycleUsesEntryWithoutMutatingSharedRecipe()
        {
            EffectRecipe recipe = Recipe();
            _effect.Init(recipe, RenderTestAssets.LoadMeshes(), null, LookSide.Plant);
            Assert.AreEqual(1.25f, _effect.recipe.cycleSeconds);
            Assert.AreEqual(1.25f, _effect.lifetime);
            Assert.AreEqual(0f, recipe.cycleSeconds);
            Assert.AreNotSame(recipe, _effect.recipe);
            _effect.Advance(.2f);
            Assert.IsTrue(_effect.shapes[0].gameObject.activeSelf);
        }

        [Test]
        public void Init_RepeatedCallKeepsExistingGeometryStateAndOwnership()
        {
            _effect.Init(Recipe(), RenderTestAssets.LoadMeshes(), null, LookSide.Plant);
            _effect.SetStatus(3, .4f, 8f);
            Mesh owned = _effect.shapes[0].GetComponent<MeshFilter>().sharedMesh;
            EffectRecipe accepted = _effect.recipe;
            TestHelpers.WithLoggingDisabled(() =>
                _effect.Init(Recipe(), RenderTestAssets.LoadMeshes(), null, LookSide.Plant));
            Assert.AreSame(accepted, _effect.recipe);
            Assert.AreEqual(1, _effect.parts.Count);
            Assert.AreEqual(1, _owner.transform.childCount);
            Assert.AreSame(owned, _effect.shapes[0].GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual(3, _effect.stacks);
            Assert.AreEqual(.4f, _effect.elapsedSeconds);
            SpellEffect.Dispose(_owner);
            Assert.IsTrue(owned == null);
        }
    }
}
