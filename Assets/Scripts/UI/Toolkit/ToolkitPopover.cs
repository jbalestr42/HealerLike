using System;
using UnityEngine;
using UnityEngine.UIElements;

// Local reading surface. It never participates in world-space layout or camera fitting.
public sealed class ToolkitPopover : IDisposable
{
    readonly ToolkitGameView _view;
    readonly VisualElement _panel;
    readonly VisualElement _hud;
    public bool isOpen { get; private set; }
    Rect _anchor;
    public ToolkitPopover(ToolkitGameView view)
    {
        _view = view; _panel = view.root.Q("detail-panel"); _hud = view.root.Q("hud-root");
        view.OnInspectRequested.AddListener(Open);
        view.OnClosePopover += Close;
        view.AddClickListener("detail-close-button", Close);
        view.root.RegisterCallback<PointerDownEvent>(Outside, TrickleDown.TrickleDown);
        _panel.RegisterCallback<GeometryChangedEvent>(Geometry);
    }
    public void Open(ToolkitCardModel model)
    {
        if (model == null)
        {
            return;
        }

        _anchor = _view.inspectAnchor;
        isOpen = true;
        _view.OnInspect.Invoke(model);
        _view.Show("detail-panel", true);
        // The panel precedes the modal layers in the document, so without this the class screen covers it
        _panel.BringToFront();
        _view.Show("detail-actions", model.source is Entity);
        Place();
    }
    void Geometry(GeometryChangedEvent evt) { if (isOpen)
        {
            Place();
        }
    }
    void Place()
    {
        Rect bounds = _hud.worldBound;
        float left = bounds.xMin + _hud.resolvedStyle.paddingLeft;
        float right = bounds.xMax - _hud.resolvedStyle.paddingRight;
        float top = bounds.yMin + _hud.resolvedStyle.paddingTop;
        VisualElement topBar = _view.root.Q("top-bar");
        if (IsShown(topBar))
        {
            top = Mathf.Max(top, topBar.worldBound.yMax + 8);
        }

        // The menu hides the party panel, whose bound would read as zero and push the popover off the screen
        float bottom = bounds.yMax - _hud.resolvedStyle.paddingBottom;
        VisualElement partyPanel = _view.root.Q("party-panel");
        if (IsShown(partyPanel))
        {
            bottom = partyPanel.worldBound.yMin - 8;
        }
        float width = Mathf.Min(280, right - left);
        float height = Mathf.Min(240, Mathf.Max(100, bottom - top));
        _panel.style.width = width;
        _panel.style.maxHeight = height;
        float actual = float.IsNaN(_panel.resolvedStyle.height) ? height : Mathf.Min(height, _panel.resolvedStyle.height);
        float x = _anchor.width > 0 ? _anchor.center.x - width * 0.5f : left;
        float y = _anchor.width > 0 ? _anchor.yMin - actual - 12 : bottom - actual;
        _panel.style.left = Mathf.Clamp(x, left, Mathf.Max(left, right - width)) - bounds.xMin;
        _panel.style.top = Mathf.Clamp(y, top, Mathf.Max(top, bottom - actual)) - bounds.yMin;
    }
    static bool IsShown(VisualElement element)
    {
        if (element == null || element.resolvedStyle.display == DisplayStyle.None)
        {
            return false;
        }

        Rect bound = element.worldBound;
        return !float.IsNaN(bound.yMin) && !float.IsNaN(bound.yMax) && bound.height > 0f;
    }

    void Outside(PointerDownEvent evt)
    {
        if (!isOpen || _panel.Contains(evt.target as VisualElement))
        {
            return;
        }

        Close();
    }
    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        _view.Show("detail-panel", false);
        _view.OnInspectEnded.Invoke();
    }
    public void Dispose()
    {
        Close();
        _view.OnInspectRequested.RemoveListener(Open);
        _view.OnClosePopover -= Close;
        _view.RemoveClickListener("detail-close-button", Close);
        _view.root.UnregisterCallback<PointerDownEvent>(Outside, TrickleDown.TrickleDown);
        _panel.UnregisterCallback<GeometryChangedEvent>(Geometry);
    }
}
