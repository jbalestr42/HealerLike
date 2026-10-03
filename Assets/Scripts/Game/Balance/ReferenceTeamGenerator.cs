using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Builds the reference teams of a run: the starting units of the character, plus one random reward for every
// rewarding room of a random path of a generated map, like a player picking at random among the choices
public static class ReferenceTeamGenerator
{
    // The rooms ending with a reward: the fights (events falling back to one), the treasures and the events,
    // counted as one reward of the usual kind (Recruit gives a unit, the libraries an item)
    public static bool GivesReward(MapNodeType roomType)
    {
        return roomType == MapNodeType.Combat
            || roomType == MapNodeType.Elite
            || roomType == MapNodeType.Treasure
            || roomType == MapNodeType.Event;
    }

    // From a random start room to the boss, going each time to a random next room
    public static List<MapNode> PickPath(RunMap map, System.Random random)
    {
        List<MapNode> path = new List<MapNode>();
        if (map.startNodes.Count == 0)
        {
            return path;
        }

        MapNode node = map.startNodes[random.Next(map.startNodes.Count)];
        while (node != null)
        {
            path.Add(node);
            node = node.next.Count > 0 ? node.next[random.Next(node.next.Count)] : null;
        }
        return path;
    }

    // One team per room of the path, as the player has it when entering the room
    public static List<ReferenceTeam> GenerateRun(IReadOnlyList<MapNode> path, IReadOnlyList<EntityData> startingUnits, RewardPools pools, System.Random random)
    {
        List<ReferenceTeam> teams = new List<ReferenceTeam>();
        ReferenceTeam team = new ReferenceTeam { units = startingUnits.Where(unit => unit != null).ToList() };
        foreach (MapNode node in path)
        {
            team = team.Copy(node.floor);
            teams.Add(team);

            if (GivesReward(node.type))
            {
                // Copied again so the reward only shows in the next rooms
                team = team.Copy(node.floor);
                AddRandomReward(team, pools, random);
            }
        }
        return teams;
    }

    public static List<ReferenceTeam> GenerateRun(MapGenerationSettings settings, IReadOnlyList<EntityData> startingUnits, RewardPools pools, int seed)
    {
        System.Random random = new System.Random(seed);
        RunMap map = MapGenerator.Generate(settings, seed);
        return GenerateRun(PickPath(map, random), startingUnits, pools, random);
    }

    // Same roll as the reward screen: a unit, a player item or a unit item. Nothing when the pool is empty
    public static void AddRandomReward(ReferenceTeam team, RewardPools pools, System.Random random)
    {
        RewardChoiceType type = UpgradeView.PickChoiceType((float)random.NextDouble(), (float)random.NextDouble(), pools.unitChance, pools.playerItemChance, pools.units.Count > 0);
        team.rewardCount++;
        if (type == RewardChoiceType.Unit)
        {
            team.units.Add(pools.units[random.Next(pools.units.Count)]);
        }
        else if (type == RewardChoiceType.PlayerItem)
        {
            if (pools.playerItems.Count > 0)
            {
                team.playerItems.Add(pools.playerItems[random.Next(pools.playerItems.Count)]);
            }
        }
        else if (pools.unitItems.Count > 0 && team.units.Count > 0)
        {
            AItemFactory item = pools.unitItems[random.Next(pools.unitItems.Count)];
            team.unitItems.Add(new ReferenceTeam.UnitItem { unit = GetUnitFor(team, item), item = item });
        }
    }

    // A Tank item goes to the best tank, a Damage item to the best damage dealer, the others spread over the team
    public static int GetUnitFor(ReferenceTeam team, AItemFactory item)
    {
        if (item != null && item.HasTag(TagNames.Tank))
        {
            return GetBestUnit(team.units, TagNames.Tank, GetMaxHealth);
        }
        if (item != null && item.HasTag(TagNames.Damage))
        {
            return GetBestUnit(team.units, TagNames.Damage, GetDamagePerSecond);
        }
        return GetUnitWithFewestItems(team);
    }

    // The unit with the highest score among the ones with the role, or among all of them when none has it.
    // Ties go to the first one
    public static int GetBestUnit(IReadOnlyList<EntityData> units, string role, System.Func<EntityData, float> score)
    {
        bool anyWithRole = units.Any(unit => unit != null && unit.HasTag(role));
        int best = -1;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == null || (anyWithRole && !units[i].HasTag(role)))
            {
                continue;
            }
            if (best < 0 || score(units[i]) > score(units[best]))
            {
                best = i;
            }
        }
        return Mathf.Max(best, 0);
    }

    // Damage of an attack over the time between two attacks (AttackRate), 0 for the units that don't attack
    public static float GetDamagePerSecond(EntityData unit)
    {
        float attackRate = GetAttribute(unit, AttributeType.AttackRate);
        return attackRate > 0f ? GetAttribute(unit, AttributeType.Damage) / attackRate : 0f;
    }

    // The first unit holding the fewest items, so the items spread over the team
    public static int GetUnitWithFewestItems(ReferenceTeam team)
    {
        int best = 0;
        for (int i = 1; i < team.units.Count; i++)
        {
            if (team.GetItemCount(i) < team.GetItemCount(best))
            {
                best = i;
            }
        }
        return best;
    }

    // Placement order, the front first: the units with the most max health, ties in team order
    public static List<int> GetPlacementOrder(IReadOnlyList<EntityData> units)
    {
        return Enumerable.Range(0, units.Count)
            .OrderByDescending(i => GetMaxHealth(units[i]))
            .ToList();
    }

    public static float GetMaxHealth(EntityData unit)
    {
        return GetAttribute(unit, AttributeType.HealthMax);
    }

    // Base value of the unit, 0 without the attribute
    static float GetAttribute(EntityData unit, AttributeType type)
    {
        return unit != null && unit.attributes != null && unit.attributes.TryGetValue(type, out float value) ? value : 0f;
    }
}
