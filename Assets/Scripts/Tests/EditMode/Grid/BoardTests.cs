using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Grids
{

// The board in the world: a tile on each active cell and the unit standing on each cell
public class BoardTests
{
    GameObject _go;
    Board _board;
    BoardLayout _layout;
    readonly List<Object> _created = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Board");
        _go.transform.position = new Vector3(0.5f, 0f, 0f);
        _board = _go.AddComponent<Board>();
        _layout = ScriptableObject.CreateInstance<BoardLayout>();
        _board.SetLayout(_layout);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_layout);
        foreach (Object created in _created)
        {
            if (created != null)
            {
                Object.DestroyImmediate(created);
            }
        }
        _created.Clear();
    }

    GameObject CreateUnit()
    {
        GameObject unit = new GameObject("Unit");
        _created.Add(unit);
        return unit;
    }

    [Test]
    public void SetLayout_ATileOnEachCellOfThePlayerOrTheEnemies()
    {
        Assert.AreEqual(32, _board.tiles.Count);
        Assert.IsTrue(_board.tiles.ContainsKey(new Vector2Int(0, 0)));
        Assert.IsTrue(_board.tiles.ContainsKey(new Vector2Int(8, 3)));
        Assert.IsFalse(_board.tiles.ContainsKey(new Vector2Int(4, 1)), "nobody's cell");
    }

    [Test]
    public void SetLayout_AnotherBoard_TheTilesLaidAgain()
    {
        BoardLayout small = ScriptableObject.CreateInstance<BoardLayout>();
        _created.Add(small);
        small.Resize(2, 1);

        _board.SetLayout(small);

        Assert.AreEqual(2, _board.tiles.Count);
        Assert.AreEqual(2, _go.transform.childCount, "the old tiles destroyed");
    }

    [Test]
    public void SetLayout_Null_TheDefaultBoard()
    {
        _board.defaultLayout = _layout;

        _board.SetLayout(null);

        Assert.AreSame(_layout, _board.layout);
        Assert.AreEqual(32, _board.tiles.Count);
    }

    [Test]
    public void Tiles_OnTheGroundOfTheirCell()
    {
        Vector3 center = _board.GetCellCenter(new Vector2Int(5, 2));
        Vector3 tile = _board.tiles[new Vector2Int(5, 2)].transform.position;

        Assert.AreEqual(center.x, tile.x, 0.0001f);
        Assert.AreEqual(center.z, tile.z, 0.0001f);
        Assert.AreEqual(0f, tile.y, 0.05f);
    }

    [Test]
    public void GetUnitPosition_HalfACellAboveTheCenterOfTheCell_TheBoardCenteredOnItsTransform()
    {
        // 9 x 4 cells of 1 around (0.5, 0, 0): from x = -4 to 5, from z = -2 to 2
        Assert.AreEqual(new Vector3(-3.5f, 0.5f, -1.5f), _board.GetUnitPosition(new Vector2Int(0, 0)));
        Assert.AreEqual(new Vector3(1.5f, 0.5f, 0.5f), _board.GetUnitPosition(new Vector2Int(5, 2)));
    }

    [Test]
    public void IsFree_OnlyAnEmptyCellOfThatSide()
    {
        Assert.IsTrue(_board.IsFree(new Vector2Int(0, 0), BoardCellType.Player));
        Assert.IsFalse(_board.IsFree(new Vector2Int(0, 0), BoardCellType.Enemy), "the other side");
        Assert.IsFalse(_board.IsFree(new Vector2Int(4, 0), BoardCellType.Player), "nobody's cell");
        Assert.IsFalse(_board.IsFree(new Vector2Int(4, 0), BoardCellType.None));

        _board.Occupy(new Vector2Int(0, 0), CreateUnit(), BoardCellType.Player);

        Assert.IsFalse(_board.IsFree(new Vector2Int(0, 0), BoardCellType.Player), "taken");
    }

    [Test]
    public void Occupy_TheUnitStandsOnTheCell_LeavingItsOldOne()
    {
        GameObject unit = CreateUnit();
        Assert.IsTrue(_board.Occupy(new Vector2Int(1, 1), unit, BoardCellType.Player));

        Assert.IsTrue(_board.Occupy(new Vector2Int(2, 3), unit, BoardCellType.Player));

        Assert.AreSame(unit, _board.GetOccupant(new Vector2Int(2, 3)));
        Assert.IsNull(_board.GetOccupant(new Vector2Int(1, 1)));
        Assert.IsTrue(_board.TryGetCoordOf(unit, out Vector2Int coord));
        Assert.AreEqual(new Vector2Int(2, 3), coord);
    }

    [Test]
    public void Occupy_ATakenCellOrACellOfTheOtherSide_Refused()
    {
        GameObject first = CreateUnit();
        GameObject second = CreateUnit();
        _board.Occupy(new Vector2Int(1, 1), first, BoardCellType.Player);

        Assert.IsFalse(_board.Occupy(new Vector2Int(1, 1), second, BoardCellType.Player));
        Assert.IsFalse(_board.Occupy(new Vector2Int(6, 1), second, BoardCellType.Player));
        Assert.AreSame(first, _board.GetOccupant(new Vector2Int(1, 1)));
        Assert.IsFalse(_board.TryGetCoordOf(second, out _));
    }

    [Test]
    public void Release_TheCellFreeAgain()
    {
        GameObject unit = CreateUnit();
        _board.Occupy(new Vector2Int(6, 0), unit, BoardCellType.Enemy);

        _board.Release(unit);

        Assert.IsTrue(_board.IsFree(new Vector2Int(6, 0), BoardCellType.Enemy));
    }

    [Test]
    public void ADestroyedUnit_NoLongerTakesItsCell()
    {
        GameObject unit = CreateUnit();
        _board.Occupy(new Vector2Int(6, 0), unit, BoardCellType.Enemy);

        Object.DestroyImmediate(unit);

        Assert.IsNull(_board.GetOccupant(new Vector2Int(6, 0)));
        Assert.IsTrue(_board.IsFree(new Vector2Int(6, 0), BoardCellType.Enemy));
    }

    [Test]
    public void TryGetNearestFreeCell_TheFreeCellOfTheSideNearestToThePosition()
    {
        // Over the empty column, next to the player's front column
        Vector3 position = _board.GetCellCenter(new Vector2Int(4, 1));

        Assert.IsTrue(_board.TryGetNearestFreeCell(BoardCellType.Player, position, out Vector2Int player));
        Assert.AreEqual(new Vector2Int(3, 1), player);
        Assert.IsTrue(_board.TryGetNearestFreeCell(BoardCellType.Enemy, position, out Vector2Int enemy));
        Assert.AreEqual(new Vector2Int(5, 1), enemy);
    }

    [Test]
    public void TryGetNearestFreeCell_SkipsTheTakenCells()
    {
        _board.Occupy(new Vector2Int(3, 1), CreateUnit(), BoardCellType.Player);

        Assert.IsTrue(_board.TryGetNearestFreeCell(BoardCellType.Player, _board.GetCellCenter(new Vector2Int(3, 1)), out Vector2Int coord));

        Assert.AreNotEqual(new Vector2Int(3, 1), coord);
        Assert.AreEqual(1f, Vector2Int.Distance(new Vector2Int(3, 1), coord), 0.0001f, "a neighbour");
    }

    [Test]
    public void TryGetNearestFreeCell_EveryCellTaken_False()
    {
        foreach (Vector2Int cell in _layout.GetCells(BoardCellType.Player))
        {
            _board.Occupy(cell, CreateUnit(), BoardCellType.Player);
        }

        Assert.IsFalse(_board.TryGetNearestFreeCell(BoardCellType.Player, Vector3.zero, out _));
    }

    [Test]
    public void TryGetCoord_TheCellUnderThePosition()
    {
        Assert.IsTrue(_board.TryGetCoord(new Vector3(1.6f, 0.5f, 0.4f), out Vector2Int coord));
        Assert.AreEqual(new Vector2Int(5, 2), coord);
        Assert.IsFalse(_board.TryGetCoord(new Vector3(6f, 0f, 0f), out _));
    }
}

}
