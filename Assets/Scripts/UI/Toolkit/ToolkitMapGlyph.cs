using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitMapGlyph : IDisposable
{
    readonly VisualElement _surface;
    readonly MapNodeType _type;
    float _lineWidth;

    public ToolkitMapGlyph(VisualElement surface, MapNodeType type)
    {
        _surface = surface;
        _type = type;
        _surface.generateVisualContent += Draw;
        _surface.RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        ReadStyle();
    }

    void ReadStyle()
    {
        _lineWidth = ToolkitStyleValues.ReadPositive(_surface.customStyle, "--map-glyph-stroke-width", 1.7f);
    }

    void OnStyleResolved(CustomStyleResolvedEvent evt)
    {
        ReadStyle();
        _surface.MarkDirtyRepaint();
    }

    public void Dispose()
    {
        _surface.generateVisualContent -= Draw;
        _surface.UnregisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        _surface.MarkDirtyRepaint();
    }

    void Draw(MeshGenerationContext context)
    {
        Painter2D p = context.painter2D;
        p.strokeColor = _surface.resolvedStyle.color;
        p.lineWidth = _lineWidth;
        switch (_type)
        {
            case MapNodeType.Rest:
                Stroke(p, 4, 11, 20, 11);
                Stroke(p, 12, 3, 12, 19);
                break;
            case MapNodeType.Treasure:
                p.BeginPath();
                p.MoveTo(Point(3, 7));
                p.LineTo(Point(21, 7));
                p.LineTo(Point(21, 19));
                p.LineTo(Point(3, 19));
                p.ClosePath();
                p.Stroke();
                Stroke(p, 3, 12, 21, 12);
                Stroke(p, 12, 10, 12, 15);
                break;
            case MapNodeType.Boss:
                p.BeginPath();
                p.MoveTo(Point(3, 5));
                p.LineTo(Point(7, 11));
                p.LineTo(Point(12, 3));
                p.LineTo(Point(17, 11));
                p.LineTo(Point(21, 5));
                p.LineTo(Point(19, 19));
                p.LineTo(Point(5, 19));
                p.ClosePath();
                p.Stroke();
                break;
            case MapNodeType.Event:
                DrawQuestionMark(p);
                break;
            case MapNodeType.Combat:
            case MapNodeType.Elite:
                Stroke(p, 4, 4, 20, 20);
                Stroke(p, 20, 4, 4, 20);
                Stroke(p, 3, 14, 9, 20);
                Stroke(p, 15, 20, 21, 14);
                if (_type == MapNodeType.Elite)
                {
                    Stroke(p, 9, 2, 15, 2);
                }

                break;
        }
    }

    Vector2 Point(float x, float y)
    {
        return new Vector2(x * _surface.contentRect.width / 24f, y * _surface.contentRect.height / 24f);
    }

    void Stroke(Painter2D p, float x1, float y1, float x2, float y2)
    {
        p.BeginPath();
        p.MoveTo(Point(x1, y1));
        p.LineTo(Point(x2, y2));
        p.Stroke();
    }

    // One stroked path: an arc over the top, then a curve that leaves the arc along its tangent and lands in the
    // stem. The dot is a filled circle, not a one-unit line.
    void DrawQuestionMark(Painter2D p)
    {
        float unit = Mathf.Min(_surface.contentRect.width, _surface.contentRect.height) / 24f;
        p.lineCap = LineCap.Round;
        p.lineJoin = LineJoin.Round;
        p.BeginPath();
        p.Arc(Point(12f, 8.5f), 4.5f * unit, 180f, 405f);
        p.BezierCurveTo(Point(14.1f, 12.8f), Point(12f, 13.4f), Point(12f, 15.5f));
        p.Stroke();
        p.fillColor = p.strokeColor;
        p.BeginPath();
        p.Arc(Point(12f, 19.6f), 1.1f * unit, 0f, 360f);
        p.Fill();
    }
}
