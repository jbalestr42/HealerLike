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

    // A unit standing on its cell of the board, like EntityManager puts it
    GameObject PlaceUnit(Vector2Int coord, BoardCellType side)
    {
        GameObject unit = CreateUnit();
        unit.transform.position = _board.GetUnitPosition(coord);
        Assert.IsTrue(_board.Occupy(coord, unit, side));
        return unit;
    }

    BoardLayout CreateLayout()
    {
        BoardLayout layout = ScriptableObject.CreateInstance<BoardLayout>();
        _created.Add(layout);
        return layout;
    }

    [Test]
    public void SetLayout_AUnitStillOnACellOfItsSide_KeepsItsCell()
    {
        GameObject unit = PlaceUnit(new Vector2Int(2, 1), BoardCellType.Player);
        Vector3 position = unit.transform.position;

        List<GameObject> displaced = _board.SetLayout(CreateLayout());

        CollectionAssert.IsEmpty(displaced);
        Assert.AreSame(unit, _board.GetOccupant(new Vector2Int(2, 1)));
        Assert.AreEqual(position, unit.transform.position);
    }

    [Test]
    public void SetLayout_ACellNoLongerOfItsSide_TheUnitGoesToTheNearestFreeCellOfItsSide()
    {
        GameObject unit = PlaceUnit(new Vector2Int(3, 1), BoardCellType.Player);
        BoardLayout layout = CreateLayout();
        for (int y = 0; y < 4; y++)
        {
            layout.SetCell(3, y, BoardCellType.None);
        }

        List<GameObject> displaced = _board.SetLayout(layout);

        CollectionAssert.IsEmpty(displaced);
        Assert.IsTrue(_board.TryGetCoordOf(unit, out Vector2Int coord));
        Assert.AreEqual(new Vector2Int(2, 1), coord);
        Assert.AreEqual(_board.GetUnitPosition(coord), unit.transform.position);
    }

    [Test]
    public void SetLayout_TheUnitsKeepingTheirCellAreNeverPushedByTheMovedOnes()
    {
        GameObject moved = PlaceUnit(new Vector2Int(3, 1), BoardCellType.Player);
        GameObject kept = PlaceUnit(new Vector2Int(2, 1), BoardCellType.Player);
        BoardLayout layout = CreateLayout();
        layout.SetCell(3, 1, BoardCellType.None);

        _board.SetLayout(layout);

        Assert.AreSame(kept, _board.GetOccupant(new Vector2Int(2, 1)));
        Assert.IsTrue(_board.TryGetCoordOf(moved, out Vector2Int coord));
        Assert.AreNotEqual(new Vector2Int(2, 1), coord);
        Assert.AreEqual(BoardCellType.Player, layout.GetCell(coord));
    }

    [Test]
    public void SetLayout_NoFreeCellLeftForAUnit_ItIsReturnedOffTheBoard()
    {
        GameObject first = PlaceUnit(new Vector2Int(0, 0), BoardCellType.Player);
        GameObject second = PlaceUnit(new Vector2Int(1, 0), BoardCellType.Player);
        BoardLayout layout = CreateLayout();
        layout.Resize(1, 1);

        List<GameObject> displaced = _board.SetLayout(layout);

        Assert.AreSame(first, _board.GetOccupant(new Vector2Int(0, 0)));
        CollectionAssert.AreEqual(new[] { second }, displaced);
        Assert.IsFalse(_board.TryGetCoordOf(second, out _));
    }

    [Test]
    public void SetLayout_EachUnitStaysOnItsSide()
    {
        GameObject enemy = PlaceUnit(new Vector2Int(5, 0), BoardCellType.Enemy);
        BoardLayout layout = CreateLayout();
        for (int y = 0; y < 4; y++)
        {
            layout.SetCell(5, y, BoardCellType.Player);
        }

        _board.SetLayout(layout);

        Assert.IsTrue(_board.TryGetCoordOf(enemy, out Vector2Int coord));
        Assert.AreEqual(new Vector2Int(6, 0), coord);
    }

    [Test]
    public void TryGetPlacementCell_TheCellUnderThePositionWhenFree()
    {
        Assert.IsTrue(_board.TryGetPlacementCell(_board.GetCellCenter(new Vector2Int(0, 3)) + new Vector3(0.4f, 0f, -0.4f), BoardCellType.Player, out Vector2Int cell));

        Assert.AreEqual(new Vector2Int(0, 3), cell);
    }

    [Test]
    public void TryGetPlacementCell_ATakenCellOrACellOfAnotherSide_TheNearestFreeCellOfTheSide()
    {
        PlaceUnit(new Vector2Int(0, 3), BoardCellType.Player);

        Assert.IsTrue(_board.TryGetPlacementCell(_board.GetCellCenter(new Vector2Int(0, 3)) + new Vector3(0.4f, 0f, 0f), BoardCellType.Player, out Vector2Int taken));
        Assert.IsTrue(_board.TryGetPlacementCell(_board.GetCellCenter(new Vector2Int(5, 0)), BoardCellType.Player, out Vector2Int enemySide));

        Assert.AreEqual(new Vector2Int(1, 3), taken, "on the right of the taken cell, nearer to the cursor");
        Assert.AreEqual(new Vector2Int(3, 0), enemySide);
    }

    [Test]
    public void TryGetPlacementPosition_WhereTheUnitStandsOnThatCell_FalseWithoutFreeCell()
    {
        Assert.IsTrue(_board.TryGetPlacementPosition(_board.GetCellCenter(new Vector2Int(6, 2)), BoardCellType.Enemy, out Vector3 placement));
        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(6, 2)), placement);

        foreach (Vector2Int cell in _layout.GetCells(BoardCellType.Enemy))
        {
            _board.Occupy(cell, CreateUnit(), BoardCellType.Enemy);
        }
        Assert.IsFalse(_board.TryGetPlacementPosition(Vector3.zero, BoardCellType.Enemy, out _));
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
