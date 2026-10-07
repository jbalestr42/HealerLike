using UnityEngine;
using UnityEngine.UIElements;

public sealed class ToolkitCooldown : VisualElement
{
    float _remaining;
    public float remaining
    {
        get => _remaining;
        set { _remaining = Mathf.Clamp01(value); MarkDirtyRepaint(); }
    }
    public ToolkitCooldown()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = style.right = style.top = style.bottom = 0;
        generateVisualContent += Draw;
    }
    void Draw(MeshGenerationContext context)
    {
        if (_remaining <= 0)
        {
            return;
        }

        var painter = context.painter2D;
        Vector2 center = contentRect.center;
        float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.5f;
        painter.fillColor = new Color(0.02f, 0.08f, 0.08f, 0.72f);
        painter.BeginPath(); painter.MoveTo(center);
        painter.LineTo(center + Vector2.up * -radius);
        // The legacy radial image starts at the top and fills counterclockwise.
        painter.Arc(center, radius, -90, -90 - _remaining * 360, ArcDirection.CounterClockwise);
        painter.ClosePath(); painter.Fill();
    }
}
