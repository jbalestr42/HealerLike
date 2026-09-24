using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureRecipeEditsTests
{
    CreatureRecipe _recipe;

    [SetUp]
    public void SetUp()
    {
        _recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_recipe);
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

    int AddPart(int parent)
    {
        return CreatureRecipeEdits.AddPart(_recipe, parent, Primitive.Sphere);
    }

    [Test]
    public void AddPart_RootThenChildren_KeepsUniqueIdsAndEarlierParents()
    {
        Assert.AreEqual(0, AddPart(-1));
        Assert.AreEqual(1, AddPart(0));
        Assert.AreEqual(2, AddPart(1));

        Assert.AreEqual(-1, _recipe.parts[0].parent);
        Assert.AreEqual(1, _recipe.parts[2].parent);
        Assert.AreNotEqual(_recipe.parts[1].id, _recipe.parts[2].id);
        Assert.IsEmpty(CreatureStudioAuthoring.Validate(_recipe));
    }

    [Test]
    public void AddPart_BadInputOrFullRecipe_ChangesNothing()
    {
        Assert.AreEqual(-1, CreatureRecipeEdits.AddPart(null, 0, Primitive.Sphere));
        Assert.AreEqual(-1, CreatureRecipeEdits.AddPart(_recipe, 0, (Primitive)999));
        AddPart(-1);
        Assert.AreEqual(-1, AddPart(5));
        while (_recipe.parts.Length < CreatureValidator.MaxParts)
        {
            AddPart(0);
        }
        CreaturePart[] before = _recipe.parts;

        Assert.AreEqual(-1, AddPart(0));
        Assert.AreEqual(-1, CreatureRecipeEdits.DuplicatePart(_recipe, 0));
        Assert.AreSame(before, _recipe.parts);
    }

    [Test]
    public void AddPart_ChildOfAColouredPart_TakesItsColour()
    {
        AddPart(-1);
        _recipe.parts[0].colour = new Color(0.1f, 0.3f, 0.8f);

        int child = AddPart(0);

        Assert.AreEqual(_recipe.parts[0].colour, _recipe.parts[child].colour);
    }

    [Test]
    public void DuplicatePart_ChildWithAnArm_CopiesThePartButNotTheArm()
    {
        AddPart(-1);
        CreatureRecipeEdits.AddPart(_recipe, 0, Primitive.Leaf);
        _recipe.parts[1].localPosition = Vector3.up;
        _recipe.parts[1].glow = 1f;
        _recipe.arms = new ArmDefinition[] { CreateArm(1) };
        _recipe.sourceLocal = new Vector3[] { Vector3.up };

        Assert.AreEqual(2, CreatureRecipeEdits.DuplicatePart(_recipe, 1));

        Assert.AreEqual(0, _recipe.parts[2].parent);
        Assert.AreEqual(Primitive.Leaf, _recipe.parts[2].primitive);
        Assert.AreEqual(1f, _recipe.parts[2].glow);
        Assert.AreEqual(Vector3.up + Vector3.right * 0.1f, _recipe.parts[2].localPosition);
        Assert.AreEqual(1, _recipe.arms.Length);
    }

    [Test]
    public void DuplicatePart_Root_BecomesAChildOfTheRoot()
    {
        AddPart(-1);

        Assert.AreEqual(1, CreatureRecipeEdits.DuplicatePart(_recipe, 0));

        Assert.AreEqual(0, _recipe.parts[1].parent);
        Assert.AreEqual(Vector3.right * 0.1f, _recipe.parts[1].localPosition);
        Assert.IsEmpty(CreatureStudioAuthoring.Validate(_recipe));
    }

    [Test]
    public void RemovePart_Subtree_RemapsTheSurvivingPartsArmsAndSockets()
    {
        AddPart(-1); // 0, the root
        AddPart(0); // 1, removed
        AddPart(0); // 2, survives as 1
        AddPart(1); // 3, removed with its parent
        AddPart(2); // 4, survives as 2 under 1
        _recipe.arms = new ArmDefinition[] { CreateArm(3), CreateArm(4), CreateArm(0) };
        _recipe.sourceLocal = new Vector3[] { Vector3.left, Vector3.right, Vector3.up, Vector3.forward };
        string survivor = _recipe.parts[4].id;

        Assert.IsTrue(CreatureRecipeEdits.RemovePart(_recipe, 1));

        Assert.AreEqual(3, _recipe.parts.Length);
        Assert.AreEqual(survivor, _recipe.parts[2].id);
        Assert.AreEqual(1, _recipe.parts[2].parent);
        Assert.AreEqual(2, _recipe.arms[0].bodyPart);
        Assert.AreEqual(0, _recipe.arms[1].bodyPart);
        CollectionAssert.AreEqual(new Vector3[] { Vector3.right, Vector3.up, Vector3.forward }, _recipe.sourceLocal);
    }

    [Test]
    public void RemovePart_RootOrBrokenHierarchy_IsRefused()
    {
        AddPart(-1);
        AddPart(0);
        CreaturePart[] before = _recipe.parts;

        Assert.IsFalse(CreatureRecipeEdits.RemovePart(_recipe, 0));
        Assert.IsFalse(CreatureRecipeEdits.RemovePart(_recipe, 5));
        Assert.IsFalse(CreatureRecipeEdits.RemovePart(null, 1));
        _recipe.parts[1].parent = 1;
        Assert.IsFalse(CreatureRecipeEdits.RemovePart(_recipe, 1));
        Assert.AreSame(before, _recipe.parts);
    }

    [Test]
    public void AddArm_OnARotatedChain_InsertsItsSourceBeforeTheOtherSockets()
    {
        AddPart(-1);
        AddPart(0);
        _recipe.parts[0].localEuler = new Vector3(0f, 0f, 90f);
        _recipe.sourceLocal = new Vector3[] { Vector3.forward };

        Assert.AreEqual(0, CreatureRecipeEdits.AddArm(_recipe, 1));

        Vector3 expected = _recipe.parts[0].localPosition + Quaternion.Euler(_recipe.parts[0].localEuler)
            * (_recipe.parts[1].localPosition + Vector3.up * 0.1f);
        Assert.Less(Vector3.Distance(expected, _recipe.sourceLocal[0]), 0.00001f);
        Assert.AreEqual(Vector3.forward, _recipe.sourceLocal[1]);
        Assert.AreEqual(4, _recipe.arms[0].restJoints.Length); // three links
    }

    [Test]
    public void AddArm_PastTheRigLimitOrOnNoPart_IsRefused()
    {
        AddPart(-1);
        Assert.AreEqual(-1, CreatureRecipeEdits.AddArm(_recipe, 4));
        for (int i = 0; i < ArmPool.MaxArms; i++)
        {
            Assert.AreEqual(i, CreatureRecipeEdits.AddArm(_recipe, 0));
        }

        Assert.AreEqual(-1, CreatureRecipeEdits.AddArm(_recipe, 0));
        Assert.AreEqual(ArmPool.MaxArms, _recipe.arms.Length);
    }

    [Test]
    public void AddArm_HeadColourOnTheRecipe_TipsTheArmWithIt()
    {
        AddPart(-1);
        int head = AddPart(0);
        _recipe.parts[head].role = PartRole.Head;
        _recipe.parts[head].colour = new Color(0.9f, 0.1f, 0.6f);

        int arm = CreatureRecipeEdits.AddArm(_recipe, 0);

        Assert.AreEqual(_recipe.parts[head].colour, _recipe.arms[arm].tipColour);
        Assert.AreEqual(_recipe.roots.colour, _recipe.arms[arm].colour);
    }

    [Test]
    public void RemoveArm_FirstOfTwo_RemovesItsSocketAndKeepsTheOther()
    {
        AddPart(-1);
        CreatureRecipeEdits.AddArm(_recipe, 0);
        CreatureRecipeEdits.AddArm(_recipe, 0);
        _recipe.sourceLocal = new Vector3[] { Vector3.left, Vector3.right, Vector3.forward };
        _recipe.arms[1].radius = 0.025f;

        Assert.IsTrue(CreatureRecipeEdits.RemoveArm(_recipe, 0));

        Assert.AreEqual(0.025f, _recipe.arms[0].radius);
        CollectionAssert.AreEqual(new Vector3[] { Vector3.right, Vector3.forward }, _recipe.sourceLocal);
        Assert.IsFalse(CreatureRecipeEdits.RemoveArm(_recipe, 5));
    }

    [TestCase(6, 0.27f, 6, 0.27f)]
    [TestCase(1000, 9f, 128, 1f)]
    [TestCase(-1, -1f, 2, 0.001f)]
    [TestCase(9, float.NaN, 9, 0.15f)]
    public void RebuildArmRestPose_CountAndLength_AreBoundedAndTheRestKept(int count, float length, int expectedCount,
        float expectedLength)
    {
        AddPart(-1);
        CreatureRecipeEdits.AddArm(_recipe, 0);
        _recipe.arms[0].segmentCount = count;
        _recipe.arms[0].segmentLength = length;
        _recipe.arms[0].colour = Color.magenta;

        Assert.IsTrue(CreatureRecipeEdits.RebuildArmRestPose(_recipe, 0));

        ArmDefinition arm = _recipe.arms[0];
        Assert.AreEqual(expectedCount, arm.segmentCount);
        Assert.AreEqual(expectedLength, arm.segmentLength);
        Assert.AreEqual(expectedCount + 1, arm.restJoints.Length);
        Assert.AreEqual(Color.magenta, arm.colour);
        for (int i = 1; i < arm.restJoints.Length; i++)
        {
            Assert.AreEqual(expectedLength, Vector3.Distance(arm.restJoints[i - 1], arm.restJoints[i]), 0.00001f);
        }
        Assert.IsFalse(CreatureRecipeEdits.RebuildArmRestPose(_recipe, 9));
    }
}

}
