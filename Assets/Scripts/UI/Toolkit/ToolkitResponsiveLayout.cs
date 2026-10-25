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
    // The pointer last used (ToolkitGameView.isTouchInput): exactly one of these
    public const string MouseClass = "input--mouse";
    public const string TouchClass = "input--touch";

    // A panel already at its size, as the editor preview and the tests lay it out
    public static bool Apply(ToolkitGameView view, float width, float height)
    {
        return Apply(view, ToolkitLayout.FromPanel(new Vector2(width, height)));
    }

    public static bool Apply(ToolkitGameView view, ToolkitLayoutState state)
    {
        view.SetLayout(state);
        ApplyClasses(view.root.Q("hud-root"), state, view.isTouchInput);
        return state.isPhone;
    }

    public static void ApplyClasses(VisualElement element, ToolkitLayoutState state, bool isTouchInput)
    {
        element.EnableInClassList(DesktopClass, !state.isPhone);
        element.EnableInClassList(PhoneClass, state.isPhone);
        element.EnableInClassList(PortraitClass, !state.isLandscape);
        element.EnableInClassList(LandscapeClass, state.isLandscape);
        element.EnableInClassList(CompactClass, state.isCompact);
        element.EnableInClassList(MouseClass, !isTouchInput);
        element.EnableInClassList(TouchClass, isTouchInput);
    }
}
