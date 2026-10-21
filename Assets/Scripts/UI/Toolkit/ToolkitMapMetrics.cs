using UnityEngine.UIElements;

// Both path painting and node placement consume the same resolved visual geometry.
public struct ToolkitMapMetrics
{
    public float floorHeight;
    public float nodeHeight;
    public float columnWidth;
    public float gutter;
    public float nodeGap;
    public float minNodeWidth;
    public float maxNodeWidth;
    public static ToolkitMapMetrics defaults
    {
        get
        {
            return new ToolkitMapMetrics
            {
                floorHeight = ToolkitRunMapPresentation.FloorHeight,
                nodeHeight = ToolkitRunMapPresentation.NodeHeight,
                columnWidth = ToolkitRunMapPresentation.MinimumColumnWidth,
                gutter = ToolkitRunMapPresentation.Gutter,
                nodeGap = 12f,
                minNodeWidth = 52f,
                maxNodeWidth = 104f,
            };
        }
    }

    public static ToolkitMapMetrics Read(ICustomStyle style)
    {
        ToolkitMapMetrics value = defaults;
        value.floorHeight = ToolkitStyleValues.ReadPositive(style, "--map-layout-floor-height", value.floorHeight);
        value.nodeHeight = ToolkitStyleValues.ReadPositive(style, "--map-layout-node-height", value.nodeHeight);
        value.columnWidth = ToolkitStyleValues.ReadPositive(style, "--map-layout-column-width", value.columnWidth);
        value.gutter = ToolkitStyleValues.ReadPositive(style, "--map-layout-gutter", value.gutter);
        value.nodeGap = ToolkitStyleValues.ReadPositive(style, "--map-layout-node-gap", value.nodeGap);
        value.minNodeWidth = ToolkitStyleValues.ReadPositive(style, "--map-layout-node-min-width", value.minNodeWidth);
        value.maxNodeWidth = ToolkitStyleValues.ReadPositive(style, "--map-layout-node-max-width", value.maxNodeWidth);
        value.maxNodeWidth = UnityEngine.Mathf.Max(value.minNodeWidth, value.maxNodeWidth);
        return value;
    }
}
