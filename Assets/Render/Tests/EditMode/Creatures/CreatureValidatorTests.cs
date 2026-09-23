using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureValidatorTests
    {
        public static CreatureRecipe Recipe()
        {
            CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
            recipe.parts = new Part[]
            {
                new Part { id = "HLBody", parent = -1, dimensions = Vector3.one, colour = Color.green }
            };
            recipe.sourceLocal = new Vector3[] { Vector3.up };
            recipe.arms = new ArmDefinition[]
            {
                new ArmDefinition
                {
                    bodyPart = 0,
                    segmentCount = 24,
                    segmentLength = 0.2f,
                    radius = 0.018f,
                    restJoints = ChainSolverTests.Rest(),
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
            CreatureRecipe recipe = Recipe();
            try
            {
                recipe.roots.count = count;
                Assert.AreEqual(valid, CreatureValidator.TryValidate(recipe, out _));
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
            CreatureRecipe recipe = Recipe();
            try
            {
                Part body = recipe.parts[0];
                recipe.parts = new Part[count];
                for (int i = 0; i < count; i++)
                {
                    recipe.parts[i] = body;
                    recipe.parts[i].id = "HLPart" + i;
                    recipe.parts[i].parent = i == 0 ? -1 : 0;
                }

                Assert.AreEqual(valid, CreatureValidator.TryValidate(recipe, out _));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void ValidTreeAndRestAreAccepted()
        {
            CreatureRecipe recipe = Recipe();
            try
            {
                Assert.IsTrue(CreatureValidator.TryValidate(recipe, out string error), error);
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
            CreatureRecipe recipe = Recipe();
            try
            {
                if (mode == 0)
                {
                    recipe.parts[0].parent = 0;
                }

                if (mode == 1)
                {
                    recipe.parts = new Part[] { recipe.parts[0], recipe.parts[0] };
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

                Assert.IsFalse(CreatureValidator.TryValidate(recipe, out _));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }
    }
}
