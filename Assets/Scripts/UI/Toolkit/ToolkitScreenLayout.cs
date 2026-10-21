using UnityEngine;

// Pixel-to-panel conversion stays independent of device DPI, which is often unavailable.
public class ToolkitScreenLayout
{
    public static float GetScale(int width, int height, bool isMobile)
    {
        if (width <= 0 || height <= 0)
        {
            return 1f;
        }

        if (height > width)
        {
            return width / 390f;
        }

        if (isMobile || height < 600)
        {
            return height / 390f;
        }

        return Mathf.Max(0.75f, Mathf.Min(width / 1440f, height / 900f));
    }

    public static Rect GetSafePanelRect(int width, int height, Rect safeArea, float scale)
    {
        float left = Mathf.Clamp(safeArea.xMin, 0f, width);
        float right = Mathf.Clamp(safeArea.xMax, left, width);
        float bottom = Mathf.Clamp(safeArea.yMin, 0f, height);
        float top = Mathf.Clamp(safeArea.yMax, bottom, height);
        return new Rect(left / scale, (height - top) / scale, (right - left) / scale, (top - bottom) / scale);
    }

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
