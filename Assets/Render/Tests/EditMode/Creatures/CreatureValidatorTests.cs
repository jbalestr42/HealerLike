using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Deliveries;

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

    [TestCase(0.6f, true)] // the healer's short band, 1.1 body units
    [TestCase(1.29f, true)] // a plant's pinned reach, 1.3 plant body units
    [TestCase(2.08f, true)] // the long band, 2.1 plant body units
    [TestCase(2.3f, false)]
    public void TryValidate_RootReach_AcceptsEveryReachBand(float footRadius, bool valid)
    {
        _recipe.roots.footRadius = footRadius;
        _recipe.roots.thickness = 0.07f;

        Assert.AreEqual(valid, CreatureValidator.TryValidate(_recipe, out _));
    }

    [TestCase(0, true)]
    [TestCase(1, false)]
    public void TryValidate_PartCount_AcceptsUpToMaxParts(int partsOverCap, bool valid)
    {
        int count = CreatureValidator.MaxParts + partsOverCap;
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

    [Test]
    public void TryValidate_FirstPartWithAParent_ReturnsFalse()
    {
        _recipe.parts[0].parent = 0;

        Assert.IsFalse(CreatureValidator.TryValidate(_recipe, out _));
    }

    [Test]
    public void TryValidate_DuplicateIds_ReturnsFalse()
    {
        _recipe.parts = new CreaturePart[] { _recipe.parts[0], _recipe.parts[0] };

        Assert.IsFalse(CreatureValidator.TryValidate(_recipe, out _));
    }

    [Test]
    public void TryValidate_ArmWithoutASourceSocket_ReturnsFalse()
    {
        _recipe.sourceLocal = new Vector3[0];

        Assert.IsFalse(CreatureValidator.TryValidate(_recipe, out _));
    }

    [Test]
    public void TryValidate_RestJointOffTheLinkLength_ReturnsFalse()
    {
        _recipe.arms[0].restJoints[2] = Vector3.one * 100f;

        Assert.IsFalse(CreatureValidator.TryValidate(_recipe, out _));
    }

    [Test]
    public void TryValidate_RootsPastTheLongestReach_ReturnsFalse()
    {
        _recipe.roots.footRadius = CreatureValidator.MaxRootReach;

        Assert.IsFalse(CreatureValidator.TryValidate(_recipe, out _));
    }
}

}
