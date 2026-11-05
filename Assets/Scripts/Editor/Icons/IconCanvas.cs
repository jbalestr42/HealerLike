using System;
using System.Collections.Generic;

// A colour of the painted icons, straight (not premultiplied) alpha
public struct IconColor
{
    public float r, g, b, a;

    public IconColor(float r, float g, float b, float a = 1f)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    // "#RRGGBB" or "#RRGGBBAA"
    public static IconColor Hex(string hex)
    {
        string digits = hex.TrimStart('#');
        float Channel(int index) => Convert.ToInt32(digits.Substring(index * 2, 2), 16) / 255f;
        return new IconColor(Channel(0), Channel(1), Channel(2), digits.Length >= 8 ? Channel(3) : 1f);
    }

    // Towards white (amount 1 = white)
    public IconColor Lighten(float amount)
    {
        return new IconColor(r + (1f - r) * amount, g + (1f - g) * amount, b + (1f - b) * amount, a);
    }

    // Towards black (amount 1 = black)
    public IconColor Darken(float amount)
    {
        return new IconColor(r * (1f - amount), g * (1f - amount), b * (1f - amount), a);
    }
}

// A small antialiased painter for the flat icons (see IconPainter): each shape fills the samples of a supersampled
// grid it covers, averaged down to the image's pixels, so the icons are drawn from code, reproducible and editable.
// Coordinates are in the image's pixels, y going down
public class IconCanvas
{
    const int Samples = 4;

    readonly int _size;
    readonly int _grid;
    // Premultiplied by alpha
    readonly float[] _r, _g, _b, _a;

    public int size => _size;

    public IconCanvas(int size)
    {
        _size = size;
        _grid = size * Samples;
        int count = _grid * _grid;
        _r = new float[count];
        _g = new float[count];
        _b = new float[count];
        _a = new float[count];
    }

    public void FillCircle(float cx, float cy, float radius, IconColor color)
    {
        FillEllipse(cx, cy, radius, radius, color);
    }

    public void FillEllipse(float cx, float cy, float rx, float ry, IconColor color)
    {
        Fill(cx - rx, cy - ry, cx + rx, cy + ry, (x, y) => InEllipse(x, y, cx, cy, rx, ry), color);
    }

    // The band between the ellipse and the one thickness inside it
    public void FillEllipseRing(float cx, float cy, float rx, float ry, float thickness, IconColor color)
    {
        float innerX = rx - thickness;
        float innerY = ry - thickness;
        Fill(cx - rx, cy - ry, cx + rx, cy + ry,
            (x, y) => InEllipse(x, y, cx, cy, rx, ry) && (innerX <= 0f || innerY <= 0f || !InEllipse(x, y, cx, cy, innerX, innerY)),
            color);
    }

    // A stroke with round ends
    public void FillLine(float x0, float y0, float x1, float y1, float width, IconColor color)
    {
        float half = width / 2f;
        Fill(System.Math.Min(x0, x1) - half, System.Math.Min(y0, y1) - half, System.Math.Max(x0, x1) + half, System.Math.Max(y0, y1) + half,
            (x, y) => SegmentDistanceSquared(x, y, x0, y0, x1, y1) <= half * half, color);
    }

    public void FillRoundRect(float x, float y, float width, float height, float radius, IconColor color)
    {
        float r = System.Math.Min(radius, System.Math.Min(width, height) / 2f);
        Fill(x, y, x + width, y + height, (px, py) =>
        {
            float cx = System.Math.Max(x + r, System.Math.Min(px, x + width - r));
            float cy = System.Math.Max(y + r, System.Math.Min(py, y + height - r));
            return (px - cx) * (px - cx) + (py - cy) * (py - cy) <= r * r;
        }, color);
    }

    // Even-odd rule, so a polygon crossing itself leaves its overlaps empty
    public void FillPolygon(IList<(float x, float y)> points, IconColor color)
    {
        if (points == null || points.Count < 3)
        {
            return;
        }

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach ((float x, float y) in points)
        {
            minX = System.Math.Min(minX, x);
            minY = System.Math.Min(minY, y);
            maxX = System.Math.Max(maxX, x);
            maxY = System.Math.Max(maxY, y);
        }
        Fill(minX, minY, maxX, maxY, (x, y) => InPolygon(points, x, y), color);
    }

    // The colour of a pixel, alpha unpremultiplied
    public IconColor GetPixel(int x, int y)
    {
        float r = 0f, g = 0f, b = 0f, a = 0f;
        for (int sy = 0; sy < Samples; sy++)
        {
            for (int sx = 0; sx < Samples; sx++)
            {
                int index = (y * Samples + sy) * _grid + x * Samples + sx;
                r += _r[index];
                g += _g[index];
                b += _b[index];
                a += _a[index];
            }
        }

        const float count = Samples * Samples;
        if (a <= 0f)
        {
            return new IconColor(0f, 0f, 0f, 0f);
        }
        return new IconColor(r / a, g / a, b / a, a / count);
    }

    // RGBA bytes, row by row, the bottom row first when flipped (as Texture2D wants them)
    public byte[] ToRgba32(bool bottomRowFirst)
    {
        byte[] bytes = new byte[_size * _size * 4];
        for (int y = 0; y < _size; y++)
        {
            int row = bottomRowFirst ? _size - 1 - y : y;
            for (int x = 0; x < _size; x++)
            {
                IconColor color = GetPixel(x, y);
                int index = (row * _size + x) * 4;
                bytes[index] = ToByte(color.r);
                bytes[index + 1] = ToByte(color.g);
                bytes[index + 2] = ToByte(color.b);
                bytes[index + 3] = ToByte(color.a);
            }
        }
        return bytes;
    }

    // Blends the colour over every sample of the box whose centre is inside
    void Fill(float minX, float minY, float maxX, float maxY, Func<float, float, bool> inside, IconColor color)
    {
        int x0 = ClampToGrid((int)System.Math.Floor(minX * Samples));
        int x1 = ClampToGrid((int)System.Math.Ceiling(maxX * Samples));
        int y0 = ClampToGrid((int)System.Math.Floor(minY * Samples));
        int y1 = ClampToGrid((int)System.Math.Ceiling(maxY * Samples));
        float keep = 1f - color.a;
        for (int gy = y0; gy < y1; gy++)
        {
            float y = (gy + 0.5f) / Samples;
            for (int gx = x0; gx < x1; gx++)
            {
                float x = (gx + 0.5f) / Samples;
                if (!inside(x, y))
                {
                    continue;
                }

                int index = gy * _grid + gx;
                _r[index] = color.r * color.a + _r[index] * keep;
                _g[index] = color.g * color.a + _g[index] * keep;
                _b[index] = color.b * color.a + _b[index] * keep;
                _a[index] = color.a + _a[index] * keep;
            }
        }
    }

    int ClampToGrid(int value)
    {
        return System.Math.Max(0, System.Math.Min(_grid, value));
    }

    static bool InEllipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx;
        float dy = (y - cy) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    static float SegmentDistanceSquared(float x, float y, float x0, float y0, float x1, float y1)
    {
        float dx = x1 - x0;
        float dy = y1 - y0;
        float lengthSquared = dx * dx + dy * dy;
        float t = lengthSquared > 0f ? System.Math.Max(0f, System.Math.Min(1f, ((x - x0) * dx + (y - y0) * dy) / lengthSquared)) : 0f;
        float ex = x - (x0 + t * dx);
        float ey = y - (y0 + t * dy);
        return ex * ex + ey * ey;
    }

    static bool InPolygon(IList<(float x, float y)> points, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            (float xi, float yi) = points[i];
            (float xj, float yj) = points[j];
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    static byte ToByte(float value)
    {
        return (byte)System.Math.Round(System.Math.Max(0f, System.Math.Min(1f, value)) * 255f);
    }
}
