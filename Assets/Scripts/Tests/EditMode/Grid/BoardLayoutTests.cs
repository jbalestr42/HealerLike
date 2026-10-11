using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Grids
{

// The board of a fight: a grid whose cells are for the player, the enemies or nobody
public class BoardLayoutTests
{
    const string DefaultLayoutPath = "Assets/Data/Board/DefaultBoardLayout.asset";

    BoardLayout _layout;

    [SetUp]
    public void SetUp()
    {
        _layout = ScriptableObject.CreateInstance<BoardLayout>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_layout);
    }

    [Test]
    public void NewLayout_PlayerOnTheLeftEnemiesOnTheRightAnEmptyColumnBetween()
    {
        Assert.AreEqual(9, _layout.width);
        Assert.AreEqual(4, _layout.height);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                Assert.AreEqual(BoardCellType.Player, _layout.GetCell(x, y), $"({x}, {y})");
            }
            Assert.AreEqual(BoardCellType.None, _layout.GetCell(4, y), $"(4, {y})");
            for (int x = 5; x < 9; x++)
            {
                Assert.AreEqual(BoardCellType.Enemy, _layout.GetCell(x, y), $"({x}, {y})");
            }
        }
    }

    [Test]
    public void GetCell_OutOfTheBoard_IsNone()
    {
        Assert.AreEqual(BoardCellType.None, _layout.GetCell(-1, 0));
        Assert.AreEqual(BoardCellType.None, _layout.GetCell(0, 4));
        Assert.AreEqual(BoardCellType.None, _layout.GetCell(9, 0));
    }

    [Test]
    public void SetCell_ChangesOnlyThatCell()
    {
        _layout.SetCell(1, 2, BoardCellType.None);

        Assert.AreEqual(BoardCellType.None, _layout.GetCell(1, 2));
        Assert.AreEqual(BoardCellType.Player, _layout.GetCell(1, 1));
        Assert.AreEqual(BoardCellType.Player, _layout.GetCell(2, 2));
    }

    [Test]
    public void SetCell_OutOfTheBoard_ChangesNothing()
    {
        List<BoardCellType> before = new List<BoardCellType>(_layout.cells);

        _layout.SetCell(12, 0, BoardCellType.Enemy);

        CollectionAssert.AreEqual(before, _layout.cells);
    }

    [Test]
    public void GetCells_AreTheCellsOfThatSide()
    {
        _layout.SetCell(0, 0, BoardCellType.None);

        List<Vector2Int> player = _layout.GetCells(BoardCellType.Player);
        List<Vector2Int> enemy = _layout.GetCells(BoardCellType.Enemy);

        Assert.AreEqual(15, player.Count);
        CollectionAssert.DoesNotContain(player, new Vector2Int(0, 0));
        Assert.AreEqual(16, enemy.Count);
        CollectionAssert.Contains(enemy, new Vector2Int(8, 3));
    }

    [Test]
    public void GetCells_ASideCanHaveSeveralZones()
    {
        _layout.SetCell(4, 0, BoardCellType.Player);
        for (int y = 0; y < 4; y++)
        {
            _layout.SetCell(1, y, BoardCellType.None);
        }

        List<Vector2Int> player = _layout.GetCells(BoardCellType.Player);

        CollectionAssert.Contains(player, new Vector2Int(0, 0));
        CollectionAssert.Contains(player, new Vector2Int(2, 0));
        CollectionAssert.Contains(player, new Vector2Int(4, 0));
        Assert.AreEqual(13, player.Count);
    }

    [Test]
    public void Resize_KeepsEachCellAtItsPlace()
    {
        _layout.SetCell(2, 3, BoardCellType.Enemy);

        _layout.Resize(5, 6);

        Assert.AreEqual(5, _layout.width);
        Assert.AreEqual(6, _layout.height);
        Assert.AreEqual(30, _layout.cells.Count);
        Assert.AreEqual(BoardCellType.Enemy, _layout.GetCell(2, 3));
        Assert.AreEqual(BoardCellType.Player, _layout.GetCell(0, 0));
        Assert.AreEqual(BoardCellType.None, _layout.GetCell(4, 0), "The old empty column");
        Assert.AreEqual(BoardCellType.None, _layout.GetCell(0, 5), "A new cell");
    }

    [Test]
    public void Resize_Smaller_DropsTheCellsOutside()
    {
        _layout.Resize(3, 2);

        Assert.AreEqual(6, _layout.cells.Count);
        Assert.AreEqual(BoardCellType.Player, _layout.GetCell(2, 1));
        CollectionAssert.IsEmpty(_layout.GetCells(BoardCellType.Enemy));
    }

    [Test]
    public void Resize_NeverBelowOneCell()
    {
        _layout.Resize(0, -2);

        Assert.AreEqual(1, _layout.width);
        Assert.AreEqual(1, _layout.height);
    }

    [Test]
    public void GetCellCenter_TheBoardIsCenteredOnItsOrigin()
    {
        _layout.cellSize = 2f;
        Vector3 origin = new Vector3(10f, 1f, 0f);

        // 9 x 4 cells of 2: from x = 1 to 19, from z = -4 to 4
        Assert.AreEqual(new Vector3(2f, 1f, -3f), _layout.GetCellCenter(origin, new Vector2Int(0, 0)));
        Assert.AreEqual(new Vector3(18f, 1f, 3f), _layout.GetCellCenter(origin, new Vector2Int(8, 3)));
    }

    [Test]
    public void TryGetCoord_IsTheCellUnderThePosition()
    {
        Vector3 origin = Vector3.zero;

        Assert.IsTrue(_layout.TryGetCoord(origin, _layout.GetCellCenter(origin, new Vector2Int(6, 1)), out Vector2Int coord));
        Assert.AreEqual(new Vector2Int(6, 1), coord);
        // Near the corner of a cell
        Assert.IsTrue(_layout.TryGetCoord(origin, new Vector3(-4.45f, 0f, 1.95f), out coord));
        Assert.AreEqual(new Vector2Int(0, 3), coord);
    }

    [Test]
    public void TryGetCoord_OutOfTheBoard_IsFalse()
    {
        Assert.IsFalse(_layout.TryGetCoord(Vector3.zero, new Vector3(-4.6f, 0f, 0f), out _));
        Assert.IsFalse(_layout.TryGetCoord(Vector3.zero, new Vector3(0f, 0f, 2.1f), out _));
    }

    [Test]
    public void GetCellType_EachUnitStandsOnItsSide()
    {
        Assert.AreEqual(BoardCellType.Player, BoardLayout.GetCellType(Entity.EntityType.Player));
        Assert.AreEqual(BoardCellType.Enemy, BoardLayout.GetCellType(Entity.EntityType.Computer));
    }

    [Test]
    public void GetFrontColumn_TheColumnOfEachSideNearestToTheOther()
    {
        Assert.AreEqual(3, _layout.GetFrontColumn(BoardCellType.Player));
        Assert.AreEqual(5, _layout.GetFrontColumn(BoardCellType.Enemy));
    }

    [Test]
    public void GetFrontColumn_TheSidesSwapped()
    {
        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                BoardCellType type = _layout.GetCell(x, y);
                _layout.SetCell(x, y, type == BoardCellType.Player ? BoardCellType.Enemy : type == BoardCellType.Enemy ? BoardCellType.Player : type);
            }
        }

        Assert.AreEqual(5, _layout.GetFrontColumn(BoardCellType.Player));
        Assert.AreEqual(3, _layout.GetFrontColumn(BoardCellType.Enemy));
    }

    [Test]
    public void GetFrontColumn_ASideWithoutCell_Is0()
    {
        foreach (Vector2Int cell in _layout.GetCells(BoardCellType.Enemy))
        {
            _layout.SetCell(cell.x, cell.y, BoardCellType.None);
        }

        Assert.AreEqual(0, _layout.GetFrontColumn(BoardCellType.Enemy));
    }

    [Test]
    public void TryGetCenterCell_NoExactCenter_AlwaysTheFirstOfTheNearestCellsColumnAfterColumn()
    {
        // The player on 4 x 4 cells: (1, 1), (1, 2), (2, 1) and (2, 2) as near to the middle (1.5, 1.5)
        Assert.IsTrue(_layout.TryGetCenterCell(BoardCellType.Player, out Vector2Int center));
        Assert.AreEqual(new Vector2Int(1, 1), center);
        Assert.IsTrue(_layout.TryGetCenterCell(BoardCellType.Enemy, out Vector2Int enemyCenter));
        Assert.AreEqual(new Vector2Int(6, 1), enemyCenter);
    }

    [Test]
    public void TryGetCenterCell_AnExactCenter_IsThatCell()
    {
        // The player on 3 columns of 5 rows: the middle on (1, 2)
        _layout.Resize(5, 5);
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                _layout.SetCell(x, y, x < 3 ? BoardCellType.Player : BoardCellType.Enemy);
            }
        }

        Assert.IsTrue(_layout.TryGetCenterCell(BoardCellType.Player, out Vector2Int center));
        Assert.AreEqual(new Vector2Int(1, 2), center);
    }

    [Test]
    public void TryGetCenterCell_TheMiddleOnNoCellOfTheSide_TheNearestCellOfTheSide()
    {
        // The player in a ring around (1, 1), its middle
        foreach (Vector2Int cell in _layout.GetCells(BoardCellType.Player))
        {
            _layout.SetCell(cell.x, cell.y, BoardCellType.None);
        }
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (x != 1 || y != 1)
                {
                    _layout.SetCell(x, y, BoardCellType.Player);
                }
            }
        }

        Assert.IsTrue(_layout.TryGetCenterCell(BoardCellType.Player, out Vector2Int center));
        Assert.AreEqual(BoardCellType.Player, _layout.GetCell(center));
        Assert.AreEqual(new Vector2Int(0, 1), center, "first of the 4 cells at 1 from the middle");
    }

    [Test]
    public void TryGetCenterCell_ASideWithoutCell_IsFalse()
    {
        foreach (Vector2Int cell in _layout.GetCells(BoardCellType.Enemy))
        {
            _layout.SetCell(cell.x, cell.y, BoardCellType.None);
        }

        Assert.IsFalse(_layout.TryGetCenterCell(BoardCellType.Enemy, out _));
    }

    [Test]
    public void DefaultLayoutAsset_Is9x4WithFourColumnsPerSide()
    {
        BoardLayout layout = AssetDatabase.LoadAssetAtPath<BoardLayout>(DefaultLayoutPath);

        Assert.IsNotNull(layout);
        Assert.AreEqual(9, layout.width);
        Assert.AreEqual(4, layout.height);
        Assert.AreEqual(16, layout.GetCells(BoardCellType.Player).Count);
        Assert.AreEqual(16, layout.GetCells(BoardCellType.Enemy).Count);
        Assert.AreEqual(BoardCellType.Player, layout.GetCell(3, 0));
        Assert.AreEqual(BoardCellType.Enemy, layout.GetCell(5, 0));
    }
}

}
