using System;
using System.Collections.Generic;

// Slay the Spire like map: paths climb one floor at a time, moving at most one column left or right,
// without ever crossing an existing edge. The rooms not visited by any path are dropped.
public static class MapGenerator
{
    // Layouts drawn again when a path gets stuck on a full floor, before giving up on the room limit
    const int MaxAttempts = 100;

    public static RunMap Generate(MapGenerationSettings settings, int seed)
    {
        Random random = new Random(seed);
        RunMap map = GenerateLayout(settings.floorCount, settings.columnCount, settings.pathCount, random, settings.startRoomCount, settings.maxRoomsPerFloor);
        MapNodeTypeAssigner.Assign(map, settings, random);
        return map;
    }

    // Rooms and paths only, every room but the boss is a combat. startRoomCount rooms on the first floor
    // (at most one per path and per column), or 0 to let each path pick its own (at least two different).
    // maxRoomsPerFloor rooms at most on a floor whatever the column count, 0 for no limit.
    public static RunMap GenerateLayout(int floorCount, int columnCount, int pathCount, Random random, int startRoomCount = 0, int maxRoomsPerFloor = 0)
    {
        if (floorCount < 1 || columnCount < 1 || pathCount < 1)
        {
            throw new ArgumentException($"Invalid map size: {floorCount} floors, {columnCount} columns, {pathCount} paths");
        }

        // A path can end up with no room in reach on a full floor: the whole layout is drawn again
        if (maxRoomsPerFloor > 0)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                RunMap map = TryGenerateLayout(floorCount, columnCount, pathCount, random, startRoomCount, maxRoomsPerFloor);
                if (map != null)
                {
                    return map;
                }
            }
            UnityEngine.Debug.LogWarning($"[MapGenerator] No layout with at most {maxRoomsPerFloor} rooms per floor after {MaxAttempts} attempts, drawn without the limit");
        }
        return TryGenerateLayout(floorCount, columnCount, pathCount, random, startRoomCount, 0);
    }

    // Null when a path gets stuck (never without a room limit: going straight up is always possible)
    static RunMap TryGenerateLayout(int floorCount, int columnCount, int pathCount, Random random, int startRoomCount, int maxRoomsPerFloor)
    {
        MapNode[,] grid = new MapNode[floorCount, columnCount];

        List<int> startColumns = PickStartColumns(columnCount, pathCount, startRoomCount, maxRoomsPerFloor, random);
        int firstStartColumn = -1;
        for (int path = 0; path < pathCount; path++)
        {
            int column;
            if (startColumns != null)
            {
                // Each start room gets a path first, the other paths share them
                column = path < startColumns.Count ? startColumns[path] : startColumns[random.Next(startColumns.Count)];
            }
            else if (IsFull(grid, 0, maxRoomsPerFloor))
            {
                column = PickExistingColumn(grid, 0, random);
            }
            else
            {
                column = random.Next(columnCount);
                // Make sure the player has at least two starting rooms to choose from
                if (path == 1 && columnCount > 1 && maxRoomsPerFloor != 1)
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
            }

            MapNode current = GetOrCreateNode(grid, 0, column);
            for (int floor = 1; floor < floorCount; floor++)
            {
                int nextColumn = PickNextColumn(grid, floor - 1, current.column, columnCount, maxRoomsPerFloor, random);
                if (nextColumn < 0)
                {
                    return null;
                }
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

    // Distinct random columns for the start rooms, null to let the paths pick them
    static List<int> PickStartColumns(int columnCount, int pathCount, int startRoomCount, int maxRoomsPerFloor, Random random)
    {
        if (startRoomCount <= 0)
        {
            return null;
        }

        List<int> columns = new List<int>(columnCount);
        for (int column = 0; column < columnCount; column++)
        {
            columns.Add(column);
        }
        // Fisher-Yates, then the first ones are kept
        for (int i = columns.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (columns[i], columns[j]) = (columns[j], columns[i]);
        }
        int count = System.Math.Min(startRoomCount, System.Math.Min(columnCount, pathCount));
        if (maxRoomsPerFloor > 0)
        {
            count = System.Math.Min(count, maxRoomsPerFloor);
        }
        return columns.GetRange(0, count);
    }

    static MapNode GetOrCreateNode(MapNode[,] grid, int floor, int column)
    {
        if (grid[floor, column] == null)
        {
            grid[floor, column] = new MapNode(floor, column, MapNodeType.Combat);
        }
        return grid[floor, column];
    }

    // No new room can be added to the floor, the paths have to go through its rooms
    static bool IsFull(MapNode[,] grid, int floor, int maxRoomsPerFloor)
    {
        if (maxRoomsPerFloor <= 0)
        {
            return false;
        }

        int roomCount = 0;
        for (int column = 0; column < grid.GetLength(1); column++)
        {
            if (grid[floor, column] != null)
            {
                roomCount++;
            }
        }
        return roomCount >= maxRoomsPerFloor;
    }

    static int PickExistingColumn(MapNode[,] grid, int floor, Random random)
    {
        List<int> columns = new List<int>();
        for (int column = 0; column < grid.GetLength(1); column++)
        {
            if (grid[floor, column] != null)
            {
                columns.Add(column);
            }
        }
        return columns[random.Next(columns.Count)];
    }

    // -1 when no column is possible: only on a full floor, as going straight up can never cross an edge
    static int PickNextColumn(MapNode[,] grid, int floor, int column, int columnCount, int maxRoomsPerFloor, Random random)
    {
        bool isNextFloorFull = IsFull(grid, floor + 1, maxRoomsPerFloor);
        List<int> candidates = new List<int>(3);
        for (int nextColumn = column - 1; nextColumn <= column + 1; nextColumn++)
        {
            if (nextColumn < 0 || nextColumn >= columnCount || CrossesExistingEdge(grid, floor, column, nextColumn))
            {
                continue;
            }
            if (isNextFloorFull && grid[floor + 1, nextColumn] == null)
            {
                continue;
            }
            candidates.Add(nextColumn);
        }
        return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : -1;
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
