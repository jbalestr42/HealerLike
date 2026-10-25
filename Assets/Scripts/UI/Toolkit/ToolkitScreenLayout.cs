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

    // The area the camera frames the board in. A phone in landscape leaves too short a strip between the HUD bars for
    // the board to read: it reaches most of the way to the screen's top and bottom, its edge rows going under the
    // bars, and keeps the free width
    public static Rect GetBoardViewport(Rect worldViewport, ToolkitLayoutMode mode)
    {
        return GetBoardViewport(worldViewport, mode, ToolkitLayoutSettings.Default);
    }

    public static Rect GetBoardViewport(Rect worldViewport, ToolkitLayoutMode mode, ToolkitLayoutSettings settings)
    {
        if (mode != ToolkitLayoutMode.Landscape)
        {
            return worldViewport;
        }

        float overlap = Mathf.Clamp01(settings.landscapeBoardOverlap);
        return Rect.MinMaxRect(worldViewport.xMin, Mathf.Lerp(worldViewport.yMin, 0f, overlap),
            worldViewport.xMax, Mathf.Lerp(worldViewport.yMax, 1f, overlap));
    }
}
