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
    public void EndDrag_WithoutSwap_OccupiesTheDropCellAndFreesTheOldHome()
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
    public void CanMoveTo_FreeCell_ReturnsTrue()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));

        Assert.IsTrue(a.CanMoveTo(new Vector2Int(2, 2)));
    }

    [Test]
    public void CanMoveTo_CellOccupiedByAnotherEntity_ReturnsFalse()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        CreateDraggable(ref _goB, new Vector2Int(2, 1));

        Assert.IsFalse(a.CanMoveTo(new Vector2Int(2, 1)));
    }

    [Test]
    public void CanMoveTo_SwapTargetHomeCell_ReturnsTrueEvenThoughItIsOccupied()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        DraggableEntity b = CreateDraggable(ref _goB, new Vector2Int(2, 1));
        TestHelpers.SetPrivateField(a, "_swapTarget", b);

        Assert.IsFalse(_grid.IsWalkable(2, 1));
        Assert.IsTrue(a.CanMoveTo(new Vector2Int(2, 1)));
    }

    [Test]
    public void EndDrag_WithSwap_ExchangesHomesAndKeepsBothCellsOccupied()
    {
        Vector2Int coordA = new Vector2Int(1, 1);
        Vector2Int coordB = new Vector2Int(2, 1);
        DraggableEntity a = CreateDraggable(ref _goA, coordA);
        DraggableEntity b = CreateDraggable(ref _goB, coordB);
        a.StartDrag(new RaycastHit());
        b.PreviewSwapTo(a.homePosition);
        TestHelpers.SetPrivateField(a, "_swapTarget", b);

        a.EndDrag(new RaycastHit());

        Assert.AreEqual(_grid.GetCellCenterFromCoord(coordB), a.homePosition);
        Assert.AreEqual(_grid.GetCellCenterFromCoord(coordB), _goA.transform.position);
        Assert.AreEqual(_grid.GetCellCenterFromCoord(coordA), b.homePosition);
        Assert.AreEqual(_grid.GetCellCenterFromCoord(coordA), _goB.transform.position);
        Assert.IsFalse(_grid.IsWalkable(coordA.x, coordA.y));
        Assert.IsFalse(_grid.IsWalkable(coordB.x, coordB.y));
    }

    [Test]
    public void CommitSwap_MovesHomeAndOccupiesTheNewCell()
    {
        DraggableEntity b = CreateDraggable(ref _goB, new Vector2Int(2, 1));
        Vector3 newHome = _grid.GetCellCenterFromCoord(new Vector2Int(0, 2));

        b.CommitSwap(newHome);

        Assert.AreEqual(newHome, b.homePosition);
        Assert.AreEqual(newHome, _goB.transform.position);
        Assert.IsFalse(_grid.IsWalkable(0, 2));
    }
}

}
