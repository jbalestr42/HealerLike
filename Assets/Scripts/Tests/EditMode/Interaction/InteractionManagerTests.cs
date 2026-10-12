using NUnit.Framework;
using UnityEngine;

namespace Interaction
{

// The drag of a unit, ended by the release of the mouse button wherever the cursor is (over the UI too)
public class InteractionManagerTests
{
    // Records the calls the manager makes
    class FakeDraggable : IDraggable
    {
        public int endDragCount;
        public int cancelDragCount;

        #region IDraggable

        public bool CanDrag()
        {
            return true;
        }

        public void StartDrag(RaycastHit hit)
        {
        }

        public void Drag(RaycastHit hit)
        {
        }

        public void EndDrag(RaycastHit hit)
        {
            endDragCount++;
        }

        public void CancelDrag()
        {
            cancelDragCount++;
        }

        #endregion
    }

    GameObject _managerGo;
    InteractionManager _manager;

    [SetUp]
    public void SetUp()
    {
        _managerGo = new GameObject("InteractionManager");
        _manager = _managerGo.AddComponent<InteractionManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_managerGo);
    }

    FakeDraggable StartDragging()
    {
        FakeDraggable draggable = new FakeDraggable();
        TestHelpers.SetPrivateField(_manager, "_draggable", draggable);
        return draggable;
    }

    [Test]
    public void EndDrag_EndsTheDragOfTheDraggedUnit()
    {
        FakeDraggable draggable = StartDragging();

        _manager.EndDrag(new RaycastHit());

        Assert.AreEqual(1, draggable.endDragCount);
        Assert.AreEqual(0, draggable.cancelDragCount);
    }

    [Test]
    public void EndDrag_TwiceInARow_EndsTheDragOnce()
    {
        FakeDraggable draggable = StartDragging();

        _manager.EndDrag(new RaycastHit());
        _manager.EndDrag(new RaycastHit());

        Assert.AreEqual(1, draggable.endDragCount);
    }

    [Test]
    public void EndDrag_NothingDragged_DoesNothing()
    {
        Assert.DoesNotThrow(() => _manager.EndDrag(new RaycastHit()));
    }

    [Test]
    public void CancelDrag_AfterTheDragEnded_DoesNotMoveTheUnitBack()
    {
        FakeDraggable draggable = StartDragging();

        _manager.EndDrag(new RaycastHit());
        _manager.CancelDrag();

        Assert.AreEqual(0, draggable.cancelDragCount);
    }
}

}
