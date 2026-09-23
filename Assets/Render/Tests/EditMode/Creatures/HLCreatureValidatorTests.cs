using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLCreatureValidatorTests
    {
        public static HLCreatureRecipe Recipe()
        {
            HLCreatureRecipe recipe = ScriptableObject.CreateInstance<HLCreatureRecipe>();
            recipe.parts = new HLPart[]
            {
                new HLPart { id = "HLBody", parent = -1, dimensions = Vector3.one, colour = Color.green }
            };
            recipe.sourceLocal = new Vector3[] { Vector3.up };
            recipe.arms = new HLArmDefinition[]
            {
                new HLArmDefinition
                {
                    bodyPart = 0,
                    segmentCount = 24,
                    segmentLength = 0.2f,
                    radius = 0.018f,
                    restJoints = HLChainSolverTests.Rest(),
                    bendPole = Vector3.up,
                    colour = Color.green
                }
            };
            return recipe;
        }

        [TestCase(6, true)]
        [TestCase(7, true)]
        [TestCase(8, true)]
        [TestCase(9, false)]
        public void RootCrownSupportsEightLegsWithinCell(int count, bool valid)
        {
            HLCreatureRecipe recipe = Recipe();
            try
            {
                recipe.roots.count = count;
                Assert.AreEqual(valid, HLCreatureValidator.TryValidate(recipe, out _));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }

        [TestCase(40, true)]
        [TestCase(41, false)]
        public void JointDetailStillHasBoundedPartBudget(int count, bool valid)
        {
            HLCreatureRecipe recipe = Recipe();
            try
            {
                HLPart body = recipe.parts[0];
                recipe.parts = new HLPart[count];
                for (int i = 0; i < count; i++)
                {
                    recipe.parts[i] = body;
                    recipe.parts[i].id = "HLPart" + i;
                    recipe.parts[i].parent = i == 0 ? -1 : 0;
                }

                Assert.AreEqual(valid, HLCreatureValidator.TryValidate(recipe, out _));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void ValidTreeAndRestAreAccepted()
        {
            HLCreatureRecipe recipe = Recipe();
            try
            {
                Assert.IsTrue(HLCreatureValidator.TryValidate(recipe, out string error), error);
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void InvalidTreeSocketAndRestAreRejected(int mode)
        {
            HLCreatureRecipe recipe = Recipe();
            try
            {
                if (mode == 0)
                {
                    recipe.parts[0].parent = 0;
                }

                if (mode == 1)
                {
                    recipe.parts = new HLPart[] { recipe.parts[0], recipe.parts[0] };
                }

                if (mode == 2)
                {
                    recipe.sourceLocal = new Vector3[0];
                }

                if (mode == 3)
                {
                    recipe.arms[0].restJoints[2] = Vector3.one * 100f;
                }

                if (mode == 4)
                {
                    recipe.roots.footRadius = 1f;
                }

                Assert.IsFalse(HLCreatureValidator.TryValidate(recipe, out _));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }
    }
}
