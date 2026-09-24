using System;
using UnityEngine;

namespace HealerLike.UI.Toolkit.Icons
{
    /// <summary>Deterministic, CPU-only icon artwork. No cameras, scenes or external service required.</summary>
    public static class ProceduralDataIcon
    {
        public const int DefaultSize = 128;
        public static Texture2D Create(DataIconDescriptor descriptor, int size = DefaultSize)
        {
            var pixels = Render(descriptor, size);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Icon " + descriptor.Label,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        public static Color32[] Render(DataIconDescriptor descriptor, int size = DefaultSize)
        {
            if (size < 16 || size > 1024) throw new ArgumentOutOfRangeException(nameof(size), "Use 16–1024 pixels.");
            uint hash = DataIconDescriptor.StableHash(descriptor.Key);
            var symbol = descriptor.Symbol;
            float hue = (hash % 360) / 360f;
            // Semantic color families keep healing, poison and fire identifiable across data assets.
            if (symbol == DataIconSymbol.Heal) hue = .43f + (hash % 20) / 1000f;
            if (symbol == DataIconSymbol.Poison) hue = .25f + (hash % 25) / 1000f;
            if (symbol == DataIconSymbol.Flame) hue = .02f + (hash % 45) / 1000f;
            if (symbol == DataIconSymbol.Shield) hue = .57f + (hash % 30) / 1000f;
            Color accent = Color.HSVToRGB(hue, .53f, .92f);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = (x + .5f) / size * 2 - 1;
                float py = (y + .5f) / size * 2 - 1;
                float radius = Mathf.Sqrt(px * px + py * py);
                // Rounded tile, softly lit field, two key-derived orbit marks and a readable category glyph.
                float corner = new Vector2(Mathf.Max(Mathf.Abs(px) - .76f, 0), Mathf.Max(Mathf.Abs(py) - .76f, 0)).magnitude;
                if (corner > .20f) { pixels[y * size + x] = Color.clear; continue; }
                Color color = Color.Lerp(new Color(.035f, .055f, .09f), accent, .10f + .18f * Mathf.Clamp01(1 - radius));
                if (Mathf.Abs(radius - .79f) < .012f) color = Color.Lerp(color, accent, .55f);
                float angle = Mathf.Atan2(py, px) + (hash % 63) * .1f;
                if (radius > .72f && radius < .85f && Mathf.Cos(angle * 3) > .965f) color = accent;
                if (Glyph(descriptor.Kind, symbol, px, py, hash)) color = Color.Lerp(accent, Color.white, .55f);
                pixels[y * size + x] = color;
            }
            return pixels;
        }

        static bool Glyph(DataIconKind kind, DataIconSymbol symbol, float x, float y, uint hash)
        {
            switch (symbol)
            {
                case DataIconSymbol.Heal:
                    return Mathf.Abs(x) < .105f && Mathf.Abs(y) < .48f || Mathf.Abs(y) < .105f && Mathf.Abs(x) < .42f;
                case DataIconSymbol.Bolt:
                    return Mathf.Abs(x + y * .48f) < .13f && Mathf.Abs(y) < .52f || Mathf.Abs(y) < .08f && Mathf.Abs(x) < .3f;
                case DataIconSymbol.Shield:
                    return y < .43f && y > -.52f && Mathf.Abs(x) < .39f * Mathf.Min(1, (y + .52f) * 2.5f) &&
                        !(y < .30f && y > -.32f && Mathf.Abs(x) < .25f * Mathf.Min(1, (y + .32f) * 3));
                case DataIconSymbol.Poison:
                    return x * x + (y + .19f) * (y + .19f) < .105f || y >= -.10f && y < .54f && Mathf.Abs(x) < (.54f - y) * .48f;
                case DataIconSymbol.Flame:
                    return (x * x / .13f + (y + .15f) * (y + .15f) / .17f < 1 ||
                        y > 0 && y < .57f && Mathf.Abs(x - .12f * Mathf.Sin(y * 8)) < (.57f - y) * .45f) &&
                        !(x * x / .025f + (y + .27f) * (y + .27f) / .05f < 1);
                case DataIconSymbol.Slime:
                    return y > -.37f && x * x / .28f + (y + .30f) * (y + .30f) / .50f < 1 &&
                        !(Mathf.Abs(Mathf.Abs(x) - .17f) < .05f && Mathf.Abs(y + .03f) < .055f);
                case DataIconSymbol.Dragon:
                    return Mathf.Abs(x) + Mathf.Abs(y) < .37f ||
                        Mathf.Abs(x) > .16f && Mathf.Abs(x) < .63f && y > -.18f && y < .52f - Mathf.Abs(x) * .5f &&
                        y > -.18f + .10f * Mathf.Sin(Mathf.Abs(x) * 30);
                case DataIconSymbol.Fox:
                    return Mathf.Abs(x) < .46f && y < .45f && y > -.48f + Mathf.Abs(x) * 1.3f &&
                        !(Mathf.Abs(x) < .22f && y > .22f) &&
                        !(Mathf.Abs(Mathf.Abs(x) - .18f) < .065f && Mathf.Abs(y) < .05f);
                case DataIconSymbol.Soldier:
                    return x * x / .17f + y * y / .25f < 1 &&
                        !(Mathf.Abs(y - .08f) < .055f && Mathf.Abs(x) > .065f) &&
                        !(Mathf.Abs(x) < .045f && y < .03f);
                case DataIconSymbol.Swarm:
                    return x * x + (y - .29f) * (y - .29f) < .035f ||
                        (x - .29f) * (x - .29f) + (y + .20f) * (y + .20f) < .035f ||
                        (x + .29f) * (x + .29f) + (y + .20f) * (y + .20f) < .035f;
                case DataIconSymbol.Archer:
                    return Mathf.Abs(Mathf.Sqrt((x + .18f) * (x + .18f) + y * y) - .46f) < .045f && x > -.14f ||
                        Mathf.Abs(x + .14f) < .025f && Mathf.Abs(y) < .44f ||
                        Mathf.Abs(y) < .03f && Mathf.Abs(x) < .50f ||
                        x > .30f && x < .52f && Mathf.Abs(y) < (.52f - x) * .6f;
                case DataIconSymbol.Mage:
                    return y > -.27f && y < .54f && Mathf.Abs(x + .07f) < (.54f - y) * .46f ||
                        Mathf.Abs(y + .29f) < .055f && Mathf.Abs(x) < .48f;
            }
            switch (kind)
            {
                case DataIconKind.Creature:
                    bool head = x * x / .20f + y * y / .24f < 1;
                    bool ears = Mathf.Abs(x) > .24f && Mathf.Abs(x) < .46f && y > .20f && y < .62f - Mathf.Abs(x) * .4f;
                    bool eyes = Mathf.Abs(Mathf.Abs(x) - .19f) < .065f && Mathf.Abs(y - .06f) < .055f;
                    bool mouth = Mathf.Abs(x) < .12f && y > -.28f && y < -.22f;
                    return (head || ears) && !eyes && !mouth;
                case DataIconKind.Character:
                    return x * x + (y - .27f) * (y - .27f) < .045f ||
                           y < -.02f && y > -.45f && Mathf.Abs(x) < .37f - (y + .45f) * .30f;
                case DataIconKind.Spell:
                    return Mathf.Abs(x) + Mathf.Abs(y) < .54f && Mathf.Abs(x) + Mathf.Abs(y) > .30f || x * x + y * y < .025f;
                default:
                    return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) < .38f && Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) > .25f || x * x + y * y < .025f;
            }
        }
    }
}
