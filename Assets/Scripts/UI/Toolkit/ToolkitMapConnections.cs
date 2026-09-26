using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitMapConnections : IDisposable
{
    readonly VisualElement _surface;
    RunState _run;
    float _width;
    ToolkitMapMetrics _metrics;
    Color _travelled;
    Color _reachable;
    Color _locked;
    float _lineWidth;
    float _activeWidth;
    public int edgeCount { get; private set; }
    public int paintedEdgeCount { get; private set; }

    bool _disposed;

    public ToolkitMapConnections(VisualElement surface)
    {
        _surface = surface;
        _surface.userData = this;
        _surface.generateVisualContent += Draw;
        _surface.RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        ReadStyle();
    }

    void OnStyleResolved(CustomStyleResolvedEvent evt)
    {
        ReadStyle();
        _surface.MarkDirtyRepaint();
    }

    void ReadStyle()
    {
        ICustomStyle style = _surface.customStyle;
        style.TryGetValue(new CustomStyleProperty<Color>("--map-path-travelled"), out _travelled);
        style.TryGetValue(new CustomStyleProperty<Color>("--map-path-reachable"), out _reachable);
        style.TryGetValue(new CustomStyleProperty<Color>("--map-path-locked"), out _locked);
        _lineWidth = ToolkitStyleValues.ReadPositive(style, "--map-path-width", 1.5f);
        _activeWidth = ToolkitStyleValues.ReadPositive(style, "--map-path-active-width", 3f);
    }

    public void Display(RunState run, float width, ToolkitMapMetrics metrics)
    {
        if (_disposed)
        {
            return;
        }

        paintedEdgeCount = 0;
        _run = run;
        _width = width;
        _metrics = metrics;
        edgeCount = 0;
        foreach (MapNode node in run.map.GetAllNodes())
        {
            edgeCount += node.next.Count;
        }

        _surface.MarkDirtyRepaint();
    }

    public void Dispose()
    {
        _disposed = true;
        paintedEdgeCount = 0;
        if (ReferenceEquals(_surface.userData, this))
        {
            _surface.userData = null;
        }

        _surface.generateVisualContent -= Draw;
        _surface.UnregisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        _run = null;
        edgeCount = 0;
        _surface.MarkDirtyRepaint();
    }

    void Draw(MeshGenerationContext context)
    {
        paintedEdgeCount = 0;
        if (_run == null)
        {
            return;
        }

        Painter2D painter = context.painter2D;
        foreach (MapNode node in _run.map.GetAllNodes())
        {
            foreach (MapNode next in node.next)
            {
                bool travelled = _run.IsVisited(node) && _run.IsVisited(next);
                bool reachable = node == _run.currentNode;
                painter.strokeColor =
                    travelled ? _travelled
                    : reachable ? _reachable
                    : _locked;
                painter.lineWidth = travelled || reachable ? _activeWidth : _lineWidth;
                Vector2 from = ToolkitRunMapPresentation.NodeCenter(node, _run.map, _width, _metrics);
                Vector2 to = ToolkitRunMapPresentation.NodeCenter(next, _run.map, _width, _metrics);
                from.y -= _metrics.nodeHeight * 0.5f;
                to.y += _metrics.nodeHeight * 0.5f;
                painter.BeginPath();
                painter.MoveTo(from);
                painter.BezierCurveTo(
                    new Vector2(from.x, (from.y + to.y) * 0.5f),
                    new Vector2(to.x, (from.y + to.y) * 0.5f),
                    to
                );
                painter.Stroke();
                paintedEdgeCount++;
            }
        }
    }
}
