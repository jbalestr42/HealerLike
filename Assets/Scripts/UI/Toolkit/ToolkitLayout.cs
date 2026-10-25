using System;
using UnityEngine;

public enum ToolkitLayoutMode
{
    Desktop,
    Portrait,
    Landscape,
}

// Every responsive threshold of the game UI, in one place. Sizes are in panel units unless named pixels
public class ToolkitLayoutSettings
{
    public static readonly ToolkitLayoutSettings Default = new ToolkitLayoutSettings();

    // The panel width in portrait, the panel height in phone landscape
    public float phoneSide = 390f;
    // The panel size a desktop screen is scaled to, never below the minimum scale
    public Vector2 desktopReference = new Vector2(1440f, 900f);
    public float desktopMinScale = 0.75f;
    // A landscape screen shorter than this many pixels is laid out as a phone
    public float desktopMinPixelHeight = 600f;
    // A landscape panel shorter than this is the phone landscape layout
    public float landscapeMaxHeight = 500f;
    // A panel narrower than this tightens its spacing
    public float compactWidth = 1100f;
    // A screen whose short side is at least this fraction of its long side is a tablet (phones stay under 0.57)
    public float tabletMinAspect = 0.6f;
    // The panel size a mobile tablet in landscape is scaled to: the desktop layout, with larger touch targets
    public Vector2 tabletReference = new Vector2(1180f, 860f);
    // A tablet shaped portrait screen keeps at least this panel height, rather than a squat phone column
    public float tabletPortraitHeight = 960f;
    // How far a phone in landscape frames the board past the HUD bars, from the free area (0) to the screen edges (1)
    public float landscapeBoardOverlap = 0.75f;
}

// What the UI is laid out for: decided from the screen alone, so every consumer sees the same answer
public readonly struct ToolkitLayoutState : IEquatable<ToolkitLayoutState>
{
    public readonly ToolkitLayoutMode mode;
    // Screen pixels per panel unit
    public readonly float scale;
    public readonly Vector2 panelSize;
    // The safe area in panel units, from the top left
    public readonly Rect safeRect;
    public readonly bool isCompact;
    // A mobile device or a phone layout: the input until a pointer says otherwise
    public readonly bool isTouchDevice;
    public bool isPhone => mode != ToolkitLayoutMode.Desktop;
    public bool isLandscape => panelSize.x > panelSize.y;

    public ToolkitLayoutState(ToolkitLayoutMode mode, float scale, Vector2 panelSize, Rect safeRect, bool isCompact,
        bool isTouchDevice)
    {
        this.mode = mode;
        this.scale = scale;
        this.panelSize = panelSize;
        this.safeRect = safeRect;
        this.isCompact = isCompact;
        this.isTouchDevice = isTouchDevice;
    }

    public override bool Equals(object obj)
    {
        return obj is ToolkitLayoutState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(mode, scale, panelSize, safeRect, isCompact, isTouchDevice);
    }

    public override string ToString()
    {
        return $"{mode} x{scale:0.##} {panelSize} safe {safeRect}{(isCompact ? " compact" : "")}{(isTouchDevice ? " touch" : "")}";
    }

    #region IEquatable<ToolkitLayoutState>
    public bool Equals(ToolkitLayoutState other)
    {
        return mode == other.mode && scale == other.scale && panelSize == other.panelSize
            && safeRect == other.safeRect && isCompact == other.isCompact && isTouchDevice == other.isTouchDevice;
    }
    #endregion
}

public static class ToolkitLayout
{
    // screen and safeArea in pixels, safeArea from the bottom left as Screen.safeArea gives it
    public static ToolkitLayoutState Resolve(Vector2Int screen, Rect safeArea, bool isMobilePlatform,
        ToolkitLayoutSettings settings = null)
    {
        settings = settings ?? ToolkitLayoutSettings.Default;
        float scale = GetScale(screen.x, screen.y, isMobilePlatform, settings);
        Vector2 panel = screen.x > 0 && screen.y > 0 ? new Vector2(screen.x / scale, screen.y / scale) : Vector2.zero;
        Rect safe = GetSafePanelRect(screen.x, screen.y, safeArea, scale);
        return FromPanel(panel, safe, scale, isMobilePlatform, settings);
    }

    // A panel already at its size, as the editor preview and the tests lay it out
    public static ToolkitLayoutState FromPanel(Vector2 panelSize, ToolkitLayoutSettings settings = null)
    {
        return FromPanel(panelSize, new Rect(Vector2.zero, panelSize), 1f, false,
            settings ?? ToolkitLayoutSettings.Default);
    }

    // The layout of the current screen, for code that converts pointer pixels between frames
    public static ToolkitLayoutState current => Resolve(new Vector2Int(Screen.width, Screen.height), Screen.safeArea,
        Application.isMobilePlatform);

    static ToolkitLayoutState FromPanel(Vector2 panel, Rect safe, float scale, bool isMobilePlatform,
        ToolkitLayoutSettings settings)
    {
        ToolkitLayoutMode mode = GetMode(panel, settings);
        return new ToolkitLayoutState(mode, scale, panel, safe, panel.x < settings.compactWidth,
            isMobilePlatform || mode != ToolkitLayoutMode.Desktop);
    }

    public static ToolkitLayoutMode GetMode(Vector2 panelSize, ToolkitLayoutSettings settings = null)
    {
        settings = settings ?? ToolkitLayoutSettings.Default;
        if (panelSize.y > panelSize.x)
        {
            return ToolkitLayoutMode.Portrait;
        }

        return panelSize.y < settings.landscapeMaxHeight ? ToolkitLayoutMode.Landscape : ToolkitLayoutMode.Desktop;
    }

    // Pixel-to-panel conversion stays independent of device DPI, which is often unavailable
    public static float GetScale(int width, int height, bool isMobilePlatform, ToolkitLayoutSettings settings = null)
    {
        settings = settings ?? ToolkitLayoutSettings.Default;
        if (width <= 0 || height <= 0)
        {
            return 1f;
        }

        bool isTablet = IsTablet(width, height, settings);
        if (height > width)
        {
            // A tablet (or a near square window) would otherwise get a phone column far shorter than a phone's
            return isTablet
                ? Mathf.Min(width / settings.phoneSide, height / settings.tabletPortraitHeight)
                : width / settings.phoneSide;
        }

        // Only a mobile tablet: a PC screen is often as square, and keeps the desktop scale below
        if (isMobilePlatform && isTablet)
        {
            return Mathf.Min(width / settings.tabletReference.x, height / settings.tabletReference.y);
        }

        if (isMobilePlatform || height < settings.desktopMinPixelHeight)
        {
            return height / settings.phoneSide;
        }

        return Mathf.Max(settings.desktopMinScale,
            Mathf.Min(width / settings.desktopReference.x, height / settings.desktopReference.y));
    }

    public static bool IsTablet(int width, int height, ToolkitLayoutSettings settings = null)
    {
        settings = settings ?? ToolkitLayoutSettings.Default;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        return Mathf.Min(width, height) >= settings.tabletMinAspect * Mathf.Max(width, height);
    }

    public static Rect GetSafePanelRect(int width, int height, Rect safeArea, float scale)
    {
        float left = Mathf.Clamp(safeArea.xMin, 0f, width);
        float right = Mathf.Clamp(safeArea.xMax, left, width);
        float bottom = Mathf.Clamp(safeArea.yMin, 0f, height);
        float top = Mathf.Clamp(safeArea.yMax, bottom, height);
        return new Rect(left / scale, (height - top) / scale, (right - left) / scale, (top - bottom) / scale);
    }
}
