using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// The real board and spawn go through the EntityManager singleton
public class TestSummonSkill : SummonSkill
{
    public Board testBoard;
    public Entity.EntityType testCasterType = Entity.EntityType.Computer;
    public bool spawnFails;
    public readonly List<GameObject> spawned = new List<GameObject>();

    protected override Board board => testBoard;
    protected override Entity.EntityType casterType => testCasterType;

    protected override GameObject Spawn(Vector3 position)
    {
        if (spawnFails)
        {
            return null;
        }
        GameObject summon = new GameObject("Summon");
        summon.transform.position = position;
        testBoard.TryGetCoord(position, out Vector2Int coord);
        testBoard.Occupy(coord, summon, BoardLayout.GetCellType(testCasterType));
        spawned.Add(summon);
        return summon;
    }
}

public class SummonSkillTests
{
    GameObject _boardGo;
    Board _board;
    BoardLayout _layout;
    GameObject _caster;
    TestSummonSkill _skill;
    readonly List<Object> _created = new List<Object>();

    // On the default board: the player on the 4 left columns, the enemies on the 4 right ones. The caster, an enemy,
    // on the front column of its side, the empty column on its left
    [SetUp]
    public void SetUp()
    {
        _boardGo = new GameObject("Board");
        _board = _boardGo.AddComponent<Board>();
        _layout = ScriptableObject.CreateInstance<BoardLayout>();
        _board.SetLayout(_layout);

        _caster = new GameObject("Necromancer");
        _caster.transform.position = _board.GetUnitPosition(new Vector2Int(5, 1));
        _board.Occupy(new Vector2Int(5, 1), _caster, BoardCellType.Enemy);
        _skill = _caster.AddComponent<TestSummonSkill>();
        _skill.testBoard = _board;
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
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
        Object.DestroyImmediate(_caster);
        Object.DestroyImmediate(_boardGo);
        Object.DestroyImmediate(_layout);
    }

    void Block(Vector2Int coord, BoardCellType side)
    {
        GameObject blocker = new GameObject("Blocker");
        _created.Add(blocker);
        _board.Occupy(coord, blocker, side);
    }

    [Test]
    public void Execute_SummonsOnTheFreeCellOfItsSideClosestToTheCaster()
    {
        // Only (6, 1) stays free around the caster on its side
        Block(new Vector2Int(5, 0), BoardCellType.Enemy);
        Block(new Vector2Int(5, 2), BoardCellType.Enemy);

        bool used = _skill.Execute(_caster);

        Assert.IsTrue(used);
        Assert.AreEqual(1, _skill.spawned.Count);
        Assert.AreSame(_skill.spawned[0], _board.GetOccupant(new Vector2Int(6, 1)));
        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(6, 1)), _skill.spawned[0].transform.position);
    }

    [Test]
    public void Execute_NeverOnTheOtherSide_NorOnTheCellsOfNobody()
    {
        _skill.data.maxAlive = 15;

        for (int i = 0; i < 15; i++)
        {
            _skill.Execute(_caster);
        }

        Assert.AreEqual(15, _skill.spawned.Count);
        foreach (GameObject summon in _skill.spawned)
        {
            Assert.IsTrue(_board.TryGetCoordOf(summon, out Vector2Int coord));
            Assert.AreEqual(BoardCellType.Enemy, _board.GetCellType(coord), coord.ToString());
        }
    }

    [Test]
    public void Execute_AnAllyOfThePlayer_SummonsOnThePlayerSide()
    {
        _board.Release(_caster);
        _caster.transform.position = _board.GetUnitPosition(new Vector2Int(3, 1));
        _board.Occupy(new Vector2Int(3, 1), _caster, BoardCellType.Player);
        _skill.testCasterType = Entity.EntityType.Player;

        _skill.Execute(_caster);

        Assert.IsTrue(_board.TryGetCoordOf(_skill.spawned[0], out Vector2Int coord));
        Assert.AreEqual(BoardCellType.Player, _board.GetCellType(coord), coord.ToString());
        Assert.AreEqual(1f, Vector2Int.Distance(new Vector2Int(3, 1), coord), 0.0001f, "a neighbour");
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
    public void Execute_NoFreeCellOfItsSide_DoesNotSummon()
    {
        foreach (Vector2Int cell in _layout.GetCells(BoardCellType.Enemy))
        {
            if (_board.GetOccupant(cell) == null)
            {
                Block(cell, BoardCellType.Enemy);
            }
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
