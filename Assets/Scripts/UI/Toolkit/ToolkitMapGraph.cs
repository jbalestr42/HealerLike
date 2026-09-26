using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Retained map artwork. Topology is built once per RunMap; progress only restyles existing nodes.
public class ToolkitMapGraph : IDisposable
{
    readonly ScrollView _scroll;
    readonly Action<MapNode> _select;
    readonly List<ToolkitMapNode> _rooms = new List<ToolkitMapNode>();
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
    readonly VisualTreeAsset _nodeTemplate = Resources.Load<VisualTreeAsset>("UI/Toolkit/Controls/MapNode");
    ToolkitMapMetrics _metrics = ToolkitMapMetrics.defaults;
    bool _isValid;
    public int connectionCount
    {
        get { return _connections != null ? _connections.edgeCount : 0; }
    }

    public ToolkitMapGraph(ScrollView scroll, Action<MapNode> select)
    {
        _scroll = scroll;
        _select = select;
        if (
            !ToolkitTemplates.Require(scroll, "map-canvas", out _canvas)
            || !ToolkitTemplates.Require(_canvas, "map-connections", out VisualElement surface)
        )
        {
            return;
        }

        _connections = new ToolkitMapConnections(surface);
        _canvas.RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        _scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        _isValid = true;
    }

    void OnStyleResolved(CustomStyleResolvedEvent evt)
    {
        _metrics = ToolkitMapMetrics.Read(_canvas.customStyle);
        if (_run != null)
        {
            Resize();
            QueueFocus();
        }
    }

    public void Display(RunState run, bool canSelect)
    {
        if (_disposed || !_isValid || run == null)
        {
            return;
        }

        bool topologyChanged = !ReferenceEquals(_map, run.map);
        bool progressChanged = !ReferenceEquals(_run, run) || _current != run.currentNode;
        bool modeChanged = _canSelect != canSelect;
        bool opened = !_visible;
        _run = run;
        _map = run.map;
        _current = run.currentNode;
        _canSelect = canSelect;
        _visible = true;
        if (topologyChanged)
        {
            Build();
        }

        if (!_isValid)
        {
            return;
        }

        if (topologyChanged || progressChanged || modeChanged)
        {
            RefreshStates();
        }

        if (topologyChanged || progressChanged || opened)
        {
            QueueFocus();
        }
    }

    public void Hide()
    {
        _visible = false;
        if (_focus != null)
        {
            _focus.Pause();
        }
    }

    void Build()
    {
        ClearRooms();
        foreach (MapNode node in _map.GetAllNodes())
        {
            ToolkitMapNode room = new ToolkitMapNode(_nodeTemplate, node, Select);
            if (room.button == null)
            {
                ClearRooms();
                _isValid = false;
                return;
            }

            _rooms.Add(room);
            _canvas.Add(room.button);
        }

        Resize();
    }

    void Select(MapNode node)
    {
        if (!_disposed && _visible && _canSelect && _run.CanTravelTo(node) && _select != null)
        {
            _select.Invoke(node);
        }
    }

    void RefreshStates()
    {
        foreach (ToolkitMapNode room in _rooms)
        {
            room.Display(_run.GetNodeState(room.node), _canSelect);
        }

        _connections.Display(_run, _width, _metrics);
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (_run == null || !_visible)
        {
            return;
        }

        Resize();
        QueueFocus();
    }

    void Resize()
    {
        float width = _scroll.contentViewport.layout.width;
        if (float.IsNaN(width) || width <= 0f)
        {
            width = 320f;
        }

        Vector2 size = ToolkitRunMapPresentation.CanvasSize(_map, width, _metrics);
        _width = size.x;
        _canvas.style.width = size.x;
        _canvas.style.height = size.y;
        foreach (ToolkitMapNode room in _rooms)
        {
            float nodeWidth = ToolkitRunMapPresentation.NodeWidth(_map, _width, _metrics);
            Vector2 position = ToolkitRunMapPresentation.NodeCenter(room.node, _map, _width, _metrics);
            room.button.style.left = position.x - nodeWidth * 0.5f;
            room.button.style.top = position.y - _metrics.nodeHeight * 0.5f;
            room.button.style.width = nodeWidth;
            room.button.style.height = _metrics.nodeHeight;
        }

        _connections.Display(_run, _width, _metrics);
    }

    void QueueFocus()
    {
        if (_focus != null)
        {
            _focus.Pause();
        }

        _focus = _canvas.schedule.Execute(Recenter);
    }

    public void Recenter()
    {
        if (_disposed || !_visible || _run == null)
        {
            return;
        }

        Vector2 viewport = _scroll.contentViewport.layout.size;
        if (viewport.x <= 0f || viewport.y <= 0f || float.IsNaN(viewport.x) || float.IsNaN(viewport.y))
        {
            return;
        }

        _scroll.scrollOffset = ToolkitRunMapPresentation.FocusOffset(
            _run,
            ToolkitRunMapPresentation.CanvasSize(_map, _width, _metrics),
            viewport,
            _metrics
        );
    }

    void ClearRooms()
    {
        foreach (ToolkitMapNode room in _rooms)
        {
            room.Dispose();
        }

        _rooms.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_focus != null)
        {
            _focus.Pause();
        }

        if (_scroll != null)
        {
            _scroll.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        if (_canvas != null)
        {
            _canvas.UnregisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        }

        ClearRooms();
        if (_connections != null)
        {
            _connections.Dispose();
        }

        _run = null;
        _map = null;
    }
}
