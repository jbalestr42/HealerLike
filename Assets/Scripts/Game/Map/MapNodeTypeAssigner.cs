using System;
using System.Collections.Generic;

// Gives a type to every room of a generated map, like Slay the Spire, from the settings of each room type:
// - fixed floors: every room of the floor is of the type listing it
// - the other rooms share a shuffled list holding each type in proportion to its weight, each room taking
//   the first type of the list it's allowed to be (combat when none): not below the first floor of the
//   type, not twice in a row on a path unless the type can follow itself (combats), and no two rooms
//   reachable from the same room sharing a type
public static class MapNodeTypeAssigner
{
    // Fills the rooms no other type fits, and the share the other types leave
    public const MapNodeType FallbackType = MapNodeType.Combat;

    public static void Assign(RunMap map, MapGenerationSettings settings, Random random)
    {
        HashSet<MapNode> assigned = new HashSet<MapNode>();

        // Fixed floors first, so the random rooms right below them can avoid repeating their type
        for (int floor = 0; floor < map.floorCount; floor++)
        {
            MapNodeType? fixedType = settings.GetFixedType(floor);
            if (fixedType.HasValue)
            {
                foreach (MapNode node in map.floors[floor])
                {
                    node.type = fixedType.Value;
                    assigned.Add(node);
                }
            }
        }

        List<MapNode> randomRooms = new List<MapNode>();
        for (int floor = 0; floor < map.floorCount; floor++)
        {
            foreach (MapNode node in map.floors[floor])
            {
                if (!assigned.Contains(node))
                {
                    randomRooms.Add(node);
                }
            }
        }

        List<MapNodeType> types = CreateTypeList(randomRooms.Count, settings, random);
        foreach (MapNode node in randomRooms)
        {
            node.type = TakeFirstAllowedType(types, node, settings, assigned);
            assigned.Add(node);
        }
    }

    // Each type in proportion to its weight among the rooms to fill, the fallback filling the rest, shuffled
    public static List<MapNodeType> CreateTypeList(int roomCount, MapGenerationSettings settings, Random random)
    {
        List<MapNodeType> types = new List<MapNodeType>(roomCount);
        float totalWeight = 0f;
        foreach (RoomTypeSettings roomType in settings.roomTypes)
        {
            totalWeight += roomType.weight;
        }

        if (totalWeight > 0f)
        {
            foreach (RoomTypeSettings roomType in settings.roomTypes)
            {
                if (roomType.type == FallbackType)
                {
                    continue;
                }

                int count = (int)System.Math.Round(roomCount * roomType.weight / totalWeight, MidpointRounding.AwayFromZero);
                for (int i = 0; i < count && types.Count < roomCount; i++)
                {
                    types.Add(roomType.type);
                }
            }
        }

        // Without any fallback weight, the rooms left over still need a type
        while (types.Count < roomCount)
        {
            types.Add(FallbackType);
        }

        // Fisher-Yates, with the seeded random so a seed always gives the same map
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }
        return types;
    }

    // The first type of the list the room can be, removed from it; the fallback when none fits (the type
    // left in the list is simply not used)
    static MapNodeType TakeFirstAllowedType(List<MapNodeType> types, MapNode node, MapGenerationSettings settings, HashSet<MapNode> assigned)
    {
        for (int i = 0; i < types.Count; i++)
        {
            if (IsAllowed(types[i], node, settings, assigned))
            {
                MapNodeType type = types[i];
                types.RemoveAt(i);
                return type;
            }
        }
        return FallbackType;
    }

    static bool IsAllowed(MapNodeType type, MapNode node, MapGenerationSettings settings, HashSet<MapNode> assigned)
    {
        RoomTypeSettings roomType = settings.GetRoomType(type);
        if (roomType == null || node.floor < roomType.firstFloor || HasAssignedSiblingOfType(node, type, assigned))
        {
            return false;
        }
        return roomType.canFollowItself || (!HasAssignedNeighbourOfType(node.previous, type, assigned) && !HasAssignedNeighbourOfType(node.next, type, assigned));
    }

    // The rooms reachable from the same room as this one must all be different
    static bool HasAssignedSiblingOfType(MapNode node, MapNodeType type, HashSet<MapNode> assigned)
    {
        foreach (MapNode parent in node.previous)
        {
            foreach (MapNode sibling in parent.next)
            {
                if (sibling != node && assigned.Contains(sibling) && sibling.type == type)
                {
                    return true;
                }
            }
        }
        return false;
    }

    static bool HasAssignedNeighbourOfType(IReadOnlyList<MapNode> neighbours, MapNodeType type, HashSet<MapNode> assigned)
    {
        foreach (MapNode neighbour in neighbours)
        {
            if (assigned.Contains(neighbour) && neighbour.type == type)
            {
                return true;
            }
        }
        return false;
    }
}
