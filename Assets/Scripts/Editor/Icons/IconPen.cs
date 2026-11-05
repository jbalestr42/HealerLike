using System;
using System.Collections.Generic;

// Draws a glyph in its own 0-100 box (y going down), placed and scaled on the canvas, with the three colours its frame
// gives it: the main one (the pictogram), the accent (its details, e.g. the half of a shield in shade) and the back
// (the frame's colour, for the cuts through the glyph, e.g. a crack)
public class IconPen
{
    public readonly IconCanvas canvas;
    public readonly IconColor main;
    public readonly IconColor accent;
    public readonly IconColor back;

    readonly float _x;
    readonly float _y;
    readonly float _scale;

    public IconPen(IconCanvas canvas, float x, float y, float size, IconColor main, IconColor accent, IconColor back)
    {
        this.canvas = canvas;
        this.main = main;
        this.accent = accent;
        this.back = back;
        _x = x;
        _y = y;
        _scale = size / 100f;
    }

    // Pairs of coordinates
    public void Poly(IconColor color, params float[] xy)
    {
        var points = new List<(float x, float y)>(xy.Length / 2);
        for (int i = 0; i + 1 < xy.Length; i += 2)
        {
            points.Add((X(xy[i]), Y(xy[i + 1])));
        }
        canvas.FillPolygon(points, color);
    }

    // A closed outline sampled along t from 0 to 1
    public void Curve(IconColor color, Func<float, (float x, float y)> point, int steps = 96)
    {
        var points = new List<(float x, float y)>(steps);
        for (int i = 0; i < steps; i++)
        {
            (float x, float y) = point(i / (float)steps);
            points.Add((X(x), Y(y)));
        }
        canvas.FillPolygon(points, color);
    }

    public void Circle(IconColor color, float cx, float cy, float radius)
    {
        canvas.FillCircle(X(cx), Y(cy), radius * _scale, color);
    }

    public void Ellipse(IconColor color, float cx, float cy, float rx, float ry)
    {
        canvas.FillEllipse(X(cx), Y(cy), rx * _scale, ry * _scale, color);
    }

    public void Ring(IconColor color, float cx, float cy, float radius, float thickness)
    {
        EllipseRing(color, cx, cy, radius, radius, thickness);
    }

    public void EllipseRing(IconColor color, float cx, float cy, float rx, float ry, float thickness)
    {
        canvas.FillEllipseRing(X(cx), Y(cy), rx * _scale, ry * _scale, thickness * _scale, color);
    }

    public void Line(IconColor color, float x0, float y0, float x1, float y1, float width)
    {
        canvas.FillLine(X(x0), Y(y0), X(x1), Y(y1), width * _scale, color);
    }

    public void Rect(IconColor color, float x, float y, float width, float height, float radius = 0f)
    {
        canvas.FillRoundRect(X(x), Y(y), width * _scale, height * _scale, radius * _scale, color);
    }

    float X(float x)
    {
        return _x + x * _scale;
    }

    float Y(float y)
    {
        return _y + y * _scale;
    }
}
