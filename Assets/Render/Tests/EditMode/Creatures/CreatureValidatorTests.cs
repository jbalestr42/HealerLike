using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class CreatureValidatorTests
{
    public static CreatureRecipe Recipe()
    {
        CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
        recipe.parts = new CreaturePart[]
        {
            new CreaturePart { id = "Body", parent = -1, dimensions = Vector3.one, colour = Color.green }
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

    CreatureRecipe _recipe;

    [SetUp]
    public void SetUp()
    {
        _recipe = Recipe();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_recipe);
    }

    [TestCase(0, true)]
    [TestCase(3, false)]
    [TestCase(8, true)]
    [TestCase(14, true)]
    [TestCase(15, false)]
    public void TryValidate_RootCount_AcceptsNoneOrFourToFourteen(int count, bool valid)
    {
        _recipe.roots.count = count;

        Assert.AreEqual(valid, CreatureValidator.TryValidate(_recipe, out _));
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(4, true)]
    [TestCase(5, false)]
    public void TryValidate_RootSegments_AcceptsOneToFour(int segments, bool valid)
    {
        _recipe.roots.segments = segments;

        Assert.AreEqual(valid, CreatureValidator.TryValidate(_recipe, out _));
    }

    [TestCase(0.6f, true)] // the short band, 1.1 body units
    [TestCase(1.16f, true)] // the long band, 2.1 body units
    [TestCase(1.3f, false)]
    public void TryValidate_RootReach_AcceptsEveryReachBand(float footRadius, bool valid)
    {
        _recipe.roots.footRadius = footRadius;
        _recipe.roots.thickness = 0.042f;

        Assert.AreEqual(valid, CreatureValidator.TryValidate(_recipe, out _));
    }

    [TestCase(40, true)]
    [TestCase(41, false)]
    public void TryValidate_PartCount_AcceptsUpToForty(int count, bool valid)
    {
        CreaturePart body = _recipe.parts[0];
        _recipe.parts = new CreaturePart[count];
        for (int i = 0; i < count; i++)
        {
            _recipe.parts[i] = body;
            _recipe.parts[i].id = "Part" + i;
            _recipe.parts[i].parent = i == 0 ? -1 : 0;
        }

        Assert.AreEqual(valid, CreatureValidator.TryValidate(_recipe, out _));
    }

    [Test]
    public void TryValidate_ValidTreeAndRest_ReturnsTrue()
    {
        Assert.IsTrue(CreatureValidator.TryValidate(_recipe, out string error), error);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void TryValidate_InvalidTreeSocketOrRest_ReturnsFalse(int mode)
    {
        if (mode == 0)
        {
            _recipe.parts[0].parent = 0;
        }

        if (mode == 1)
        {
            _recipe.parts = new CreaturePart[] { _recipe.parts[0], _recipe.parts[0] };
        }

        if (mode == 2)
        {
            _recipe.sourceLocal = new Vector3[0];
        }

        if (mode == 3)
        {
            _recipe.arms[0].restJoints[2] = Vector3.one * 100f;
        }

        if (mode == 4)
        {
            _recipe.roots.footRadius = CreatureValidator.MaxRootReach;
        }

        Assert.IsFalse(CreatureValidator.TryValidate(_recipe, out _));
    }
}

}
