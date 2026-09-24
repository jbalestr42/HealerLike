using System;
using System.Collections.Generic;

// Gives a type to every room of a generated map:
// - fixed floors: first floor = combats, treasure floor, last floor = rests
// - other rooms are drawn from the settings weights, without elites/rests on the lowest floors
//   and without two special rooms of the same type in a row on a path (combats can follow each other)
public static class MapNodeTypeAssigner
{
    static readonly MapNodeType[] RandomTypes = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Rest, MapNodeType.Treasure };

    public static void Assign(RunMap map, MapGenerationSettings settings, Random random)
    {
        HashSet<MapNode> assigned = new HashSet<MapNode>();

        // Fixed floors first, so the random rooms right below them can avoid repeating their type
        for (int floor = 0; floor < map.floorCount; floor++)
        {
            MapNodeType? fixedType = GetFixedType(floor, map.floorCount, settings);
            if (fixedType.HasValue)
            {
                foreach (MapNode node in map.floors[floor])
                {
                    node.type = fixedType.Value;
                    assigned.Add(node);
                }
            }
        }

        for (int floor = 0; floor < map.floorCount; floor++)
        {
            foreach (MapNode node in map.floors[floor])
            {
                if (!assigned.Contains(node))
                {
                    node.type = PickRandomType(node, settings, assigned, random);
                    assigned.Add(node);
                }
            }
        }
    }

    static MapNodeType? GetFixedType(int floor, int floorCount, MapGenerationSettings settings)
    {
        if (floor == 0)
        {
            return MapNodeType.Combat;
        }
        if (settings.restBeforeBoss && floor == floorCount - 1)
        {
            return MapNodeType.Rest;
        }
        if (floor == settings.treasureFloor)
        {
            return MapNodeType.Treasure;
        }
        return null;
    }

    static bool IsAllowed(MapNodeType type, MapNode node, MapGenerationSettings settings, HashSet<MapNode> assigned)
    {
        if (type == MapNodeType.Combat)
        {
            return true;
        }
        if (type == MapNodeType.Elite && node.floor < settings.firstEliteFloor)
        {
            return false;
        }
        if (type == MapNodeType.Rest && node.floor < settings.firstRestFloor)
        {
            return false;
        }
        return !HasAssignedNeighbourOfType(node.previous, type, assigned) && !HasAssignedNeighbourOfType(node.next, type, assigned);
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

    // Weighted draw among the allowed types, combat when nothing else is possible
    static MapNodeType PickRandomType(MapNode node, MapGenerationSettings settings, HashSet<MapNode> assigned, Random random)
    {
        List<MapNodeType> candidates = new List<MapNodeType>(RandomTypes.Length);
        float totalWeight = 0f;
        foreach (MapNodeType type in RandomTypes)
        {
            if (GetWeight(type, settings) > 0f && IsAllowed(type, node, settings, assigned))
            {
                candidates.Add(type);
                totalWeight += GetWeight(type, settings);
            }
        }

        if (candidates.Count == 0)
        {
            return MapNodeType.Combat;
        }

        double roll = random.NextDouble() * totalWeight;
        foreach (MapNodeType type in candidates)
        {
            roll -= GetWeight(type, settings);
            if (roll < 0)
            {
                return type;
            }
        }
        return candidates[candidates.Count - 1];
    }

    static float GetWeight(MapNodeType type, MapGenerationSettings settings)
    {
        switch (type)
        {
            case MapNodeType.Combat:
                return settings.combatWeight;
            case MapNodeType.Elite:
                return settings.eliteWeight;
            case MapNodeType.Rest:
                return settings.restWeight;
            case MapNodeType.Treasure:
                return settings.treasureWeight;
            default:
                return 0f;
        }
    }
}
