using System;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures.Editor.Studio.Tests
{
    public class CreatureStudioAuthoringTests
    {
        CreatureRecipe recipe;
        [SetUp] public void SetUp() { recipe = ScriptableObject.CreateInstance<CreatureRecipe>(); }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(recipe); }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void BuildSample_ProducesValidIndependentEditableRecipe(int index)
        {
            CreatureRecipe a = CreatureStudioAuthoring.BuildSample(index);
            CreatureRecipe b = CreatureStudioAuthoring.BuildSample(index);
            try
            {
                Assert.NotNull(a);
                Assert.NotNull(b);
                Assert.IsEmpty(CreatureStudioAuthoring.Validate(a));
                Assert.AreEqual(CreatureStudioAuthoring.SampleNames[index], a.name);
                Assert.AreEqual(HideFlags.None, a.hideFlags);
                Assert.AreNotSame(a.parts, b.parts);
                Vector3 before = b.parts[0].dimensions;
                a.parts[0].dimensions = Vector3.one * 12f;
                Assert.AreEqual(before, b.parts[0].dimensions);
                if (index == 2) Assert.AreEqual(0, a.roots.count);
                else Assert.Greater(a.arms.Length, 0);
            }
            finally
            {
                if (a != null) UnityEngine.Object.DestroyImmediate(a);
                if (b != null) UnityEngine.Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void Clone_DeepCopiesArmJointArraysAndAllRecipeFields()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            recipe.arms = new[] { Arm(0) };
            recipe.sourceLocal = new[] { Vector3.up };
            recipe.neckLocal = Vector3.one;
            recipe.wiltColour = Color.yellow;
            recipe.stoneOchre = Color.red;
            CreatureRecipe copy = CreatureStudioAuthoring.Clone(recipe);
            try
            {
                Assert.AreEqual(recipe.neckLocal, copy.neckLocal);
                Assert.AreEqual(recipe.wiltColour, copy.wiltColour);
                Assert.AreEqual(recipe.stoneOchre, copy.stoneOchre);
                Assert.AreEqual(recipe.idle, copy.idle);
                Assert.AreEqual(recipe.roots, copy.roots);
                copy.arms[0].restJoints[1] = Vector3.forward;
                copy.sourceLocal[0] = Vector3.zero;
                copy.parts[0].colour = Color.black;
                Assert.AreEqual(Vector3.up, recipe.arms[0].restJoints[1]);
                Assert.AreEqual(Vector3.up, recipe.sourceLocal[0]);
                Assert.AreNotEqual(Color.black, recipe.parts[0].colour);
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        [Test]
        public void AddPart_InitialRootThenChildrenHaveUniqueIdsAndEarlierParents()
        {
            Assert.AreEqual(0, CreatureStudioAuthoring.AddPart(recipe));
            Assert.AreEqual(-1, recipe.parts[0].parent);
            Assert.AreEqual(1, CreatureStudioAuthoring.AddPart(recipe, 0));
            Assert.AreEqual(2, CreatureStudioAuthoring.AddPart(recipe, 1));
            Assert.AreEqual(1, recipe.parts[2].parent);
            Assert.AreNotEqual(recipe.parts[0].id, recipe.parts[1].id);
            Assert.AreNotEqual(recipe.parts[1].id, recipe.parts[2].id);
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
        }

        [Test]
        public void AddPart_InvalidInputsAndPartLimitDoNotMutate()
        {
            Assert.AreEqual(-1, CreatureStudioAuthoring.AddPart(null));
            Assert.AreEqual(-1, CreatureStudioAuthoring.AddPart(recipe, 0, (Primitive)999));
            CreatureStudioAuthoring.AddPart(recipe);
            Assert.AreEqual(-1, CreatureStudioAuthoring.AddPart(recipe, 5));
            while (recipe.parts.Length < CreatureValidator.MaxParts) CreatureStudioAuthoring.AddPart(recipe);
            CreaturePart[] before = recipe.parts;
            Assert.AreEqual(-1, CreatureStudioAuthoring.AddPart(recipe));
            Assert.AreEqual(-1, CreatureStudioAuthoring.DuplicatePart(recipe, 0));
            Assert.AreSame(before, recipe.parts);
        }

        [Test]
        public void DuplicatePart_PreservesParentSettingsAndDoesNotDuplicateArms()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            CreatureStudioAuthoring.AddPart(recipe, 0, Primitive.Leaf);
            recipe.parts[1].localPosition = Vector3.up;
            recipe.parts[1].glow = 1f;
            recipe.arms = new[] { Arm(1) };
            recipe.sourceLocal = new[] { Vector3.up };
            Assert.AreEqual(2, CreatureStudioAuthoring.DuplicatePart(recipe, 1));
            Assert.AreEqual(0, recipe.parts[2].parent);
            Assert.AreEqual(Primitive.Leaf, recipe.parts[2].primitive);
            Assert.AreEqual(1f, recipe.parts[2].glow);
            Assert.AreEqual(Vector3.up + Vector3.right * .1f, recipe.parts[2].localPosition);
            Assert.AreNotEqual(recipe.parts[1].id, recipe.parts[2].id);
            Assert.AreEqual(1, recipe.arms.Length);
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
        }

        [Test]
        public void DuplicatePart_RootCopyBecomesChildInsteadOfSecondRoot()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            Assert.AreEqual(1, CreatureStudioAuthoring.DuplicatePart(recipe, 0));
            Assert.AreEqual(0, recipe.parts[1].parent);
            Assert.AreEqual(Vector3.right * .1f, recipe.parts[1].localPosition);
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
        }

        [Test]
        public void RemovePart_RemovesWholeSubtreeAndRemapsSurvivingArmSocketPairs()
        {
            CreatureStudioAuthoring.AddPart(recipe);       // 0 root
            CreatureStudioAuthoring.AddPart(recipe, 0);    // 1 removed
            CreatureStudioAuthoring.AddPart(recipe, 0);    // 2 survives as 1
            CreatureStudioAuthoring.AddPart(recipe, 1);    // 3 removed child
            CreatureStudioAuthoring.AddPart(recipe, 2);    // 4 survives as 2, parent 1
            recipe.arms = new[] { Arm(3), Arm(4), Arm(0) };
            recipe.sourceLocal = new[] { Vector3.left, Vector3.right, Vector3.up, Vector3.forward };
            string survivor = recipe.parts[4].id;
            Assert.IsTrue(CreatureStudioAuthoring.RemovePart(recipe, 1));
            Assert.AreEqual(3, recipe.parts.Length);
            Assert.AreEqual(survivor, recipe.parts[2].id);
            Assert.AreEqual(1, recipe.parts[2].parent);
            Assert.AreEqual(2, recipe.arms.Length);
            Assert.AreEqual(2, recipe.arms[0].bodyPart);
            Assert.AreEqual(0, recipe.arms[1].bodyPart);
            CollectionAssert.AreEqual(new[] { Vector3.right, Vector3.up, Vector3.forward }, recipe.sourceLocal);
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
        }

        [Test]
        public void RemovePart_RootInvalidIndexAndMalformedHierarchyAreRejectedWithoutMutation()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            CreatureStudioAuthoring.AddPart(recipe);
            CreaturePart[] before = recipe.parts;
            Assert.IsFalse(CreatureStudioAuthoring.RemovePart(recipe, 0));
            Assert.IsFalse(CreatureStudioAuthoring.RemovePart(recipe, 5));
            Assert.IsFalse(CreatureStudioAuthoring.RemovePart(null, 1));
            recipe.parts[1].parent = 1;
            Assert.IsFalse(CreatureStudioAuthoring.RemovePart(recipe, 1));
            Assert.AreSame(before, recipe.parts);
        }

        [Test]
        public void Validate_ReportsRuntimeStructureAndNonfiniteExtraFields()
        {
            Assert.IsNotEmpty(CreatureStudioAuthoring.Validate(null));
            Assert.IsNotEmpty(CreatureStudioAuthoring.Validate(recipe));
            CreatureStudioAuthoring.AddPart(recipe);
            recipe.neckLocal = new Vector3(float.NaN, 0f, 0f);
            recipe.parts[0].role = (PartRole)999;
            recipe.stoneOchre = new Color(float.PositiveInfinity, 0f, 0f);
            Assert.AreEqual(3, CreatureStudioAuthoring.Validate(recipe).Length);
        }

        [Test]
        public void Clone_NullInputAndMissingArraysAreHandled()
        {
            Assert.IsNull(CreatureStudioAuthoring.Clone(null));
            recipe.parts = null; recipe.arms = null; recipe.sourceLocal = null;
            CreatureRecipe copy = CreatureStudioAuthoring.Clone(recipe);
            try { Assert.IsEmpty(copy.parts); Assert.IsEmpty(copy.arms); Assert.IsEmpty(copy.sourceLocal); }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
            Assert.Throws<ArgumentOutOfRangeException>(() => CreatureStudioAuthoring.BuildSample(3));
        }

        [Test]
        public void AddArm_CreatesValidLinksAndInsertsSourceBeforeExtraSockets()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            CreatureStudioAuthoring.AddPart(recipe, 0);
            recipe.parts[0].localEuler = new Vector3(0f, 0f, 90f);
            recipe.sourceLocal = new[] { Vector3.forward };
            Assert.AreEqual(0, CreatureStudioAuthoring.AddArm(recipe, 1));
            Assert.AreEqual(1, recipe.arms[0].bodyPart);
            Assert.AreEqual(4, recipe.arms[0].restJoints.Length);
            Vector3 expected = recipe.parts[0].localPosition + Quaternion.Euler(recipe.parts[0].localEuler) *
                (recipe.parts[1].localPosition + Vector3.up * .1f);
            Assert.Less(Vector3.Distance(expected, recipe.sourceLocal[0]), .00001f);
            Assert.AreEqual(Vector3.forward, recipe.sourceLocal[1]);
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
        }

        [Test]
        public void AddArm_StopsAtRigLimitAndRejectsInvalidPart()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            Assert.AreEqual(-1, CreatureStudioAuthoring.AddArm(recipe, 4));
            for (int i = 0; i < HealerLike.Render.Deliveries.ArmPool.MaxArms; i++) Assert.AreEqual(i, CreatureStudioAuthoring.AddArm(recipe, 0));
            Assert.AreEqual(-1, CreatureStudioAuthoring.AddArm(recipe, 0));
            Assert.AreEqual(HealerLike.Render.Deliveries.ArmPool.MaxArms, recipe.arms.Length);
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
        }

        [Test]
        public void RemoveArm_RemovesPairedSourceAndPreservesOthers()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            CreatureStudioAuthoring.AddArm(recipe, 0);
            CreatureStudioAuthoring.AddArm(recipe, 0);
            recipe.sourceLocal = new[] { Vector3.left, Vector3.right, Vector3.forward };
            recipe.arms[1].radius = .025f;
            Assert.IsTrue(CreatureStudioAuthoring.RemoveArm(recipe, 0));
            Assert.AreEqual(.025f, recipe.arms[0].radius);
            CollectionAssert.AreEqual(new[] { Vector3.right, Vector3.forward }, recipe.sourceLocal);
            Assert.IsFalse(CreatureStudioAuthoring.RemoveArm(recipe, 5));
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
        }

        [TestCase(6, .27f, 6, .27f)]
        [TestCase(1000, 9f, 128, 1f)]
        [TestCase(-1, -1f, 2, .001f)]
        [TestCase(9, float.NaN, 9, .15f)]
        public void RebuildArmRestPose_RepairsCountLengthAndPreservesAuthoredSettings(int count, float length,
            int expectedCount, float expectedLength)
        {
            CreatureStudioAuthoring.AddPart(recipe);
            CreatureStudioAuthoring.AddArm(recipe, 0);
            recipe.arms[0].segmentCount = count;
            recipe.arms[0].segmentLength = length;
            recipe.arms[0].colour = Color.magenta;
            recipe.arms[0].rootLocal = Vector3.forward;
            Assert.IsTrue(CreatureStudioAuthoring.RebuildArmRestPose(recipe, 0));
            ArmDefinition arm = recipe.arms[0];
            Assert.AreEqual(expectedCount, arm.segmentCount);
            Assert.AreEqual(expectedLength, arm.segmentLength);
            Assert.AreEqual(expectedCount + 1, arm.restJoints.Length);
            Assert.AreEqual(Vector3.zero, arm.restJoints[0]);
            Assert.AreEqual(Color.magenta, arm.colour);
            Assert.AreEqual(Vector3.forward, arm.rootLocal);
            for (int i = 1; i < arm.restJoints.Length; i++)
                Assert.AreEqual(expectedLength, Vector3.Distance(arm.restJoints[i - 1], arm.restJoints[i]), .00001f);
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
            Assert.IsFalse(CreatureStudioAuthoring.RebuildArmRestPose(recipe, 9));
        }

        [Test]
        public void NewPartsAndArmsInheritAuthoredColoursInsteadOfHardcodedPaletteValues()
        {
            CreatureStudioAuthoring.AddPart(recipe);
            recipe.parts[0].colour = new Color(.1f,.3f,.8f);
            int child = CreatureStudioAuthoring.AddPart(recipe,0);
            Assert.AreEqual(recipe.parts[0].colour,recipe.parts[child].colour);
            recipe.parts[child].role = PartRole.Head;
            recipe.parts[child].colour = new Color(.9f,.1f,.6f);
            int arm = CreatureStudioAuthoring.AddArm(recipe,0);
            Assert.AreEqual(recipe.parts[child].colour,recipe.arms[arm].tipColour);
            Assert.AreEqual(recipe.roots.colour,recipe.arms[arm].colour);
        }

        static ArmDefinition Arm(int bodyPart) => new ArmDefinition
        {
            bodyPart = bodyPart, segmentCount = 2, segmentLength = 1f, radius = .03f,
            restJoints = new[] { Vector3.zero, Vector3.up, Vector3.up * 2f },
            bendPole = Vector3.forward, colour = Color.green
        };
    }
}
