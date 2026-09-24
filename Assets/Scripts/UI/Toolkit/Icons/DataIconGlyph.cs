using UnityEngine;

// Glyph shapes of the data icons, in tile coordinates from -1 to 1
public static class DataIconGlyph
{
    public static bool Contains(DataIconKind kind, DataIconSymbol symbol, float x, float y)
    {
        switch (symbol)
        {
            case DataIconSymbol.Heal:
                return Mathf.Abs(x) < 0.105f && Mathf.Abs(y) < 0.48f
                    || Mathf.Abs(y) < 0.105f && Mathf.Abs(x) < 0.42f;
            case DataIconSymbol.Bolt:
                return Mathf.Abs(x + y * 0.48f) < 0.13f && Mathf.Abs(y) < 0.52f
                    || Mathf.Abs(y) < 0.08f && Mathf.Abs(x) < 0.3f;
            case DataIconSymbol.Shield:
                return y < 0.43f && y > -0.52f && Mathf.Abs(x) < 0.39f * Mathf.Min(1f, (y + 0.52f) * 2.5f)
                    && !(y < 0.30f && y > -0.32f && Mathf.Abs(x) < 0.25f * Mathf.Min(1f, (y + 0.32f) * 3f));
            case DataIconSymbol.Poison:
                return x * x + (y + 0.19f) * (y + 0.19f) < 0.105f
                    || y >= -0.10f && y < 0.54f && Mathf.Abs(x) < (0.54f - y) * 0.48f;
            case DataIconSymbol.Flame:
                return (x * x / 0.13f + (y + 0.15f) * (y + 0.15f) / 0.17f < 1f
                    || y > 0f && y < 0.57f && Mathf.Abs(x - 0.12f * Mathf.Sin(y * 8f)) < (0.57f - y) * 0.45f)
                    && !(x * x / 0.025f + (y + 0.27f) * (y + 0.27f) / 0.05f < 1f);
            case DataIconSymbol.Slime:
                return y > -0.37f && x * x / 0.28f + (y + 0.30f) * (y + 0.30f) / 0.50f < 1f
                    && !(Mathf.Abs(Mathf.Abs(x) - 0.17f) < 0.05f && Mathf.Abs(y + 0.03f) < 0.055f);
            case DataIconSymbol.Dragon:
                return Mathf.Abs(x) + Mathf.Abs(y) < 0.37f
                    || Mathf.Abs(x) > 0.16f && Mathf.Abs(x) < 0.63f
                    && y > -0.18f && y < 0.52f - Mathf.Abs(x) * 0.5f
                    && y > -0.18f + 0.10f * Mathf.Sin(Mathf.Abs(x) * 30f);
            case DataIconSymbol.Fox:
                return Mathf.Abs(x) < 0.46f && y < 0.45f && y > -0.48f + Mathf.Abs(x) * 1.3f
                    && !(Mathf.Abs(x) < 0.22f && y > 0.22f)
                    && !(Mathf.Abs(Mathf.Abs(x) - 0.18f) < 0.065f && Mathf.Abs(y) < 0.05f);
            case DataIconSymbol.Soldier:
                return x * x / 0.17f + y * y / 0.25f < 1f
                    && !(Mathf.Abs(y - 0.08f) < 0.055f && Mathf.Abs(x) > 0.065f)
                    && !(Mathf.Abs(x) < 0.045f && y < 0.03f);
            case DataIconSymbol.Swarm:
                return x * x + (y - 0.29f) * (y - 0.29f) < 0.035f
                    || (x - 0.29f) * (x - 0.29f) + (y + 0.20f) * (y + 0.20f) < 0.035f
                    || (x + 0.29f) * (x + 0.29f) + (y + 0.20f) * (y + 0.20f) < 0.035f;
            case DataIconSymbol.Archer:
                return Mathf.Abs(Mathf.Sqrt((x + 0.18f) * (x + 0.18f) + y * y) - 0.46f) < 0.045f && x > -0.14f
                    || Mathf.Abs(x + 0.14f) < 0.025f && Mathf.Abs(y) < 0.44f
                    || Mathf.Abs(y) < 0.03f && Mathf.Abs(x) < 0.50f
                    || x > 0.30f && x < 0.52f && Mathf.Abs(y) < (0.52f - x) * 0.6f;
            case DataIconSymbol.Mage:
                return y > -0.27f && y < 0.54f && Mathf.Abs(x + 0.07f) < (0.54f - y) * 0.46f
                    || Mathf.Abs(y + 0.29f) < 0.055f && Mathf.Abs(x) < 0.48f;
        }

        return ContainsKind(kind, x, y);
    }

    // Generic symbols fall back to the emblem of their category
    static bool ContainsKind(DataIconKind kind, float x, float y)
    {
        switch (kind)
        {
            case DataIconKind.Creature:
                bool head = x * x / 0.20f + y * y / 0.24f < 1f;
                bool ears = Mathf.Abs(x) > 0.24f && Mathf.Abs(x) < 0.46f
                    && y > 0.20f && y < 0.62f - Mathf.Abs(x) * 0.4f;
                bool eyes = Mathf.Abs(Mathf.Abs(x) - 0.19f) < 0.065f && Mathf.Abs(y - 0.06f) < 0.055f;
                bool mouth = Mathf.Abs(x) < 0.12f && y > -0.28f && y < -0.22f;
                return (head || ears) && !eyes && !mouth;
            case DataIconKind.Character:
                return x * x + (y - 0.27f) * (y - 0.27f) < 0.045f
                    || y < -0.02f && y > -0.45f && Mathf.Abs(x) < 0.37f - (y + 0.45f) * 0.30f;
            case DataIconKind.Spell:
                return Mathf.Abs(x) + Mathf.Abs(y) < 0.54f && Mathf.Abs(x) + Mathf.Abs(y) > 0.30f
                    || x * x + y * y < 0.025f;
            default:
                return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) < 0.38f && Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) > 0.25f
                    || x * x + y * y < 0.025f;
        }
    }
}
