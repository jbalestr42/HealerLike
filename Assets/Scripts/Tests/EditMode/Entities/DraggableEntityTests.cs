using NUnit.Framework;
using UnityEngine;

namespace Entities
{

public class DraggableEntityTests
{
    GameObject _gridGo;
    GameObject _ground;
    GridManager _grid;
    GameObject _goA;
    GameObject _goB;

    [SetUp]
    public void SetUp()
    {
        _gridGo = new GameObject("GridManager");
        _ground = new GameObject("Ground");
        _grid = _gridGo.AddComponent<GridManager>();
        TestHelpers.SetPrivateField(_grid, "_ground", _ground);
        _grid.width = 4;
        _grid.height = 3;
        _grid.size = 1f;
        _grid.Generate();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_goA);
        Object.DestroyImmediate(_goB);
        Object.DestroyImmediate(_gridGo);
        Object.DestroyImmediate(_ground);
    }

    // Builds a DraggableEntity standing on coord, wired the way Start() would (without its
    // PlayerBehaviour singleton lookup), with its cell flagged as occupied like EntityManager does.
    DraggableEntity CreateDraggable(ref GameObject go, Vector2Int coord)
    {
        go = new GameObject("Draggable");
        DraggableEntity draggable = go.AddComponent<DraggableEntity>();
        Vector3 center = _grid.GetCellCenterFromCoord(coord);
        go.transform.position = center;
        TestHelpers.SetPrivateField(draggable, "_grid", _grid);
        TestHelpers.SetPrivateField(draggable, "_colliders", new Collider[0]);
        TestHelpers.SetPrivateField(draggable, "_originalLayers", new int[0]);
        TestHelpers.SetPrivateField(draggable, "_homePosition", center);
        _grid.SetWalkable(coord, false);
        return draggable;
    }

    [Test]
    public void StartDrag_FreesTheHomeCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));

        a.StartDrag(new RaycastHit());

        Assert.IsTrue(_grid.IsWalkable(1, 1));
    }

    [Test]
    public void CancelDrag_GoesBackHomeAndOccupiesTheHomeCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());
        _goA.transform.position = _grid.GetCellCenterFromCoord(new Vector2Int(3, 0));

        a.CancelDrag();

        Assert.AreEqual(_grid.GetCellCenterFromCoord(new Vector2Int(1, 1)), _goA.transform.position);
        Assert.IsFalse(_grid.IsWalkable(1, 1));
        Assert.IsTrue(_grid.IsWalkable(3, 0));
    }

    [Test]
    public void EndDrag_OccupiesTheDropCellAndFreesTheOldHome()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());
        Vector3 dropCenter = _grid.GetCellCenterFromCoord(new Vector2Int(3, 0));
        _goA.transform.position = dropCenter;

        a.EndDrag(new RaycastHit());

        Assert.AreEqual(dropCenter, a.homePosition);
        Assert.IsFalse(_grid.IsWalkable(3, 0));
        Assert.IsTrue(_grid.IsWalkable(1, 1));
    }

    [Test]
    public void MoveToNearestFreeCell_FreeCell_MovesOntoIt()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());
        Vector3 target = _grid.GetCellCenterFromCoord(new Vector2Int(3, 2));

        a.MoveToNearestFreeCell(target + new Vector3(0.2f, 0f, -0.2f));

        Assert.AreEqual(target, _goA.transform.position);
    }

    [Test]
    public void MoveToNearestFreeCell_CellOfAnotherUnit_MovesOntoTheClosestFreeCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(0, 0));
        CreateDraggable(ref _goB, new Vector2Int(2, 1));
        a.StartDrag(new RaycastHit());
        Vector3 occupied = _grid.GetCellCenterFromCoord(new Vector2Int(2, 1));

        // Cursor on the right side of the occupied cell: the free cell on its right is the closest
        a.MoveToNearestFreeCell(occupied + new Vector3(0.4f, 0f, 0f));

        Assert.AreEqual(_grid.GetCellCenterFromCoord(new Vector2Int(3, 1)), _goA.transform.position);
    }

    [Test]
    public void MoveToNearestFreeCell_OwnHomeCell_CanGoBackOnIt()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());
        _goA.transform.position = _grid.GetCellCenterFromCoord(new Vector2Int(3, 0));

        a.MoveToNearestFreeCell(_grid.GetCellCenterFromCoord(new Vector2Int(1, 1)));

        Assert.AreEqual(_grid.GetCellCenterFromCoord(new Vector2Int(1, 1)), _goA.transform.position);
    }

    [Test]
    public void EndDrag_OverAnotherUnit_LeavesItInPlaceAndDropsOnTheClosestFreeCell()
    {
        Vector2Int coordA = new Vector2Int(0, 0);
        Vector2Int coordB = new Vector2Int(2, 1);
        DraggableEntity a = CreateDraggable(ref _goA, coordA);
        DraggableEntity b = CreateDraggable(ref _goB, coordB);
        a.StartDrag(new RaycastHit());
        a.MoveToNearestFreeCell(_grid.GetCellCenterFromCoord(coordB) + new Vector3(0f, 0f, 0.4f));

        a.EndDrag(new RaycastHit());

        Vector3 above = _grid.GetCellCenterFromCoord(new Vector2Int(2, 2));
        Assert.AreEqual(above, a.homePosition);
        Assert.AreEqual(_grid.GetCellCenterFromCoord(coordB), b.homePosition);
        Assert.AreEqual(_grid.GetCellCenterFromCoord(coordB), _goB.transform.position);
        Assert.IsFalse(_grid.IsWalkable(2, 2));
        Assert.IsFalse(_grid.IsWalkable(coordB.x, coordB.y));
        Assert.IsTrue(_grid.IsWalkable(coordA.x, coordA.y));
    }
}

}
