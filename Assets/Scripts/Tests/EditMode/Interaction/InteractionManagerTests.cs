using NUnit.Framework;
using UnityEngine;

namespace Interaction
{

// The drag of a unit, ended by the release of the mouse button wherever the cursor is (over the UI too), and the
// selection kept or not when an interaction starts (e.g. a spell aimed while a unit panel is open)
public class InteractionManagerTests
{
    class FakeSelectable : ISelectable
    {
        public int unselectCount;

        #region ISelectable

        public void Select()
        {
        }

        public void UnSelect()
        {
            unselectCount++;
        }

        #endregion
    }

    class FakeInteraction : AInteraction
    {
        public int cancelCount;

        public override int GetLayerMask()
        {
            return 0;
        }

        public override void Cancel()
        {
            cancelCount++;
        }
    }

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

    FakeSelectable SelectOne()
    {
        FakeSelectable selectable = new FakeSelectable();
        TestHelpers.SetPrivateField(_manager, "_selectable", selectable);
        return selectable;
    }

    [Test]
    public void SetInteraction_NotKeepingTheSelection_UnselectsTheSelectedUnit()
    {
        FakeSelectable selectable = SelectOne();

        _manager.SetInteraction(new FakeInteraction(), keepSelection: false);

        Assert.AreEqual(1, selectable.unselectCount);
    }

    // By default (e.g. a spell aimed): the panel of the selected unit stays open
    [Test]
    public void SetInteraction_TheUnitStaysSelected()
    {
        FakeSelectable selectable = SelectOne();
        FakeInteraction interaction = new FakeInteraction();

        _manager.SetInteraction(interaction);

        Assert.AreEqual(0, selectable.unselectCount);
        Assert.AreSame(interaction, _manager.GetInteraction());
        // Still the one closed by Close or Escape
        _manager.CancelSelection();
        Assert.AreEqual(1, selectable.unselectCount);
    }

    [Test]
    public void SetInteraction_StillCancelsThePreviousInteraction()
    {
        FakeInteraction previous = new FakeInteraction();
        _manager.SetInteraction(previous);

        _manager.SetInteraction(new FakeInteraction());

        Assert.AreEqual(1, previous.cancelCount);
    }
}

}
