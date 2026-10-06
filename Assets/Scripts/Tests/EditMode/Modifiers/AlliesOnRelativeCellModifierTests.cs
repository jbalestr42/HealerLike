using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Modifiers
{

// The allies and the grid come from the EntityManager and PlayerBehaviour singletons in the game
public class TestAlliesOnRelativeCellModifier : AlliesOnRelativeCellModifier
{
    public List<GameObject> allies = new List<GameObject>();
    public float cellSize = 2f;

    protected override List<GameObject> GetAllies() => allies;
    protected override float GetCellSize() => cellSize;
}

// Phalanx: a value for each living ally on a cell of the pattern around the target
public class AlliesOnRelativeCellModifierTests
{
    const float CellSize = 2f;

    readonly TestUnits _units = new TestUnits();
    Entity _target;

    [SetUp]
    public void SetUp()
    {
        _target = CreateAt("Target", 0, 0);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    // A unit on the cell (x, y) of the grid
    Entity CreateAt(string name, int x, int y)
    {
        Entity entity = _units.Create(100f, 100f, name);
        entity.transform.position = new Vector3(x * CellSize, 0f, y * CellSize);
        return entity;
    }

    TestAlliesOnRelativeCellModifier CreateModifier(params Entity[] allies)
    {
        return CreateModifier(false, allies);
    }

    TestAlliesOnRelativeCellModifier CreateModifier(bool isWhenAlone, params Entity[] allies)
    {
        TestAlliesOnRelativeCellModifier modifier = new TestAlliesOnRelativeCellModifier
        {
            data = new AlliesOnRelativeCellModifierData { value = 2f, isWhenAlone = isWhenAlone, pattern = RelativeCellPatternType.Adjacent, range = 1 },
            cellSize = CellSize,
        };
        modifier.allies.Add(_target.gameObject);
        foreach (Entity ally in allies)
        {
            modifier.allies.Add(ally.gameObject);
        }
        modifier.Init(null, _target.gameObject);
        return modifier;
    }

    [Test]
    public void ApplyModifier_WithoutAdjacentAlly_IsZero()
    {
        Assert.AreEqual(0f, CreateModifier().ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_GivesTheValueForEachAdjacentAlly()
    {
        TestAlliesOnRelativeCellModifier modifier = CreateModifier(CreateAt("Right", 1, 0), CreateAt("Diagonal", -1, 1), CreateAt("Far", 2, 0));

        Assert.AreEqual(2, modifier.CountAllies());
        Assert.AreEqual(4f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_SkipsTheDeadAllies()
    {
        Entity dead = CreateAt("Dead", 1, 0);
        dead.health.SetValue(0f);

        Assert.AreEqual(0f, CreateModifier(dead).ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_FollowsTheAlliesMovingAway()
    {
        Entity ally = CreateAt("Ally", 1, 0);
        TestAlliesOnRelativeCellModifier modifier = CreateModifier(ally);
        Assert.AreEqual(2f, modifier.ApplyModifier(), 0.0001f);

        ally.transform.position = new Vector3(3f * CellSize, 0f, 0f);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_WhenAlone_WithoutAdjacentAlly_GivesTheValue()
    {
        TestAlliesOnRelativeCellModifier modifier = CreateModifier(true, CreateAt("Far", 2, 0));

        Assert.AreEqual(2f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_WhenAlone_WithAnAdjacentAlly_IsZero()
    {
        TestAlliesOnRelativeCellModifier modifier = CreateModifier(true, CreateAt("Right", 1, 0), CreateAt("Diagonal", -1, 1));

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_WhenAlone_AnAdjacentAllyDying_GivesTheValueBack()
    {
        Entity ally = CreateAt("Ally", 1, 0);
        TestAlliesOnRelativeCellModifier modifier = CreateModifier(true, ally);
        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);

        ally.health.SetValue(0f);

        Assert.AreEqual(2f, modifier.ApplyModifier(), 0.0001f);
    }
}

}
