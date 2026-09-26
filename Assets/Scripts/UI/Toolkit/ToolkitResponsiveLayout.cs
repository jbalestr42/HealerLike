using UnityEngine.UIElements;

public static class ToolkitResponsiveLayout
{
    public static bool Apply(ToolkitGameView view, float width, float height)
    {
        VisualElement hud = view.root.Q("hud-root");
        ICustomStyle style = hud.customStyle;
        float mobileWidth = ToolkitStyleValues.ReadPositive(style, "--ui-mobile-width-breakpoint", 760f);
        float mobileHeight = ToolkitStyleValues.ReadPositive(style, "--ui-mobile-height-breakpoint", 500f);
        float compactWidth = ToolkitStyleValues.ReadPositive(style, "--ui-compact-breakpoint", 1100f);
        bool mobile = width < mobileWidth || height < mobileHeight;
        hud.EnableInClassList("is-compact", width < compactWidth);
        hud.EnableInClassList("is-mobile", mobile);
        hud.EnableInClassList("is-landscape", width > height);
        view.isTouchLayout = mobile;
        VisualElement controls = view.root.Q(mobile ? "pause-settings" : "command-section");
        VisualElement speed = view.root.Q("speed-controls");
        VisualElement markers = view.root.Q("mark-entity-toggle");
        if (speed.parent != controls)
        {
            controls.Add(speed);
            controls.Add(markers);
        }

        view.SetButton("inventory-button", mobile ? "Bag" : "Inventory", true);
        return mobile;
    }
}
