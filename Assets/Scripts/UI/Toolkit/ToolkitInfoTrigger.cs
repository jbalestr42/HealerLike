using System;
using UnityEngine.UIElements;

// Hovering an element with a mouse, or tapping it, opens what it stands for in the ToolkitPopover through the view's
// OnInspectRequested, the route a creature in the field takes; the popover closes as the mouse leaves it, or on a tap
// outside it. Nothing opens while the model is null (e.g. a resource the character doesn't have)
public sealed class ToolkitInfoTrigger : IDisposable
{
    readonly ToolkitGameView _view;
    readonly VisualElement _element;
    readonly Func<ToolkitCardModel> _getModel;

    public ToolkitInfoTrigger(ToolkitGameView view, VisualElement element, Func<ToolkitCardModel> getModel)
    {
        _view = view;
        _element = element;
        _getModel = getModel;
        _element.pickingMode = PickingMode.Position;
        _element.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        _element.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        _element.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _element.RegisterCallback<ClickEvent>(OnClick);
    }

    public VisualElement element => _element;

    // False when there is nothing to show
    public bool Open()
    {
        ToolkitCardModel model = _getModel();
        if (model == null)
        {
            return false;
        }

        _view.inspectAnchor = _element.worldBound;
        _view.OnInspectRequested.Invoke(model);
        return true;
    }

    public void Dispose()
    {
        _element.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        _element.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        _element.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        _element.UnregisterCallback<ClickEvent>(OnClick);
    }

    void OnPointerEnter(PointerEnterEvent evt)
    {
        if (!IsTouch(evt.pointerType))
        {
            Open();
        }
    }

    // A touch keeps the popover open until a tap outside it
    void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (!IsTouch(evt.pointerType))
        {
            _view.ClosePopover();
        }
    }

    // A touch opens on its press, a mouse on its hover
    public static bool IsTouch(string pointerType)
    {
        return pointerType == PointerType.touch || pointerType == PointerType.pen;
    }

    // The press ends here: an element inside a card's button reads its details instead of activating the card
    void OnPointerDown(PointerDownEvent evt)
    {
        evt.StopPropagation();
        Open();
    }

    void OnClick(ClickEvent evt)
    {
        evt.StopPropagation();
    }
}
