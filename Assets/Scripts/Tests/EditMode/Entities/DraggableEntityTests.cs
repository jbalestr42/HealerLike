using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The units of the player dragged on the board: the default one, the player on the 4 left columns
public class DraggableEntityTests
{
    GameObject _boardGo;
    Board _board;
    BoardLayout _layout;
    GameObject _goA;
    GameObject _goB;

    [SetUp]
    public void SetUp()
    {
        _boardGo = new GameObject("Board");
        _board = _boardGo.AddComponent<Board>();
        _layout = ScriptableObject.CreateInstance<BoardLayout>();
        _board.SetLayout(_layout);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_goA);
        Object.DestroyImmediate(_goB);
        Object.DestroyImmediate(_boardGo);
        Object.DestroyImmediate(_layout);
    }

    // A unit of the player standing on its cell, wired the way Start() would (without its EntityManager singleton
    // lookup)
    DraggableEntity CreateDraggable(ref GameObject go, Vector2Int coord)
    {
        go = new GameObject("Draggable");
        DraggableEntity draggable = go.AddComponent<DraggableEntity>();
        go.transform.position = _board.GetUnitPosition(coord);
        TestHelpers.SetPrivateField(draggable, "_board", _board);
        TestHelpers.SetPrivateField(draggable, "_colliders", new Collider[0]);
        TestHelpers.SetPrivateField(draggable, "_originalLayers", new int[0]);
        TestHelpers.SetPrivateField(draggable, "_homePosition", go.transform.position);
        _board.Occupy(coord, go, BoardCellType.Player);
        return draggable;
    }

    [Test]
    public void StartDrag_FreesItsCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));

        a.StartDrag(new RaycastHit());

        Assert.IsTrue(_board.IsFree(new Vector2Int(1, 1), BoardCellType.Player));
    }

    [Test]
    public void MoveToNearestFreeCell_FreeCell_MovesOntoIt()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());

        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(3, 2)) + new Vector3(0.2f, 0f, -0.2f));

        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(3, 2)), _goA.transform.position);
    }

    [Test]
    public void MoveToNearestFreeCell_CellOfAnotherUnit_MovesOntoTheClosestFreeCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(0, 0));
        CreateDraggable(ref _goB, new Vector2Int(2, 1));
        a.StartDrag(new RaycastHit());

        // Cursor on the right side of the occupied cell: the free cell on its right is the closest
        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(2, 1)) + new Vector3(0.4f, 0f, 0f));

        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(3, 1)), _goA.transform.position);
    }

    [Test]
    public void MoveToNearestFreeCell_OwnCell_CanGoBackOnIt()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());
        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(3, 0)));

        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(1, 1)));

        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(1, 1)), _goA.transform.position);
    }

    [Test]
    public void MoveToNearestFreeCell_NeverOnTheEnemySide()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());

        // The cursor over an enemy cell
        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(6, 2)));

        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(3, 2)), _goA.transform.position);
    }

    [Test]
    public void EndDrag_OccupiesTheDropCellAndFreesTheOldOne()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());
        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(3, 0)));

        a.EndDrag(new RaycastHit());

        Assert.AreSame(_goA, _board.GetOccupant(new Vector2Int(3, 0)));
        Assert.IsTrue(_board.IsFree(new Vector2Int(1, 1), BoardCellType.Player));
        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(3, 0)), a.homePosition);
    }

    [Test]
    public void EndDrag_OverAnotherUnit_LeavesItInPlaceAndDropsOnTheClosestFreeCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(0, 0));
        DraggableEntity b = CreateDraggable(ref _goB, new Vector2Int(2, 1));
        a.StartDrag(new RaycastHit());
        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(2, 1)) + new Vector3(0f, 0f, 0.4f));

        a.EndDrag(new RaycastHit());

        Assert.AreSame(_goA, _board.GetOccupant(new Vector2Int(2, 2)), "the cell above, the closest to the cursor");
        Assert.AreSame(_goB, _board.GetOccupant(new Vector2Int(2, 1)));
        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(2, 1)), b.homePosition);
        Assert.IsTrue(_board.IsFree(new Vector2Int(0, 0), BoardCellType.Player));
    }

    [Test]
    public void EndDrag_OnATakenCell_BackOnItsCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        CreateDraggable(ref _goB, new Vector2Int(2, 2));
        a.StartDrag(new RaycastHit());
        _goA.transform.position = _board.GetUnitPosition(new Vector2Int(2, 2));

        a.EndDrag(new RaycastHit());

        Assert.AreSame(_goA, _board.GetOccupant(new Vector2Int(1, 1)));
        Assert.AreSame(_goB, _board.GetOccupant(new Vector2Int(2, 2)));
    }

    [Test]
    public void EndDrag_ItsColliderHitByTheRaycastsAgain()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        GameObject colliderGo = new GameObject("Collider");
        colliderGo.transform.SetParent(_goA.transform);
        colliderGo.layer = Layers.Entity;
        TestHelpers.SetPrivateField(a, "_colliders", new Collider[] { colliderGo.AddComponent<BoxCollider>() });
        TestHelpers.SetPrivateField(a, "_originalLayers", new int[1]);
        a.StartDrag(new RaycastHit());
        Assert.AreEqual(Layers.IgnoreRaycast, colliderGo.layer, "ignored by the raycasts while dragged");

        a.EndDrag(new RaycastHit());

        // Otherwise the unit could never be clicked and dragged again
        Assert.AreEqual(Layers.Entity, colliderGo.layer);
    }

    [Test]
    public void CancelDrag_BackOnItsCell()
    {
        DraggableEntity a = CreateDraggable(ref _goA, new Vector2Int(1, 1));
        a.StartDrag(new RaycastHit());
        a.MoveToNearestFreeCell(_board.GetCellCenter(new Vector2Int(3, 0)));

        a.CancelDrag();

        Assert.AreSame(_goA, _board.GetOccupant(new Vector2Int(1, 1)));
        Assert.AreEqual(_board.GetUnitPosition(new Vector2Int(1, 1)), _goA.transform.position);
        Assert.IsTrue(_board.IsFree(new Vector2Int(3, 0), BoardCellType.Player));
    }
}

}
