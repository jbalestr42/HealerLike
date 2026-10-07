using UnityEngine;
using UnityEngine.UIElements;

// Small code-native line icons inside unchanged 44-pixel buttons.
public sealed class ToolkitHudGlyph : VisualElement
{
    readonly string _kind;
    ToolkitHudGlyph(string kind)
    {
        _kind = kind; pickingMode = PickingMode.Ignore;
        style.width = style.height = 22; style.alignSelf = Align.Center;
        style.scale = new Scale(new Vector3(0.8f, 0.8f, 1));
        generateVisualContent += Draw;
    }
    public static void Attach(VisualElement root)
    {
        foreach (string kind in new[] { "map", "inventory", "pause" })
        {
            Button button = root.Q<Button>(kind + "-button");
            if (button == null)
            {
                continue;
            }

            button.text = "";
            VisualElement surface = new VisualElement { pickingMode = PickingMode.Ignore };
            surface.AddToClassList("hud-icon-surface");
            surface.Add(new ToolkitHudGlyph(kind));
            button.Add(surface);
        }
    }
    void Draw(MeshGenerationContext context)
    {
        Painter2D p = context.painter2D;
        p.strokeColor = new Color(0.97f, 0.96f, 0.87f); p.lineWidth = 1.5f;
        if (_kind == "pause") { Line(p, 7, 4, 7, 19); Line(p, 15, 4, 15, 19); }
        else if (_kind == "inventory")
        {
            p.BeginPath(); p.MoveTo(new Vector2(4, 8)); p.LineTo(new Vector2(18, 8));
            p.LineTo(new Vector2(20, 20)); p.LineTo(new Vector2(2, 20)); p.ClosePath(); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(8, 10)); p.LineTo(new Vector2(8, 5));
            p.Arc(new Vector2(11, 5), 3, 180, 360); p.LineTo(new Vector2(14, 10)); p.Stroke();
        }
        else
        {
            p.BeginPath(); p.MoveTo(new Vector2(2, 5)); p.LineTo(new Vector2(8, 2));
            p.LineTo(new Vector2(14, 5)); p.LineTo(new Vector2(20, 2));
            p.LineTo(new Vector2(20, 17)); p.LineTo(new Vector2(14, 20));
            p.LineTo(new Vector2(8, 17)); p.LineTo(new Vector2(2, 20)); p.ClosePath(); p.Stroke();
            Line(p, 8, 2, 8, 17); Line(p, 14, 5, 14, 20);
        }
    }
    static void Line(Painter2D p, float x, float y, float endX, float endY)
    { p.BeginPath(); p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(endX, endY)); p.Stroke(); }
}
