using UnityEngine;

// Geometry and labels only. Room availability and progress belong to RunState.
public static class ToolkitRunMapPresentation
{
    public const float FloorHeight = 112f;
    public const float NodeHeight = 68f;
    public const float MinimumColumnWidth = 64f;
    public const float Gutter = 24f;

    public static Vector2 CanvasSize(RunMap map, float viewportWidth)
    {
        return new Vector2(Mathf.Max(viewportWidth, map.columnCount * MinimumColumnWidth + Gutter * 2f),
            (map.floorCount + 1) * FloorHeight + Gutter * 2f);
    }

    public static Vector2 NodeCenter(MapNode node, RunMap map, float width)
    {
        float x = node == map.boss ? width * 0.5f
            : Gutter + (node.column + 0.5f) * (width - Gutter * 2f) / map.columnCount;
        return new Vector2(x, Gutter + (map.floorCount - node.floor + 0.5f) * FloorHeight);
    }

    public static float NodeWidth(RunMap map, float width)
    {
        return Mathf.Clamp((width - Gutter * 2f) / map.columnCount - 12f, 52f, 104f);
    }

    public static Vector2 FocusOffset(RunState run, Vector2 canvasSize, Vector2 viewportSize)
    {
        // Frame the current room and the reachable floor together, including the first floor at entry.
        float floor = Mathf.Max(0f, run.currentFloor + 0.5f);
        float y = Gutter + (run.map.floorCount - floor + 0.5f) * FloorHeight;
        float x = canvasSize.x * 0.5f;
        var available = run.GetAvailableNodes();
        if (available.Count > 0)
        {
            float left = canvasSize.x;
            float right = 0f;
            foreach (MapNode node in available)
            {
                float center = NodeCenter(node, run.map, canvasSize.x).x;
                left = Mathf.Min(left, center);
                right = Mathf.Max(right, center);
            }
            x = (left + right) * 0.5f;
        }
        return new Vector2(Mathf.Clamp(x - viewportSize.x * 0.5f, 0f, Mathf.Max(0f, canvasSize.x - viewportSize.x)),
            Mathf.Clamp(y - viewportSize.y * 0.58f, 0f, Mathf.Max(0f, canvasSize.y - viewportSize.y)));
    }

    public static string StateLabel(MapNodeState state)
    {
        switch (state)
        {
            case MapNodeState.Available: return "NEXT";
            case MapNodeState.Current: return "HERE";
            case MapNodeState.Visited: return "VISITED";
            default: return "AHEAD";
        }
    }

    public static string Description(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Combat: return "Fight an encounter and choose a reward.";
            case MapNodeType.Elite: return "A tougher encounter with more reward choices.";
            case MapNodeType.Treasure: return "Choose a reward for the journey ahead.";
            case MapNodeType.Rest: return "Recover your allies' health.";
            case MapNodeType.Boss: return "Reach the summit to complete this expedition.";
            default: return string.Empty;
        }
    }
}
