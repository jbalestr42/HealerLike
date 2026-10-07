using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitMobileLayout : IDisposable
{
    ToolkitGameView _view;
    ToolkitGameContext _context;
    ToolkitSafeArea _safeArea;
    ToolkitPopover _popover;
    Action _toggleFocus;
    bool _focused;
    public Rect normalizedWorldViewport => _view == null || _view.root.panel == null
        ? new Rect(.04f, .24f, .92f, .64f)
        : ToolkitScreenLayout.GetViewport(_view.root.Q("world-space").worldBound, _view.root.worldBound);

    public void Init(ToolkitGameView view, ToolkitGameContext context, UIDocument document)
    {
        Dispose(); _view = view; _context = context;
        _safeArea = new ToolkitSafeArea(view.root, document);
        _popover = new ToolkitPopover(view);
        view.AddClickListener("cancel-button", CancelInteraction);
        view.AddClickListener("focus-button", ToggleFocus);
        Update(); RefreshFocus();
    }
    public static PanelSettings CreatePanelSettings(PanelSettings source) => ToolkitTheme.CreatePanelSettings(source, null);
    public void Update(Func<Rect> safeAreaProvider = null)
    {
        Vector2 size = _safeArea.Update(safeAreaProvider);
        Resize(size.x, size.y);
    }
    public void Resize(float width, float height) { ToolkitResponsiveLayout.Apply(_view, width, height); }
    public void Refresh()
    {
        bool blocked = _context.isMenu || _context.isPaused || _context.isInventoryOpen
            || (_context.ui != null && !_context.IsCurrentView(ViewType.Game));
        if (blocked) { _view.CancelGestures(); CloseDrawers(); }
        _view.Show("cancel-button", _context.hasInteraction && !blocked);
        _view.SetButton("pause-button", null, !_context.isMenu
            && (_context.ui == null || _context.IsCurrentView(ViewType.Game)));
        // This control lives in Pause and must remain operable while paused.
        _view.SetButton("focus-button", null, !_context.isMenu);
    }
    public void SetBattleFocus(bool focused, Action toggle)
    { _focused = focused; _toggleFocus = toggle; RefreshFocus(); }
    public bool CloseDrawers()
    { bool open = _popover != null && _popover.isOpen; _popover?.Close(); return open; }
    void CancelInteraction()
    { _view.CancelGestures(); _context.interaction?.CancelInteraction(); Refresh(); }
    void ToggleFocus() { _toggleFocus?.Invoke(); }
    void RefreshFocus()
    {
        if (_view == null)
        {
            return;
        }

        _view.Show("focus-button", _toggleFocus != null);
        _view.SetButton("focus-button", _focused ? "Overview" : "Focus battle", !_context.isMenu);
    }
    public void Dispose()
    {
        if (_view != null)
        {
            _view.CancelGestures();
            _view.RemoveClickListener("cancel-button", CancelInteraction);
            _view.RemoveClickListener("focus-button", ToggleFocus);
        }
        _popover?.Dispose(); _popover = null;
        _safeArea?.Dispose(); _safeArea = null; _view = null;
    }
}
