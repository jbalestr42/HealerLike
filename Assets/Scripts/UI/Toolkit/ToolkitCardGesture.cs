using System;
using UnityEngine;
using UnityEngine.UIElements;

// Captures compact-row pointers before Button/ScrollView resolve them. Scroll is still the
// ScrollView's own offset, but only the resolved horizontal owner is allowed to change it.
public sealed class ToolkitCardGesture : IDisposable
{
    readonly ToolkitCard _card;
    readonly ToolkitGameView _view;
    readonly Button _button;
    readonly ToolkitPress _press = new ToolkitPress();
    readonly Action _activate;
    IVisualElementScheduledItem _timer;
    ToolkitCardModel _pressed;
    Vector2 _point;
    Vector2 _scrollStart;
    Vector2 _down;
    ScrollView _scroll;
    bool _holding;
    bool _dragging;
    bool _navigationInspect;
    int _escapeFrame = -1;

    public ToolkitCardGesture(ToolkitCard card, ToolkitGameView view, Action activate)
    {
        _card = card; _view = view; _button = card.button; _activate = activate;
        _button.clickable = null;
        _button.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
        _button.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
        _button.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
        _button.RegisterCallback<PointerCancelEvent>(CancelEvent);
        _button.RegisterCallback<PointerCaptureOutEvent>(Lost);
        _button.RegisterCallback<DetachFromPanelEvent>(Detached);
        _button.RegisterCallback<NavigationSubmitEvent>(Submit);
        _button.RegisterCallback<KeyDownEvent>(Key);
        _button.RegisterCallback<NavigationCancelEvent>(InspectNavigation);
        _view.OnInspectEnded.AddListener(InspectionClosed);
        _view.OnGesturesCancelled += Cancel;
    }

    void Down(PointerDownEvent evt)
    {
        if (evt.button != 0 || _card.model == null)
        {
            return;
        }
        // Legacy input may synthesize Down from an orphaned Ended sample.
        if (!(_view.canBeginPointer?.Invoke(evt.pointerId) ?? true))
        { evt.StopImmediatePropagation(); return; }
        _pressed = _card.model;
        if (!_press.Begin(evt.pointerId, evt.position, Time.realtimeSinceStartup, _pressed.canDrag))
        {
            return;
        }

        _point = _down = evt.position;
        _scroll = _button.GetFirstAncestorOfType<ScrollView>();
        _scrollStart = _scroll != null ? _scroll.scrollOffset : Vector2.zero;
        _button.CapturePointer(evt.pointerId);
        _timer = _button.schedule.Execute(Tick).Every(16);
        evt.StopImmediatePropagation();
    }

    void Move(PointerMoveEvent evt)
    {
        if (evt.pointerId != _press.pointer)
        {
            return;
        }

        _point = evt.position;
        Resolve();
        evt.StopImmediatePropagation();
    }

    void Tick() { if (_press.pointer >= 0)
        {
            Resolve();
        }
    }

    void Resolve()
    {
        ToolkitPress.Owner owner = _press.Move(_press.pointer, _point, Time.realtimeSinceStartup);
        if (owner == ToolkitPress.Owner.Hold && !_holding)
        {
            _holding = true;
            _view.inspectAnchor = _button.worldBound;
            _view.OnInspectRequested.Invoke(_pressed);
        }
        if (owner == ToolkitPress.Owner.Scroll && _scroll != null)
        {
            _scroll.scrollOffset = _scrollStart + new Vector2(_down.x - _point.x, 0);
        }

        if (owner == ToolkitPress.Owner.Drag)
        {
            if (!_dragging)
            {
                _dragging = (_pressed.canBeginDrag?.Invoke() ?? true)
                    && _view.rosterDrag != null && _view.rosterDrag.Begin(
                    _pressed.source as EntityData, ScreenPoint(_point), _pressed.deployed);
                if (!_dragging) { Cancel(); return; }
                _view.ClosePopover();
            }
            _view.rosterDrag.Move(ScreenPoint(_point));
        }
    }

    void Up(PointerUpEvent evt)
    {
        if (evt.pointerId != _press.pointer)
        {
            return;
        }

        _point = evt.position;
        Resolve();
        ToolkitPress.Owner owner = _press.End(evt.pointerId, _point, Time.realtimeSinceStartup);
        if (_dragging)
        {
            if (_pressed.canBeginDrag?.Invoke() ?? true)
            {
                _view.rosterDrag?.End(ScreenPoint(_point));
            }
            else
            {
                _view.rosterDrag?.Cancel();
            }
        }
        _dragging = false;
        if (_holding && _pressed.source is CharacterSkillSlot)
        {
            _view.ClosePopover();
        }
        // The captured source must still represent this entry after periodic HUD refreshes.
        if (owner == ToolkitPress.Owner.Tap && _button.worldBound.Contains(_point)
            && _card.model?.key == _pressed.key)
        { _view.inspectAnchor = _button.worldBound; _activate(); }
        Release(evt.pointerId);
        evt.StopImmediatePropagation();
    }

    Vector2 ScreenPoint(Vector2 point)
    {
        if (_view.screenPointProvider != null)
        {
            return _view.screenPointProvider(point);
        }

        Vector2 origin = RuntimePanelUtils.ScreenToPanel(_button.panel, Vector2.zero);
        Vector2 end = RuntimePanelUtils.ScreenToPanel(_button.panel, new Vector2(Screen.width, Screen.height));
        return new Vector2((point.x - origin.x) / (end.x - origin.x) * Screen.width,
            Screen.height - (point.y - origin.y) / (end.y - origin.y) * Screen.height);
    }

    void Submit(NavigationSubmitEvent evt)
    { _view.ClosePopover(); _activate(); evt.StopPropagation(); }
    void InspectionClosed() { _navigationInspect = false; }
    // Both UI input modules already map controller Cancel (B / Circle). On a focused
    // card it toggles inspection; Submit (A / Cross) keeps its activation meaning.
    void InspectNavigation(NavigationCancelEvent evt)
    {
        // Cancel also maps to Escape. Its one cancel/pause action belongs to
        // ToolkitGameActions.Update, irrespective of MonoBehaviour update order.
        // Standalone reports Unknown device type; PanelEventHandler sends KeyDown
        // before NavigationCancel, so the stamp also covers synthetic/backend keys.
        if (Input.GetKeyDown(KeyCode.Escape) || _escapeFrame == Time.frameCount)
        {
            return;
        }

        if (_navigationInspect)
        {
            _view.ClosePopover();
        }
        else
        {
            _view.ClosePopover();
            _navigationInspect = true;
            Inspect();
        }
        evt.StopPropagation();
    }
    void Inspect()
    {
        _view.inspectAnchor = _button.worldBound;
        _view.OnInspectRequested.Invoke(_card.model);
    }
    void Key(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape)
        {
            _escapeFrame = Time.frameCount;
            return;
        }
        if (evt.keyCode != KeyCode.I && evt.keyCode != KeyCode.F1)
        {
            return;
        }

        Inspect();
        evt.StopPropagation();
    }
    void CancelEvent(PointerCancelEvent evt) { if (evt.pointerId == _press.pointer)
        {
            Cancel();
        }
    }
    void Lost(PointerCaptureOutEvent evt) { if (evt.pointerId == _press.pointer)
        {
            Cancel();
        }
    }
    void Detached(DetachFromPanelEvent evt) { Cancel(); }
    void Release(int pointer)
    {
        _timer?.Pause(); _timer = null; _holding = false; _pressed = null;
        if (pointer >= 0 && _button.HasPointerCapture(pointer))
        {
            _button.ReleasePointer(pointer);
        }
    }
    public void Cancel()
    {
        int pointer = _press.pointer;
        _press.Cancel();
        if (_dragging)
        {
            _view.rosterDrag?.Cancel();
        }

        if (_holding && _pressed?.source is CharacterSkillSlot)
        {
            _view.ClosePopover();
        }

        _dragging = false;
        Release(pointer);
    }
    public void Dispose()
    {
        Cancel();
        _view.OnGesturesCancelled -= Cancel;
        _button.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
        _button.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
        _button.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
        _button.UnregisterCallback<PointerCancelEvent>(CancelEvent);
        _button.UnregisterCallback<PointerCaptureOutEvent>(Lost);
        _button.UnregisterCallback<DetachFromPanelEvent>(Detached);
        _button.UnregisterCallback<NavigationSubmitEvent>(Submit);
        _button.UnregisterCallback<KeyDownEvent>(Key);
        _button.UnregisterCallback<NavigationCancelEvent>(InspectNavigation);
        _view.OnInspectEnded.RemoveListener(InspectionClosed);
    }
}
