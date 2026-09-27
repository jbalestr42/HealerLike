using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// The real grid and spawn go through the PlayerBehaviour and EntityManager singletons
public class TestSummonSkill : SummonSkill
{
    public GridManager testGrid;
    public bool spawnFails;
    public readonly List<GameObject> spawned = new List<GameObject>();

    protected override GridManager grid => testGrid;

    protected override GameObject Spawn(Vector3 position)
    {
        if (spawnFails)
        {
            return null;
        }
        GameObject summon = new GameObject("Summon");
        summon.transform.position = position;
        testGrid.SetWalkable(position, false);
        spawned.Add(summon);
        return summon;
    }
}

public class SummonSkillTests
{
    GameObject _gridGo;
    GameObject _ground;
    GridManager _grid;
    GameObject _caster;
    TestSummonSkill _skill;

    [SetUp]
    public void SetUp()
    {
        _gridGo = new GameObject("Grid");
        _ground = new GameObject("Ground");
        _grid = _gridGo.AddComponent<GridManager>();
        TestHelpers.SetPrivateField(_grid, "_ground", _ground);
        _grid.width = 5;
        _grid.height = 5;
        _grid.size = 1f;
        _grid.Generate();

        _caster = new GameObject("Necromancer");
        _caster.transform.position = CellCenter(2, 2);
        _grid.SetWalkable(2, 2, false);
        _skill = _caster.AddComponent<TestSummonSkill>();
        _skill.testGrid = _grid;
        _skill.data = new SummonSkillData { onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(), cooldown = 5f, maxAlive = 2 };
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject summon in _skill.spawned)
        {
            if (summon != null)
            {
                Object.DestroyImmediate(summon);
            }
        }
        Object.DestroyImmediate(_caster);
        Object.DestroyImmediate(_gridGo);
        Object.DestroyImmediate(_ground);
    }

    Vector3 CellCenter(int x, int y)
    {
        Vector3 center = _grid.GetCellCenterFromCoord(new Vector2Int(x, y));
        // Entities stand on the ground, below the cell centers
        return new Vector3(center.x, 0f, center.z);
    }

    [Test]
    public void Execute_SummonsOnTheFreeCellClosestToTheCaster()
    {
        // Only (2, 3) stays free around the caster
        _grid.SetWalkable(1, 2, false);
        _grid.SetWalkable(3, 2, false);
        _grid.SetWalkable(2, 1, false);

        bool used = _skill.Execute(_caster);

        Assert.IsTrue(used);
        Assert.AreEqual(1, _skill.spawned.Count);
        Assert.AreEqual(CellCenter(2, 3), _skill.spawned[0].transform.position);
    }

    [Test]
    public void Execute_SummonsOnTheCasterPlane()
    {
        _caster.transform.position = CellCenter(2, 2) + Vector3.up * 0.3f;

        _skill.Execute(_caster);

        Assert.AreEqual(0.3f, _skill.spawned[0].transform.position.y, 0.0001f);
    }

    [Test]
    public void Execute_NeverSummonsOutsideTheGrid()
    {
        // In a corner, half of the neighbours are out of the grid
        _grid.SetWalkable(2, 2, true);
        _caster.transform.position = CellCenter(0, 0);
        _grid.SetWalkable(0, 0, false);
        _skill.data.maxAlive = 10;

        for (int i = 0; i < 5; i++)
        {
            _skill.Execute(_caster);
        }

        Assert.AreEqual(5, _skill.spawned.Count);
        foreach (GameObject summon in _skill.spawned)
        {
            Vector2Int coord = _grid.GetCoordFromPosition(summon.transform.position);
            Assert.IsTrue(_grid.IsValidCoord(coord), coord.ToString());
            Assert.AreEqual(CellCenter(coord.x, coord.y), summon.transform.position);
        }
    }

    [Test]
    public void Execute_MaxAliveReached_DoesNotSummon()
    {
        _skill.Execute(_caster);
        _skill.Execute(_caster);

        bool used = _skill.Execute(_caster);

        Assert.IsFalse(used);
        Assert.AreEqual(2, _skill.spawned.Count);
        Assert.AreEqual(2, _skill.aliveCount);
    }

    [Test]
    public void Execute_AfterASummonDied_SummonsAgain()
    {
        _skill.Execute(_caster);
        _skill.Execute(_caster);
        Object.DestroyImmediate(_skill.spawned[0]);

        bool used = _skill.Execute(_caster);

        Assert.IsTrue(used);
        Assert.AreEqual(2, _skill.aliveCount);
    }

    [Test]
    public void Execute_NoFreeCell_DoesNotSummon()
    {
        foreach (GridCell cell in _grid.cells)
        {
            _grid.SetWalkable(cell, false);
        }

        bool used = _skill.Execute(_caster);

        Assert.IsFalse(used);
        Assert.AreEqual(0, _skill.aliveCount);
    }

    [Test]
    public void Execute_SpawnFailed_IsNotUsedAndCountsNoSummon()
    {
        _skill.spawnFails = true;

        bool used = _skill.Execute(_caster);

        Assert.IsFalse(used);
        Assert.AreEqual(0, _skill.aliveCount);
    }

    [Test]
    public void CooldownDuration_IsTheDataCooldown()
    {
        Assert.AreEqual(5f, _skill.cooldownDuration);
    }
}

}
