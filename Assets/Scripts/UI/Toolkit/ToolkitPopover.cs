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
    // Only a new size (content, wrapping) needs a new place: Place moving the panel raises this event too, and
    // reacting to that move loops the layout
    void Geometry(GeometryChangedEvent evt)
    {
        if (isOpen && !Approximately(evt.oldRect.size, evt.newRect.size))
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
        SetLength(_panel.style.width, width, value => _panel.style.width = value);
        SetLength(_panel.style.maxHeight, height, value => _panel.style.maxHeight = value);
        float actual = float.IsNaN(_panel.resolvedStyle.height) ? height : Mathf.Min(height, _panel.resolvedStyle.height);
        Vector2 position = GetPosition(_anchor, new Vector2(width, actual), Rect.MinMaxRect(left, top, right, bottom));
        SetLength(_panel.style.left, position.x - bounds.xMin, value => _panel.style.left = value);
        SetLength(_panel.style.top, position.y - bounds.yMin, value => _panel.style.top = value);
    }

    // Rewriting a value the layout rounded to the pixel grid dirties it again: only a real change is written
    static void SetLength(StyleLength current, float value, Action<float> set)
    {
        if (current.keyword != StyleKeyword.Undefined || Mathf.Abs(current.value.value - value) > 0.5f)
        {
            set(value);
        }
    }

    static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) <= 0.5f && Mathf.Abs(a.y - b.y) <= 0.5f;
    }

    // Above its anchor, or below it when there is no room above (e.g. the item bar under the header): a popover
    // covering its anchor takes the pointer from it, closing on the leave and reopening on the enter, in a loop.
    // Without an anchor, at the bottom left of the area
    public static Vector2 GetPosition(Rect anchor, Vector2 size, Rect area)
    {
        const float gap = 12;
        float x = anchor.width > 0 ? anchor.center.x - size.x * 0.5f : area.xMin;
        float y = area.yMax - size.y;
        if (anchor.width > 0)
        {
            y = anchor.yMin - size.y - gap;
            if (y < area.yMin && anchor.yMax + gap + size.y <= area.yMax)
            {
                y = anchor.yMax + gap;
            }
        }

        return new Vector2(Mathf.Clamp(x, area.xMin, Mathf.Max(area.xMin, area.xMax - size.x)),
            Mathf.Clamp(y, area.yMin, Mathf.Max(area.yMin, area.yMax - size.y)));
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
