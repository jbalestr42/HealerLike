// The backgrounds of the painted icons, each giving the pen its glyph is drawn with: a disc for the statuses (like
// the spell icons), a rounded square for the items
public static class IconFrames
{
    // The pictograms' colour, on every frame
    public static readonly IconColor Cream = IconColor.Hex("#F6EFDC");

    // A light rim, a dark band, then the colour, the glyph in cream with details in a light shade of the colour
    public static IconPen Disc(IconCanvas canvas, IconColor color)
    {
        float size = canvas.size;
        float centre = size / 2f;
        canvas.FillCircle(centre, centre, size * 0.48f, color.Lighten(0.3f));
        canvas.FillCircle(centre, centre, size * 0.45f, color.Darken(0.45f));
        canvas.FillCircle(centre, centre, size * 0.415f, color);
        float glyph = size * 0.58f;
        return new IconPen(canvas, centre - glyph / 2f, centre - glyph / 2f, glyph, Cream, color.Lighten(0.4f), color);
    }
}
