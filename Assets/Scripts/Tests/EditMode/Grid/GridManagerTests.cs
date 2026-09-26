using NUnit.Framework;
using UnityEngine;

namespace Grids
{

public class GridManagerTests
{
    GameObject _go;
    GameObject _ground;
    GridManager _gridManager;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("GridManager");
        _gridManager = _go.AddComponent<GridManager>();
        _ground = new GameObject("Ground");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_ground);
    }

    [Test]
    public void Ground_ReturnsTheSerializedGround()
    {
        TestHelpers.SetPrivateField(_gridManager, "_ground", _ground);

        Assert.AreSame(_ground, _gridManager.ground);
    }

    void Generate(int width = 4, int height = 3, float size = 1f)
    {
        TestHelpers.SetPrivateField(_gridManager, "_ground", _ground);
        _gridManager.width = width;
        _gridManager.height = height;
        _gridManager.size = size;
        _gridManager.Generate();
    }

    [Test]
    public void Generate_CreatesAllCellsWalkable()
    {
        Generate();

        Assert.AreEqual(12, _gridManager.cells.Length);
        foreach (GridCell cell in _gridManager.cells)
        {
            Assert.IsTrue(cell.walkable);
            Assert.IsTrue(_gridManager.IsWalkable(cell.coord.x, cell.coord.y));
        }
    }

    [Test]
    public void SetWalkable_False_MakesOnlyThatCellUnwalkable()
    {
        Generate();

        _gridManager.SetWalkable(1, 2, false);

        Assert.IsFalse(_gridManager.IsWalkable(1, 2));
        Assert.IsTrue(_gridManager.IsWalkable(2, 1));
        Assert.IsTrue(_gridManager.IsWalkable(0, 2));
    }

    [Test]
    public void SetWalkable_True_RestoresWalkability()
    {
        Generate();
        _gridManager.SetWalkable(new Vector2Int(3, 0), false);

        _gridManager.SetWalkable(_gridManager.GetCell(3, 0), true);

        Assert.IsTrue(_gridManager.IsWalkable(3, 0));
    }

    [Test]
    public void SetWalkable_FromPosition_TargetsTheCellUnderThatPosition()
    {
        Generate();
        Vector3 center = _gridManager.GetCellCenterFromCoord(new Vector2Int(2, 1));

        _gridManager.SetWalkable(center, false);

        Assert.IsFalse(_gridManager.IsWalkable(2, 1));
    }

    [Test]
    public void SetWalkable_OutOfBounds_DoesNothing()
    {
        Generate();

        Assert.DoesNotThrow(() => _gridManager.SetWalkable(-1, 0, false));
        Assert.DoesNotThrow(() => _gridManager.SetWalkable(4, 0, false));
        foreach (GridCell cell in _gridManager.cells)
        {
            Assert.IsTrue(cell.walkable);
        }
    }

    [Test]
    public void IsWalkable_OutOfBounds_ReturnsFalse()
    {
        Generate();

        Assert.IsFalse(_gridManager.IsWalkable(-1, 0));
        Assert.IsFalse(_gridManager.IsWalkable(0, 3));
    }

    [Test]
    public void CanPlaceObject_ReturnsTrueOnlyOnFreeCells()
    {
        Generate();
        _gridManager.SetWalkable(1, 1, false);

        Assert.IsTrue(_gridManager.CanPlaceObject(new Vector2Int(0, 0)));
        Assert.IsTrue(_gridManager.CanPlaceObject(_gridManager.GetCell(2, 1)));
        Assert.IsFalse(_gridManager.CanPlaceObject(new Vector2Int(1, 1)));
        Assert.IsFalse(_gridManager.CanPlaceObject(new Vector2Int(-1, 1)));
    }

    [Test]
    public void GetNearestWalkablePosition_OnFreeCell_ReturnsThatCellCenter()
    {
        Generate();
        Vector3 center = _gridManager.GetCellCenterFromCoord(new Vector2Int(2, 1));

        Vector3 result = _gridManager.GetNearestWalkablePosition(center + new Vector3(0.2f, 0f, -0.3f));

        Assert.AreEqual(center, result);
    }

    [Test]
    public void GetNearestWalkablePosition_OnOccupiedCell_ReturnsClosestFreeCellCenter()
    {
        Generate();
        _gridManager.SetWalkable(1, 1, false);
        Vector3 occupiedCenter = _gridManager.GetCellCenterFromCoord(new Vector2Int(1, 1));

        // Slightly offset towards +x so (2, 1) is strictly the closest free cell.
        Vector3 result = _gridManager.GetNearestWalkablePosition(occupiedCenter + new Vector3(0.2f, 0f, 0f));

        Assert.AreEqual(_gridManager.GetCellCenterFromCoord(new Vector2Int(2, 1)), result);
    }

    [Test]
    public void GetNearestWalkablePosition_OutsideTheGrid_ReturnsClosestEdgeCellCenter()
    {
        Generate();
        Vector3 corner = _gridManager.GetCellCenterFromCoord(new Vector2Int(3, 2));

        Vector3 result = _gridManager.GetNearestWalkablePosition(corner + new Vector3(5f, 0f, 5f));

        Assert.AreEqual(corner, result);
    }

    [Test]
    public void GetNearestWalkablePosition_NoFreeCell_ReturnsCenterOfCellUnderPosition()
    {
        Generate();
        foreach (GridCell cell in _gridManager.cells)
        {
            _gridManager.SetWalkable(cell, false);
        }
        Vector3 center = _gridManager.GetCellCenterFromCoord(new Vector2Int(1, 2));

        Vector3 result = _gridManager.GetNearestWalkablePosition(center + new Vector3(0.1f, 0f, 0.1f));

        Assert.AreEqual(center, result);
    }
}

}
