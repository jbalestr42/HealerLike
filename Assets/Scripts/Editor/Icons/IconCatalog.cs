using System.Collections.Generic;

// What IconPainter paints: for each data asset, its glyph, frame and colour, and where its icon is saved
public static class IconCatalog
{
    public enum Frame
    {
        // A status: a disc, like the spells
        Disc,
    }

    public struct Entry
    {
        // The data the icon is set on (a status sets it on the buff putting it there)
        public string assetPath;
        // Under Assets/Icons, without the extension
        public string iconPath;
        public string glyph;
        public Frame frame;
        public string color;

        public Entry(string assetPath, string iconPath, string glyph, Frame frame, string color)
        {
            this.assetPath = assetPath;
            this.iconPath = iconPath;
            this.glyph = glyph;
            this.frame = frame;
            this.color = color;
        }

        public string iconAssetPath => "Assets/Icons/" + iconPath + ".png";
    }

    public static readonly List<Entry> entries = new List<Entry>
    {
        Status("Blessed", "halo", "#C9A23A"),
        Status("Blighted", "heartCracked", "#6A4A7A"),
        Status("Chilled", "snowflake", "#3F93AE"),
        Status("Empowered", "swordUp", "#B04A3A"),
        Status("Fortified", "shieldPlus", "#3E6A8C"),
        Status("Hasted", "chevrons", "#C9772E"),
        Status("Invulnerable", "bubble", "#7A6AB8"),
        Status("Poisoned", "drop", "#5E8C3A"),
        Status("Regenerating", "heartPlus", "#2E9C7C"),
        Status("Sundered", "shieldCracked", "#7A5A3A"),
        Status("Vulnerable", "reticle", "#9C3A62"),
    };

    static Entry Status(string name, string glyph, string color)
    {
        return new Entry($"Assets/Data/Statuses/{name}Status/{name}Status.asset", "Statuses/" + name, glyph, Frame.Disc, color);
    }

    public static IconCanvas Paint(Entry entry, int size)
    {
        var canvas = new IconCanvas(size);
        IconColor color = IconColor.Hex(entry.color);
        IconPen pen = IconFrames.Disc(canvas, color);
        IconGlyphs.all[entry.glyph](pen);
        return canvas;
    }
}
