using System;
using System.Collections.Generic;

// Slay the Spire like map: paths climb one floor at a time, moving at most one column left or right,
// without ever crossing an existing edge. The rooms not visited by any path are dropped.
public static class MapGenerator
{
    public static RunMap Generate(MapGenerationSettings settings, int seed)
    {
        Random random = new Random(seed);
        RunMap map = GenerateLayout(settings.floorCount, settings.columnCount, settings.pathCount, random);
        MapNodeTypeAssigner.Assign(map, settings, random);
        return map;
    }

    // Rooms and paths only, every room but the boss is a combat
    public static RunMap GenerateLayout(int floorCount, int columnCount, int pathCount, Random random)
    {
        if (floorCount < 1 || columnCount < 1 || pathCount < 1)
        {
            throw new ArgumentException($"Invalid map size: {floorCount} floors, {columnCount} columns, {pathCount} paths");
        }

        MapNode[,] grid = new MapNode[floorCount, columnCount];

        int firstStartColumn = -1;
        for (int path = 0; path < pathCount; path++)
        {
            int column = random.Next(columnCount);
            // Make sure the player has at least two starting rooms to choose from
            if (path == 1 && columnCount > 1)
            {
                while (column == firstStartColumn)
                {
                    column = random.Next(columnCount);
                }
            }
            if (path == 0)
            {
                firstStartColumn = column;
            }

            MapNode current = GetOrCreateNode(grid, 0, column);
            for (int floor = 1; floor < floorCount; floor++)
            {
                int nextColumn = PickNextColumn(grid, floor - 1, current.column, columnCount, random);
                MapNode nextNode = GetOrCreateNode(grid, floor, nextColumn);
                current.Connect(nextNode);
                current = nextNode;
            }
        }

        List<List<MapNode>> floors = new List<List<MapNode>>();
        for (int floor = 0; floor < floorCount; floor++)
        {
            List<MapNode> nodes = new List<MapNode>();
            for (int column = 0; column < columnCount; column++)
            {
                if (grid[floor, column] != null)
                {
                    nodes.Add(grid[floor, column]);
                }
            }
            floors.Add(nodes);
        }

        MapNode boss = new MapNode(floorCount, columnCount / 2, MapNodeType.Boss);
        foreach (MapNode node in floors[floorCount - 1])
        {
            node.Connect(boss);
        }

        return new RunMap(floors, boss, columnCount);
    }

    static MapNode GetOrCreateNode(MapNode[,] grid, int floor, int column)
    {
        if (grid[floor, column] == null)
        {
            grid[floor, column] = new MapNode(floor, column, MapNodeType.Combat);
        }
        return grid[floor, column];
    }

    // Going straight up can never cross an edge, so there is always at least one candidate
    static int PickNextColumn(MapNode[,] grid, int floor, int column, int columnCount, Random random)
    {
        List<int> candidates = new List<int>(3);
        for (int nextColumn = column - 1; nextColumn <= column + 1; nextColumn++)
        {
            if (nextColumn >= 0 && nextColumn < columnCount && !CrossesExistingEdge(grid, floor, column, nextColumn))
            {
                candidates.Add(nextColumn);
            }
        }
        return candidates[random.Next(candidates.Count)];
    }

    // A diagonal edge crosses the opposite diagonal between the same two columns
    static bool CrossesExistingEdge(MapNode[,] grid, int floor, int column, int nextColumn)
    {
        if (nextColumn == column)
        {
            return false;
        }

        MapNode neighbour = grid[floor, nextColumn];
        MapNode neighbourTarget = grid[floor + 1, column];
        return neighbour != null && neighbourTarget != null && neighbour.IsConnectedTo(neighbourTarget);
    }
}
