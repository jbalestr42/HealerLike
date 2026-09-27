using System;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Spells
{
    public class EffectValidatorTests
    {
        static LookPart Part(string id = "Shape") => new LookPart
            { id = id, primitive = Primitive.Sphere, size = Vector3.one };
        static EffectRecipe Recipe() => new EffectRecipe
            { entry = new ElementEntry { parts = new[] { Part() } }, count = 1 };

        [Test]
        public void LegacyDefaults_AreValidWithoutPaletteOrPresentation()
        {
            var recipe = Recipe();
            recipe.entry.presentation = null;
            recipe.entry.stackBeads = null;
            recipe.additions = null;
            Assert.IsTrue(EffectValidator.TryValidate(recipe, out string error), error);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void AuxiliaryFragments_ValidatePartData(int fragment)
        {
            var recipe = Recipe();
            var parts = new[] { new LookPart() };
            if (fragment == 0) recipe.entry.stackBeads = parts;
            if (fragment == 1) recipe.entry.criticalRings = parts;
            if (fragment == 2) recipe.entry.sideRim = parts;
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out string error));
            StringAssert.Contains("valid spell part", error);
        }

        [Test]
        public void AggregateBudget_IncludesAuxiliaryFragmentsAndEveryCompositeOccurrence()
        {
            var recipe = Recipe();
            recipe.entry.stackBeads = new LookPart[256];
            for (int i = 0; i < 256; i++) recipe.entry.stackBeads[i] = Part("Bead" + i);
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out string error));
            StringAssert.Contains("1..256", error);
            recipe.entry.stackBeads = Array.Empty<LookPart>();
            var child = Recipe();
            recipe.additions = new EffectRecipe[255];
            for (int i = 0; i < 255; i++) recipe.additions[i] = child;
            Assert.IsTrue(EffectValidator.TryValidate(recipe, out error), error);
            recipe.additions = new EffectRecipe[256];
            for (int i = 0; i < 256; i++) recipe.additions[i] = child;
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out error));
            StringAssert.Contains("1..256", error);
        }

        [Test]
        public void CompositeCycleAndNullChild_ReturnDiagnosticsWithoutRecursing()
        {
            var recipe = Recipe();
            var child = Recipe();
            recipe.additions = new[] { child };
            child.additions = new[] { recipe };
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out string error));
            StringAssert.Contains("cycle", error);
            child.additions = new EffectRecipe[] { null };
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out error));
            StringAssert.Contains("non-null", error);
        }

        [Test]
        public void DeepComposite_IsBoundedByTheAggregateBudget()
        {
            var root = Recipe();
            var tip = root;
            for (int i = 0; i < 1000; i++)
            {
                var next = Recipe();
                tip.additions = new[] { next };
                tip = next;
            }
            Assert.IsFalse(EffectValidator.TryValidate(root, out string error));
            StringAssert.Contains("1..256", error);
        }

        [Test]
        public void AttachmentsAndPivots_UseSharedFragmentValidation()
        {
            var recipe = Recipe();
            LookPart attached = Part("Attached");
            attached.attachTo = "Missing";
            recipe.entry.sideRim = new[] { attached };
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out string error));
            StringAssert.Contains("earlier unique", error);
            attached.attachTo = null;
            attached.pivot = (ShapeAnchor)999;
            recipe.entry.sideRim[0] = attached;
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out error));
            StringAssert.Contains("anchor", error);
            attached.pivot = ShapeAnchor.Center;
            attached.attachTo = "Base";
            recipe.entry.sideRim = new[] { Part("Base"), attached };
            Assert.IsTrue(EffectValidator.TryValidate(recipe, out error), error);
        }

        [Test]
        public void RecipeData_RejectsInvalidEnumCountScaleTimingAndColour()
        {
            Action<EffectRecipe>[] invalid =
            {
                r => r.element = (EffectKey)999,
                r => r.motion = (EffectMotionKind)999,
                r => r.socket = (EffectSocket)999,
                r => r.family = (EffectFamily)999,
                r => r.tempo = (EffectTempo)999,
                r => r.count = 2,
                r => r.count = 0,
                r => r.scale = float.PositiveInfinity,
                r => r.scale = 0f,
                r => r.cycleSeconds = float.NaN,
                r => r.cycleSeconds = -1f,
                r => r.colour = new Color(float.NaN, 0, 0),
                r => r.entry.count = (EffectCount)999,
                r => r.entry.minCount = 2,
                r => r.entry.cycleSeconds = 0,
                r => r.entry.presentation.releaseSeconds = -1
            };
            foreach (var change in invalid)
            {
                var recipe = Recipe();
                change(recipe);
                Assert.IsFalse(EffectValidator.TryValidate(recipe, out string error));
                Assert.IsNotEmpty(error);
            }
        }

        [Test]
        public void GroundProfiles_RejectNonfiniteValuesAndInvalidStateBounds()
        {
            Action<GroundEffect>[] invalid =
            {
                g => g.shape = (GroundShape)999,
                g => g.kick = float.PositiveInfinity,
                g => g.fadeIn = -1,
                g => g.light = -2,
                g => g.ash = 2,
                g => g.width = 0,
                g => g.band = -1,
                g => g.shiver = float.NaN
            };
            foreach (var change in invalid)
            {
                var recipe = Recipe();
                recipe.entry.ground = new GroundEffect();
                change(recipe.entry.ground);
                Assert.IsFalse(EffectValidator.TryValidate(recipe, out string error));
                StringAssert.Contains("ground", error);
            }
            var valid = Recipe();
            valid.entry.ground = new GroundEffect { light = -1, vitality = -1, kick = -10 };
            Assert.IsTrue(EffectValidator.TryValidate(valid, out string validError), validError);
            valid.entry.groundRadius = float.NaN;
            Assert.IsFalse(EffectValidator.TryValidate(valid, out _));
            valid.entry.groundRadius = 1;
            valid.entry.groundStrength = 1.1f;
            Assert.IsFalse(EffectValidator.TryValidate(valid, out _));
        }
    }
}
