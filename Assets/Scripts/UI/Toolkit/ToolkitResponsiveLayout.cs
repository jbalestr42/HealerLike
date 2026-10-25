using UnityEngine;
using UnityEngine.UIElements;

public static class ToolkitResponsiveLayout
{
    // A panel already at its size, as the editor preview and the tests lay it out
    public static bool Apply(ToolkitGameView view, float width, float height)
    {
        return Apply(view, ToolkitLayout.FromPanel(new Vector2(width, height)));
    }

    public static bool Apply(ToolkitGameView view, ToolkitLayoutState state)
    {
        VisualElement hud = view.root.Q("hud-root");
        hud.EnableInClassList("is-compact", state.isCompact);
        hud.EnableInClassList("is-mobile", state.isPhone);
        hud.EnableInClassList("is-landscape", state.isLandscape);
        view.isTouchLayout = state.isPhone;
        return state.isPhone;
    }
}
