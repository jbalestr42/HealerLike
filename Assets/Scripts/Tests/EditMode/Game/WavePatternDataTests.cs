using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

public class WavePatternDataTests
{
    WavePatternData _wave;

    [SetUp]
    public void SetUp()
    {
        _wave = ScriptableObject.CreateInstance<WavePatternData>();
        _wave.width = 3;
        _wave.height = 2;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_wave);
    }

    EntityData CreateUnit(string name)
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        unit.name = name;
        _units.Add(unit);
        return unit;
    }

    readonly System.Collections.Generic.List<EntityData> _units = new System.Collections.Generic.List<EntityData>();

    [TearDown]
    public void DestroyUnits()
    {
        foreach (EntityData unit in _units)
        {
            Object.DestroyImmediate(unit);
        }
        _units.Clear();
    }

    [Test]
    public void SetEntity_ThenGetEntity_TheUnitOfTheCell_OthersEmpty()
    {
        EntityData soldier = CreateUnit("Soldier");

        _wave.SetEntity(2, 1, soldier);

        Assert.AreSame(soldier, _wave.GetEntity(2, 1));
        Assert.IsNull(_wave.GetEntity(1, 2), "another cell");
        Assert.IsNull(_wave.GetEntity(0, 0));
    }

    [Test]
    public void OutOfTheGrid_NoUnit_AndNothingSet()
    {
        EntityData soldier = CreateUnit("Soldier");

        _wave.SetEntity(3, 0, soldier);
        _wave.SetEntity(0, -1, soldier);

        Assert.IsNull(_wave.GetEntity(3, 0));
        Assert.IsNull(_wave.GetEntity(-1, 0));
        CollectionAssert.IsEmpty(_wave.GetUnits());
    }

    [Test]
    public void GetUnits_ColumnAfterColumn_EmptyCellsLeftOut()
    {
        EntityData front = CreateUnit("Front");
        EntityData back = CreateUnit("Back");
        EntityData middle = CreateUnit("Middle");
        _wave.SetEntity(2, 0, back);
        _wave.SetEntity(0, 1, front);
        _wave.SetEntity(1, 0, middle);

        CollectionAssert.AreEqual(new[] { (0, 1, front), (1, 0, middle), (2, 0, back) }, _wave.GetUnits());
    }

    [Test]
    public void Resize_KeepsTheUnitsOfTheCellsStillInTheGrid()
    {
        EntityData kept = CreateUnit("Kept");
        EntityData cut = CreateUnit("Cut");
        _wave.SetEntity(1, 1, kept);
        _wave.SetEntity(2, 0, cut);

        _wave.Resize(2, 4);

        Assert.AreEqual(2, _wave.width);
        Assert.AreEqual(4, _wave.height);
        Assert.AreSame(kept, _wave.GetEntity(1, 1));
        Assert.IsNull(_wave.GetEntity(1, 3), "new cell");
        CollectionAssert.AreEqual(new[] { (1, 1, kept) }, _wave.GetUnits());
    }

    [Test]
    public void SettingTheWidthOrTheHeight_ResizesTheGrid()
    {
        EntityData unit = CreateUnit("Unit");
        _wave.SetEntity(0, 1, unit);

        _wave.width = 1;
        _wave.height = 3;

        Assert.AreSame(unit, _wave.GetEntity(0, 1));
        _wave.SetEntity(0, 2, unit);
        Assert.AreSame(unit, _wave.GetEntity(0, 2));
    }

    BoardLayout CreateLayout()
    {
        BoardLayout layout = ScriptableObject.CreateInstance<BoardLayout>();
        _layouts.Add(layout);
        return layout;
    }

    readonly List<BoardLayout> _layouts = new List<BoardLayout>();

    [TearDown]
    public void DestroyLayouts()
    {
        foreach (BoardLayout layout in _layouts)
        {
            Object.DestroyImmediate(layout);
        }
        _layouts.Clear();
    }

    [Test]
    public void SetLayout_TheWaveTakesTheSizeOfTheBoard_EachUnitKeepingItsCell()
    {
        EntityData unit = CreateUnit("Unit");
        _wave.SetEntity(2, 1, unit);

        _wave.SetLayout(CreateLayout());

        Assert.AreEqual(9, _wave.width);
        Assert.AreEqual(4, _wave.height);
        Assert.AreSame(unit, _wave.GetEntity(2, 1));
    }

    [Test]
    public void IsEnemyCell_OnlyTheEnemyCellsOfTheBoard_EveryCellWithoutBoard()
    {
        Assert.IsTrue(_wave.IsEnemyCell(0, 0), "without board");

        _wave.SetLayout(CreateLayout());

        Assert.IsTrue(_wave.IsEnemyCell(5, 0));
        Assert.IsTrue(_wave.IsEnemyCell(8, 3));
        Assert.IsFalse(_wave.IsEnemyCell(4, 0), "nobody's cell");
        Assert.IsFalse(_wave.IsEnemyCell(0, 0), "a player cell");
    }

    [Test]
    public void GetUnits_LeavesOutTheUnitsOffTheEnemyCells()
    {
        EntityData enemy = CreateUnit("Enemy");
        EntityData stray = CreateUnit("Stray");
        _wave.SetLayout(CreateLayout());
        _wave.SetEntity(6, 2, enemy);
        _wave.SetEntity(1, 2, stray);

        CollectionAssert.AreEqual(new[] { (6, 2, enemy) }, _wave.GetUnits());
    }

    [Test]
    public void FrontColumn_TheEnemyColumnNearestToThePlayer()
    {
        Assert.AreEqual(0, _wave.frontColumn, "without board");

        BoardLayout layout = CreateLayout();
        _wave.SetLayout(layout);
        Assert.AreEqual(5, _wave.frontColumn, "the player on the left");

        // The sides swapped: the player on the right
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                BoardCellType type = layout.GetCell(x, y);
                layout.SetCell(x, y, type == BoardCellType.Player ? BoardCellType.Enemy : type == BoardCellType.Enemy ? BoardCellType.Player : type);
            }
        }
        Assert.AreEqual(3, _wave.frontColumn, "the player on the right");
    }

    [Test]
    public void GetUnitsByRank_TheColumnsCountedFromTheFrontOne()
    {
        EntityData front = CreateUnit("Front");
        EntityData back = CreateUnit("Back");
        _wave.SetLayout(CreateLayout());
        _wave.SetEntity(5, 1, front);
        _wave.SetEntity(7, 3, back);

        CollectionAssert.AreEqual(new[] { (0, 1, front), (2, 3, back) }, _wave.GetUnitsByRank());
    }

    [Test]
    public void EveryWaveAsset_StandsOnTheDefaultBoard_EveryUnitOnAnEnemyCell()
    {
        BoardLayout defaultLayout = AssetDatabase.LoadAssetAtPath<BoardLayout>("Assets/Data/Board/DefaultBoardLayout.asset");
        string[] guids = AssetDatabase.FindAssets("t:WavePatternData", new[] { "Assets/Data/WavePatterns", "Assets/Data/Balance" });
        Assert.IsNotEmpty(guids);

        foreach (string guid in guids)
        {
            WavePatternData wave = AssetDatabase.LoadAssetAtPath<WavePatternData>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.AreSame(defaultLayout, wave.layout, wave.name);
            Assert.AreEqual(defaultLayout.width, wave.width, wave.name);
            Assert.AreEqual(defaultLayout.height, wave.height, wave.name);
            for (int x = 0; x < wave.width; x++)
            {
                for (int y = 0; y < wave.height; y++)
                {
                    if (wave.GetEntity(x, y) != null)
                    {
                        Assert.IsTrue(wave.IsEnemyCell(x, y), $"{wave.name} ({x}, {y})");
                    }
                }
            }
            Assert.IsNotEmpty(new List<(int, int, EntityData)>(wave.GetUnits()), wave.name);
        }
    }

    // The balance attackers stand like the dummies, in an X on the three front columns: corners and center
    [Test]
    public void BalanceWaves_TheFiveAttackersInTheSameXAsTheDummies()
    {
        WavePatternData attackers = AssetDatabase.LoadAssetAtPath<WavePatternData>("Assets/Data/Balance/Wave_Balance_Attackers.asset");
        WavePatternData dummies = AssetDatabase.LoadAssetAtPath<WavePatternData>("Assets/Data/Balance/Wave_Balance_Dummies.asset");
        Vector2Int[] cross = { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0), new Vector2Int(2, 2) };

        foreach (WavePatternData wave in new[] { attackers, dummies })
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            foreach ((int rank, int y, EntityData entity) unit in wave.GetUnitsByRank())
            {
                cells.Add(new Vector2Int(unit.rank, unit.y));
            }
            CollectionAssert.AreEquivalent(cross, cells, wave.name);
        }
        HashSet<EntityData> attackerUnits = new HashSet<EntityData>();
        foreach ((int x, int y, EntityData entity) unit in attackers.GetUnits())
        {
            attackerUnits.Add(unit.entity);
        }
        Assert.AreEqual(5, attackerUnits.Count, "five different attackers");
    }
}

}
