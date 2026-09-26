using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Retained map artwork. Topology is built once per RunMap; progress only restyles existing nodes.
public sealed class ToolkitMapGraph : IDisposable
{
    sealed class Room
    {
        public MapNode node;
        public Button button;
        public Label state;
        public Action activate;
    }

    readonly ScrollView _scroll;
    readonly Action<MapNode> _select;
    readonly List<Room> _rooms = new List<Room>();
    readonly VisualElement _canvas;
    readonly ToolkitMapConnections _connections;
    RunState _run;
    MapNode _current;
    RunMap _map;
    bool _canSelect;
    bool _visible;
    bool _disposed;
    float _width;
    IVisualElementScheduledItem _focus;

    public ToolkitMapGraph(ScrollView scroll, Action<MapNode> select)
    {
        _scroll = scroll;
        _select = select;
        _canvas = scroll.Q("map-canvas");
        _connections = new ToolkitMapConnections();
        _canvas.Add(_connections);
        _scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    public void Display(RunState run, bool canSelect)
    {
        if (_disposed || run == null) return;
        bool topologyChanged = !ReferenceEquals(_map, run.map);
        bool progressChanged = !ReferenceEquals(_run, run) || _current != run.currentNode;
        bool modeChanged = _canSelect != canSelect;
        bool opened = !_visible;
        _run = run;
        _map = run.map;
        _current = run.currentNode;
        _canSelect = canSelect;
        _visible = true;
        if (topologyChanged) Build();
        if (topologyChanged || progressChanged || modeChanged) RefreshStates();
        if (topologyChanged || progressChanged || opened) QueueFocus();
    }

    public void Hide()
    {
        _visible = false;
        _focus?.Pause();
    }

    void Build()
    {
        ClearRooms();
        foreach (MapNode node in _map.GetAllNodes())
        {
            Room room = new Room { node = node };
            room.button = new Button { name = $"map-node-{node.floor}-{node.column}", userData = node };
            room.button.AddToClassList("map-node");
            room.button.AddToClassList("room-" + node.type.ToString().ToLowerInvariant());
            room.button.tooltip = $"{MapView.GetNodeLabel(node.type)} · Room {node.floor + 1}\n"
                + ToolkitRunMapPresentation.Description(node.type);
            VisualElement glyph = new ToolkitMapGlyph(node.type);
            glyph.AddToClassList("map-node__glyph");
            room.button.Add(glyph);
            Label label = new Label(MapView.GetNodeLabel(node.type));
            label.AddToClassList("map-node__label");
            label.pickingMode = PickingMode.Ignore;
            room.button.Add(label);
            room.state = new Label();
            room.state.AddToClassList("map-node__state");
            room.state.pickingMode = PickingMode.Ignore;
            room.button.Add(room.state);
            room.activate = () =>
            {
                if (!_disposed && _visible && _canSelect && _run.CanTravelTo(node)) _select?.Invoke(node);
            };
            room.button.clicked += room.activate;
            _rooms.Add(room);
            _canvas.Add(room.button);
        }
        Resize();
    }

    void RefreshStates()
    {
        foreach (Room room in _rooms)
        {
            MapNodeState state = _run.GetNodeState(room.node);
            room.button.EnableInClassList("is-available", state == MapNodeState.Available);
            room.button.EnableInClassList("is-current", state == MapNodeState.Current);
            room.button.EnableInClassList("is-visited", state == MapNodeState.Visited);
            room.button.EnableInClassList("is-locked", state == MapNodeState.Locked);
            room.button.SetEnabled(_canSelect && state == MapNodeState.Available);
            room.state.text = ToolkitRunMapPresentation.StateLabel(state);
        }
        _connections.Display(_run, _width);
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (_run == null || !_visible) return;
        Resize();
        QueueFocus();
    }

    void Resize()
    {
        float width = _scroll.contentViewport.layout.width;
        if (float.IsNaN(width) || width <= 0f) width = 320f;
        Vector2 size = ToolkitRunMapPresentation.CanvasSize(_map, width);
        _width = size.x;
        _canvas.style.width = size.x;
        _canvas.style.height = size.y;
        foreach (Room room in _rooms)
        {
            float nodeWidth = ToolkitRunMapPresentation.NodeWidth(_map, _width);
            Vector2 position = ToolkitRunMapPresentation.NodeCenter(room.node, _map, _width);
            room.button.style.left = position.x - nodeWidth * 0.5f;
            room.button.style.top = position.y - ToolkitRunMapPresentation.NodeHeight * 0.5f;
            room.button.style.width = nodeWidth;
            room.button.style.height = ToolkitRunMapPresentation.NodeHeight;
        }
        _connections.Display(_run, _width);
    }

    void QueueFocus()
    {
        _focus?.Pause();
        _focus = _canvas.schedule.Execute(Recenter);
    }

    public void Recenter()
    {
        if (_disposed || !_visible || _run == null) return;
        Vector2 viewport = _scroll.contentViewport.layout.size;
        if (viewport.x <= 0f || viewport.y <= 0f || float.IsNaN(viewport.x) || float.IsNaN(viewport.y)) return;
        _scroll.scrollOffset = ToolkitRunMapPresentation.FocusOffset(_run,
            ToolkitRunMapPresentation.CanvasSize(_map, _width), viewport);
    }

    void ClearRooms()
    {
        foreach (Room room in _rooms)
        {
            room.button.clicked -= room.activate;
            room.button.RemoveFromHierarchy();
        }
        _rooms.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _focus?.Pause();
        _scroll.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        ClearRooms();
        _connections.RemoveFromHierarchy();
        _run = null;
        _map = null;
    }
}

// A separate visual under the node buttons keeps path painting independent from input handling.
public sealed class ToolkitMapConnections : VisualElement
{
    RunState _run;
    float _width;
    public int edgeCount { get; private set; }

    public ToolkitMapConnections()
    {
        name = "map-connections";
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = style.right = style.top = style.bottom = 0f;
        generateVisualContent += Draw;
    }

    public void Display(RunState run, float width)
    {
        _run = run;
        _width = width;
        edgeCount = 0;
        foreach (MapNode node in run.map.GetAllNodes()) edgeCount += node.next.Count;
        MarkDirtyRepaint();
    }

    void Draw(MeshGenerationContext context)
    {
        if (_run == null) return;
        Painter2D painter = context.painter2D;
        foreach (MapNode node in _run.map.GetAllNodes())
        {
            foreach (MapNode next in node.next)
            {
                bool travelled = _run.IsVisited(node) && _run.IsVisited(next);
                bool reachable = node == _run.currentNode;
                painter.strokeColor = travelled ? new Color(0.82f, 0.67f, 0.39f)
                    : reachable ? new Color(0.75f, 0.88f, 0.63f) : new Color(0.22f, 0.33f, 0.32f);
                painter.lineWidth = travelled || reachable ? 3f : 1.5f;
                Vector2 from = ToolkitRunMapPresentation.NodeCenter(node, _run.map, _width);
                Vector2 to = ToolkitRunMapPresentation.NodeCenter(next, _run.map, _width);
                from.y -= ToolkitRunMapPresentation.NodeHeight * 0.5f;
                to.y += ToolkitRunMapPresentation.NodeHeight * 0.5f;
                painter.BeginPath();
                painter.MoveTo(from);
                painter.BezierCurveTo(new Vector2(from.x, (from.y + to.y) * 0.5f),
                    new Vector2(to.x, (from.y + to.y) * 0.5f), to);
                painter.Stroke();
            }
        }
    }
}

sealed class ToolkitMapGlyph : VisualElement
{
    readonly MapNodeType _type;
    public ToolkitMapGlyph(MapNodeType type)
    {
        _type = type;
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
    }

    void Draw(MeshGenerationContext context)
    {
        Painter2D p = context.painter2D;
        p.strokeColor = resolvedStyle.color;
        p.lineWidth = 1.7f;
        switch (_type)
        {
            case MapNodeType.Rest:
                Stroke(p, 4, 11, 20, 11); Stroke(p, 12, 3, 12, 19);
                break;
            case MapNodeType.Treasure:
                p.BeginPath(); p.MoveTo(new Vector2(3, 7)); p.LineTo(new Vector2(21, 7));
                p.LineTo(new Vector2(21, 19)); p.LineTo(new Vector2(3, 19)); p.ClosePath(); p.Stroke();
                Stroke(p, 3, 12, 21, 12); Stroke(p, 12, 10, 12, 15);
                break;
            case MapNodeType.Boss:
                p.BeginPath(); p.MoveTo(new Vector2(3, 5)); p.LineTo(new Vector2(7, 11));
                p.LineTo(new Vector2(12, 3)); p.LineTo(new Vector2(17, 11));
                p.LineTo(new Vector2(21, 5)); p.LineTo(new Vector2(19, 19));
                p.LineTo(new Vector2(5, 19)); p.ClosePath(); p.Stroke();
                break;
            default:
                Stroke(p, 4, 4, 20, 20); Stroke(p, 20, 4, 4, 20);
                Stroke(p, 3, 14, 9, 20); Stroke(p, 15, 20, 21, 14);
                if (_type == MapNodeType.Elite) Stroke(p, 9, 2, 15, 2);
                break;
        }
    }

    static void Stroke(Painter2D p, float x1, float y1, float x2, float y2)
    {
        p.BeginPath(); p.MoveTo(new Vector2(x1, y1)); p.LineTo(new Vector2(x2, y2)); p.Stroke();
    }
}
