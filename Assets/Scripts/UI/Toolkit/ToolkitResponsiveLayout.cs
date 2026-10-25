using UnityEngine;
using UnityEngine.UIElements;

// The root classes the stylesheets read the layout from, one class per axis
public static class ToolkitResponsiveLayout
{
    // Form factor: exactly one of these
    public const string DesktopClass = "layout--desktop";
    public const string PhoneClass = "layout--phone";
    // Orientation of the panel, on every form factor: exactly one of these
    public const string PortraitClass = "layout--portrait";
    public const string LandscapeClass = "layout--landscape";
    // A narrow panel tightens its spacing
    public const string CompactClass = "layout--compact";
    // The pointer the UI is laid out for: exactly one of these
    public const string MouseClass = "input--mouse";
    public const string TouchClass = "input--touch";

    // A panel already at its size, as the editor preview and the tests lay it out
    public static bool Apply(ToolkitGameView view, float width, float height)
    {
        return Apply(view, ToolkitLayout.FromPanel(new Vector2(width, height)));
    }

    public static bool Apply(ToolkitGameView view, ToolkitLayoutState state)
    {
        ApplyClasses(view.root.Q("hud-root"), state);
        view.isTouchLayout = state.isPhone;
        return state.isPhone;
    }

    public static void ApplyClasses(VisualElement element, ToolkitLayoutState state)
    {
        element.EnableInClassList(DesktopClass, !state.isPhone);
        element.EnableInClassList(PhoneClass, state.isPhone);
        element.EnableInClassList(PortraitClass, !state.isLandscape);
        element.EnableInClassList(LandscapeClass, state.isLandscape);
        element.EnableInClassList(CompactClass, state.isCompact);
        // Until the input follows the last pointer used, a phone layout is a touch one
        element.EnableInClassList(MouseClass, !state.isPhone);
        element.EnableInClassList(TouchClass, state.isPhone);
    }
}
