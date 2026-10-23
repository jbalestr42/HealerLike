using System;
using UnityEngine.UIElements;

// The run map: the next room picked on it, or looked at while arranging the units. The run moves on its own.
public sealed class ToolkitMapPanel : IDisposable
{
    ToolkitGameContext _context;
    ToolkitGameView _view;
    ToolkitMapGraph _graph;
    Button _open;
    Button _close;
    Button _recenter;

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        Dispose();
        _context = context;
        _view = view;
        _graph = new ToolkitMapGraph(view.root.Q<ScrollView>("map-scroll"), Select);
        _open = view.root.Q<Button>("map-button");
        _close = view.root.Q<Button>("map-close-button");
        _recenter = view.root.Q<Button>("map-recenter-button");
        _open.clicked += Open;
        _close.clicked += OnClose;
        _recenter.clicked += _graph.Recenter;
    }

    public void Refresh()
    {
        if (_context == null || _view == null)
        {
            return;
        }

        RunState run = _context.ascension != null ? _context.ascension.run : null;
        bool visible = run != null && _context.IsCurrentView(GameScreen.Map);
        bool canSelect =
            visible && _context.ascension.canSelectRoom;
        _view.Show("map-panel", visible);
        _view.Show("map-button", run != null);
        _open.SetEnabled(CanOpen());
        _view.Show("map-close-button", visible && _context.IsPreparing());
        if (!visible)
        {
            _graph.Hide();
            return;
        }

        _view.SetText("map-title", canSelect ? "Choose your next room" : "Your expedition");
        _view.SetText(
            "map-progress",
            run.currentNode == null ? "THE JOURNEY BEGINS" : $"ROOM {run.currentFloor + 1} / {run.map.floorCount + 1}"
        );
        _view.SetText(
            "map-description",
            canSelect
                ? "Follow a bright path. Select a room to continue."
                : "Plan your route, then return to your party."
        );
        _graph.Display(run, canSelect);
    }

    bool CanOpen()
    {
        return _context != null
            && _context.ascension != null
            && _context.ascension.run != null
            && _context.IsCurrentView(GameScreen.Game)
            && _context.IsPreparing()
            && !_context.isPaused
            && !_context.isInventoryOpen
            && !_context.hasInteraction
            && _context.ascension.canLookAtMap;
    }

    void Open()
    {
        if (!CanOpen())
        {
            return;
        }

        _context.ascension.LookAtMap();
        Refresh();
    }

    void OnClose()
    {
        Close();
    }

    public bool Close()
    {
        if (_context == null || _context.ascension == null || !_context.IsCurrentView(GameScreen.Map) || !_context.IsPreparing())
        {
            return false;
        }

        _context.ascension.CloseMap();
        Refresh();
        return true;
    }

    void Select(MapNode node)
    {
        if (_context == null)
        {
            return;
        }

        RunState run = _context.ascension != null ? _context.ascension.run : null;
        // Recheck on click, including a queued second click after travel or a restarted run.
        if (
            run == null
            || !_context.IsCurrentView(GameScreen.Map)
            || !_context.ascension.canSelectRoom
            || !run.CanTravelTo(node)
        )
        {
            return;
        }

        _context.ascension.SelectRoom(node);
        Refresh();
    }

    public void Dispose()
    {
        if (_open != null)
        {
            _open.clicked -= Open;
        }

        if (_close != null)
        {
            _close.clicked -= OnClose;
        }

        if (_recenter != null && _graph != null)
        {
            _recenter.clicked -= _graph.Recenter;
        }

        if (_graph != null)
        {
            _graph.Dispose();
        }

        _graph = null;
        _open = _close = _recenter = null;
        _context = null;
        _view = null;
    }
}
