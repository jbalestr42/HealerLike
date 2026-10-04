using System;
using System.Collections.Generic;

// The units and items a player would typically have when reaching a floor, used to simulate the fights
[Serializable]
public class ReferenceTeam
{
    [Serializable]
    public struct UnitItem
    {
        // Index of the unit holding the item in units
        public int unit;
        public AItemFactory item;
    }

    public int floor;
    // Rewards received before reaching the floor
    public int rewardCount;
    public List<EntityData> units = new List<EntityData>();
    public List<UnitItem> unitItems = new List<UnitItem>();
    public List<AItemFactory> playerItems = new List<AItemFactory>();

    // The units of a wave as a team without reward
    public static ReferenceTeam FromWave(WavePatternData wave)
    {
        ReferenceTeam team = new ReferenceTeam();
        if (wave == null || wave.slots == null)
        {
            return team;
        }

        foreach (EntitySlot slot in wave.slots)
        {
            if (slot.entity != null)
            {
                team.units.Add(slot.entity);
            }
        }
        return team;
    }

    public ReferenceTeam Copy(int newFloor)
    {
        return new ReferenceTeam
        {
            floor = newFloor,
            rewardCount = rewardCount,
            units = new List<EntityData>(units),
            unitItems = new List<UnitItem>(unitItems),
            playerItems = new List<AItemFactory>(playerItems),
        };
    }

    public int GetItemCount(int unit)
    {
        return unitItems.FindAll(unitItem => unitItem.unit == unit).Count;
    }

    // e.g. "Floor 3, 2 rewards: Knight [Iron Shield], Archer | Player items: Tome"
    public string Describe()
    {
        List<string> unitNames = new List<string>();
        for (int i = 0; i < units.Count; i++)
        {
            List<string> items = unitItems.FindAll(unitItem => unitItem.unit == i).ConvertAll(unitItem => GetName(unitItem.item));
            unitNames.Add(items.Count > 0 ? $"{GetName(units[i])} [{string.Join(", ", items)}]" : GetName(units[i]));
        }

        string description = $"Floor {floor}, {rewardCount} rewards: {string.Join(", ", unitNames)}";
        if (playerItems.Count > 0)
        {
            description += $" | Player items: {string.Join(", ", playerItems.ConvertAll(GetName))}";
        }
        return description;
    }

    static string GetName(EntityData unit)
    {
        return unit != null ? unit.title : "None";
    }

    static string GetName(AItemFactory item)
    {
        return item != null ? item.title : "None";
    }
}
