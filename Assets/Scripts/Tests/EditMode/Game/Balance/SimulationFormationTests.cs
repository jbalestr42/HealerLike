using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class SimulationFormationTests
{
    readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    EntityData CreateUnit(string role, float maxHealth = 100f)
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        unit.attributes[AttributeType.HealthMax] = maxHealth;
        if (role != null)
        {
            GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
            tag.name = role;
            _created.Add(tag);
            unit.tags.Add(tag);
        }
        _created.Add(unit);
        return unit;
    }

    [Test]
    public void GetRowOffset_FromTheCenterOutwards()
    {
        CollectionAssert.AreEqual(new[] { 0, 1, -1, 2, -2 }, new List<int> { 0, 1, 2, 3, 4 }.ConvertAll(SimulationFormation.GetRowOffset));
    }

    [Test]
    public void GetRoleRank_TanksThenSupportsThenTheOthers()
    {
        Assert.AreEqual(0, SimulationFormation.GetRoleRank(CreateUnit(TagNames.Tank)));
        Assert.AreEqual(1, SimulationFormation.GetRoleRank(CreateUnit(TagNames.Support)));
        Assert.AreEqual(2, SimulationFormation.GetRoleRank(CreateUnit(TagNames.Damage)));
        Assert.AreEqual(2, SimulationFormation.GetRoleRank(CreateUnit(null)));
    }

    [Test]
    public void GetCells_TanksOnTheRight_SupportsInTheMiddle_DamageOnTheLeft()
    {
        List<EntityData> units = new List<EntityData>
        {
            CreateUnit(TagNames.Damage),
            CreateUnit(TagNames.Support),
            CreateUnit(TagNames.Tank),
        };

        Dictionary<int, Vector2Int> cells = SimulationFormation.GetCells(units, 6, 8, 5);

        Assert.AreEqual(new Vector2Int(6, 8), cells[2]);
        Assert.AreEqual(new Vector2Int(5, 8), cells[1]);
        Assert.AreEqual(new Vector2Int(4, 8), cells[0]);
    }

    [Test]
    public void GetCells_SameRole_FromTheCenterOutwards_MostHealthFirst()
    {
        List<EntityData> units = new List<EntityData>
        {
            CreateUnit(TagNames.Damage, 80f),
            CreateUnit(TagNames.Damage, 200f),
            CreateUnit(TagNames.Damage, 100f),
        };

        Dictionary<int, Vector2Int> cells = SimulationFormation.GetCells(units, 6, 8, 5);

        Assert.AreEqual(new Vector2Int(6, 8), cells[1]);
        Assert.AreEqual(new Vector2Int(6, 9), cells[2]);
        Assert.AreEqual(new Vector2Int(6, 7), cells[0]);
    }

    [Test]
    public void GetCells_NoTank_TheSupportsInFront()
    {
        List<EntityData> units = new List<EntityData> { CreateUnit(TagNames.Damage), CreateUnit(TagNames.Support) };

        Dictionary<int, Vector2Int> cells = SimulationFormation.GetCells(units, 6, 8, 5);

        Assert.AreEqual(new Vector2Int(6, 8), cells[1]);
        Assert.AreEqual(new Vector2Int(5, 8), cells[0]);
    }

    [Test]
    public void GetCells_FullColumn_GoesOnInTheNextOneOnTheLeft_BeforeTheNextRole()
    {
        List<EntityData> units = new List<EntityData>
        {
            CreateUnit(TagNames.Tank),
            CreateUnit(TagNames.Tank),
            CreateUnit(TagNames.Tank),
            CreateUnit(TagNames.Damage),
        };

        Dictionary<int, Vector2Int> cells = SimulationFormation.GetCells(units, 6, 8, 2);

        Assert.AreEqual(new Vector2Int(6, 8), cells[0]);
        Assert.AreEqual(new Vector2Int(6, 9), cells[1]);
        Assert.AreEqual(new Vector2Int(5, 8), cells[2]);
        Assert.AreEqual(new Vector2Int(4, 8), cells[3]);
    }

    [Test]
    public void GetCells_NoUnit_NoCell()
    {
        Assert.IsEmpty(SimulationFormation.GetCells(new List<EntityData>(), 6, 8, 5));
    }

    [Test]
    public void GetCells_EveryUnitPlaced_EveryCellDifferent()
    {
        List<EntityData> units = new List<EntityData>();
        for (int i = 0; i < 12; i++)
        {
            units.Add(CreateUnit(i % 3 == 0 ? TagNames.Tank : i % 3 == 1 ? TagNames.Support : TagNames.Damage));
        }

        Dictionary<int, Vector2Int> cells = SimulationFormation.GetCells(units, 6, 8, 3);

        Assert.AreEqual(12, cells.Count);
        CollectionAssert.AllItemsAreUnique(cells.Values);
    }
}

}
