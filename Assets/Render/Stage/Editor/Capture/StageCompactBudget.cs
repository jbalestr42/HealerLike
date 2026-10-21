using System.Globalization;

namespace HealerLike.Render.Stage
{
    // The height the party row and the command dock may take together, in panel units.
    //
    // 180 was 76 + 96 (GameUI.Compact.uss) plus a margin. RenderSpellSpacing.uss is loaded onto the document root,
    // where it beats the panel theme, and sets the same rows to 100 and 116: cards tall enough for the 94 pixel
    // portrait, and a dock that holds the 82 pixel spell row and the 18 pixel mana gauge with its margins. Those
    // heights are the layout, so the number moved. 220 is 216 plus 4 for rounding in worldBound.
    public static class StageCompactBudget
    {
        public const float RowsMax = 220f;

        public static bool Fits(float partyHeight, float dockHeight)
        {
            return partyHeight + dockHeight <= RowsMax;
        }

        // The check text, which carries the measurement so a failure explains itself
        public static string Describe(float partyHeight, float dockHeight)
        {
            return "Both compact rows occupy at most " + Format(RowsMax) + " logical pixels: party-panel "
                + Format(partyHeight) + " + command-dock " + Format(dockHeight) + " = "
                + Format(partyHeight + dockHeight);
        }

        static string Format(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
