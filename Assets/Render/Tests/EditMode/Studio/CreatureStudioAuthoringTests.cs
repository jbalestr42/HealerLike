using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureStudioAuthoringTests
{
    readonly List<Object> _objects = new List<Object>();
    CreatureRecipe _recipe;

    [SetUp]
    public void SetUp()
    {
        _recipe = Track(ScriptableObject.CreateInstance<CreatureRecipe>());
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            if (trackedObject != null)
            {
                Object.DestroyImmediate(trackedObject);
            }
        }
        _objects.Clear();
    }

    T Track<T>(T instance) where T : Object
    {
        _objects.Add(instance);
        return instance;
    }

    static ArmDefinition CreateArm(int bodyPart)
    {
        return new ArmDefinition
        {
            bodyPart = bodyPart,
            segmentCount = 2,
            segmentLength = 1f,
            radius = 0.03f,
            restJoints = new Vector3[] { Vector3.zero, Vector3.up, Vector3.up * 2f },
            bendPole = Vector3.forward,
            colour = Color.green
        };
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void BuildSample_EachStarter_IsValidAndOwnsItsParts(int index)
    {
        CreatureRecipe first = Track(CreatureStudioAuthoring.BuildSample(index));
        CreatureRecipe second = Track(CreatureStudioAuthoring.BuildSample(index));
        Vector3 before = second.parts[0].dimensions;

        first.parts[0].dimensions = Vector3.one * 12f;

        Assert.IsEmpty(CreatureStudioAuthoring.Validate(first));
        Assert.AreEqual(CreatureStudioAuthoring.SampleNames[index], first.name);
        Assert.AreEqual(HideFlags.None, first.hideFlags);
        Assert.AreEqual(before, second.parts[0].dimensions);
    }

    [Test]
    public void BuildSample_StoneSentinel_StandsWithoutRoots()
    {
        CreatureRecipe stone = Track(CreatureStudioAuthoring.BuildSample(2));

        Assert.AreEqual(0, stone.roots.count);
    }

    [Test]
    public void BuildSample_OutsideTheStarters_LogsAndReturnsNull()
    {
        LogAssert.Expect(LogType.Error, "[CreatureStudioAuthoring] No sample at 3");

        Assert.IsNull(CreatureStudioAuthoring.BuildSample(3));
    }

    [Test]
    public void Clone_RecipeWithAnArm_CopiesEveryArrayAndField()
    {
        CreatureRecipeEdits.AddPart(_recipe, -1, Primitive.Sphere);
        _recipe.arms = new ArmDefinition[] { CreateArm(0) };
        _recipe.sourceLocal = new Vector3[] { Vector3.up };
        _recipe.neckLocal = Vector3.one;
        _recipe.wiltColour = Color.yellow;
        _recipe.stoneOchre = Color.red;

        CreatureRecipe copy = Track(CreatureStudioAuthoring.Clone(_recipe));
        copy.arms[0].restJoints[1] = Vector3.forward;
        copy.sourceLocal[0] = Vector3.zero;
        copy.parts[0].colour = Color.black;

        Assert.AreEqual(Vector3.one, copy.neckLocal);
        Assert.AreEqual(Color.yellow, copy.wiltColour);
        Assert.AreEqual(Color.red, copy.stoneOchre);
        Assert.AreEqual(Vector3.up, _recipe.arms[0].restJoints[1]);
        Assert.AreEqual(Vector3.up, _recipe.sourceLocal[0]);
        Assert.AreNotEqual(Color.black, _recipe.parts[0].colour);
    }

    [Test]
    public void Clone_MissingArrays_GivesEmptyArrays()
    {
        _recipe.parts = null;
        _recipe.arms = null;
        _recipe.sourceLocal = null;

        CreatureRecipe copy = Track(CreatureStudioAuthoring.Clone(_recipe));

        Assert.IsNull(CreatureStudioAuthoring.Clone(null));
        Assert.IsEmpty(copy.parts);
        Assert.IsEmpty(copy.arms);
        Assert.IsEmpty(copy.sourceLocal);
    }

    [Test]
    public void Validate_BrokenRecipeAndExtraFields_ReportsEachProblem()
    {
        Assert.IsNotEmpty(CreatureStudioAuthoring.Validate(null));
        Assert.IsNotEmpty(CreatureStudioAuthoring.Validate(_recipe));
        CreatureRecipeEdits.AddPart(_recipe, -1, Primitive.Sphere);
        _recipe.neckLocal = new Vector3(float.NaN, 0f, 0f);
        _recipe.parts[0].role = (PartRole)999;
        _recipe.stoneOchre = new Color(float.PositiveInfinity, 0f, 0f);

        string[] warnings = CreatureStudioAuthoring.Validate(_recipe);

        CollectionAssert.AreEquivalent(new[]
        {
            "Neck coordinates must be finite.",
            "Wilt and stone colours must be finite.",
            "Every part needs a valid role."
        }, warnings);
    }
}

}
