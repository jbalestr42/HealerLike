using UnityEngine;

// The free area the HUD leaves to the world, as a camera viewport
public class ToolkitScreenLayout
{
    public static Rect GetViewport(Rect worldRect, Rect panelRect)
    {
        if (
            panelRect.width <= 0f
            || panelRect.height <= 0f
            || worldRect.width <= 0f
            || worldRect.height <= 0f
            || float.IsNaN(worldRect.width)
            || float.IsNaN(worldRect.height)
        )
        {
            return new Rect(0.04f, 0.24f, 0.92f, 0.64f);
        }

        float left = Mathf.Clamp01((worldRect.xMin - panelRect.xMin) / panelRect.width);
        float right = Mathf.Clamp01((worldRect.xMax - panelRect.xMin) / panelRect.width);
        float bottom = Mathf.Clamp01(1f - (worldRect.yMax - panelRect.yMin) / panelRect.height);
        float top = Mathf.Clamp01(1f - (worldRect.yMin - panelRect.yMin) / panelRect.height);
        return Rect.MinMaxRect(left, bottom, right, top);
    }
}
