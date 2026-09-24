using UnityEngine;

// Deterministic icon artwork drawn on the CPU, no camera, scene or external service needed
public static class ProceduralDataIcon
{
    public static readonly int DefaultSize = 128;

    public static Texture2D Create(DataIconDescriptor descriptor)
    {
        return Create(descriptor, DefaultSize);
    }

    public static Texture2D Create(DataIconDescriptor descriptor, int size)
    {
        Color32[] pixels = Render(descriptor, size);
        if (pixels == null)
        {
            return null;
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Icon " + descriptor.label,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    public static Color32[] Render(DataIconDescriptor descriptor)
    {
        return Render(descriptor, DefaultSize);
    }

    public static Color32[] Render(DataIconDescriptor descriptor, int size)
    {
        if (size < 16 || size > 1024)
        {
            Debug.LogError($"[ProceduralDataIcon] Icon size {size} is out of range, use 16 to 1024 pixels");
            return null;
        }

        uint hash = DataIconDescriptor.StableHash(descriptor.key);
        DataIconSymbol symbol = descriptor.symbol;
        Color accent = Color.HSVToRGB(GetHue(symbol, hash), 0.53f, 0.92f);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                pixels[y * size + x] = GetPixel(descriptor.kind, symbol, accent, hash, px, py);
            }
        }

        return pixels;
    }

    // Semantic color families keep healing, poison and fire identifiable across data assets
    static float GetHue(DataIconSymbol symbol, uint hash)
    {
        float hue = (hash % 360) / 360f;
        if (symbol == DataIconSymbol.Heal)
        {
            hue = 0.43f + (hash % 20) / 1000f;
        }

        if (symbol == DataIconSymbol.Poison)
        {
            hue = 0.25f + (hash % 25) / 1000f;
        }

        if (symbol == DataIconSymbol.Flame)
        {
            hue = 0.02f + (hash % 45) / 1000f;
        }

        if (symbol == DataIconSymbol.Shield)
        {
            hue = 0.57f + (hash % 30) / 1000f;
        }

        return hue;
    }

    // Rounded tile, softly lit field, two key-derived orbit marks and a readable category glyph
    static Color GetPixel(DataIconKind kind, DataIconSymbol symbol, Color accent, uint hash, float px, float py)
    {
        float radius = Mathf.Sqrt(px * px + py * py);
        Vector2 cornerOffset = new Vector2(Mathf.Max(Mathf.Abs(px) - 0.76f, 0f), Mathf.Max(Mathf.Abs(py) - 0.76f, 0f));
        float corner = cornerOffset.magnitude;
        if (corner > 0.20f)
        {
            return Color.clear;
        }

        Color color = Color.Lerp(new Color(0.035f, 0.055f, 0.09f), accent, 0.10f + 0.18f * Mathf.Clamp01(1f - radius));
        if (Mathf.Abs(radius - 0.79f) < 0.012f)
        {
            color = Color.Lerp(color, accent, 0.55f);
        }

        float angle = Mathf.Atan2(py, px) + (hash % 63) * 0.1f;
        if (radius > 0.72f && radius < 0.85f && Mathf.Cos(angle * 3f) > 0.965f)
        {
            color = accent;
        }

        if (DataIconGlyph.Contains(kind, symbol, px, py))
        {
            color = Color.Lerp(accent, Color.white, 0.55f);
        }

        return color;
    }
}
