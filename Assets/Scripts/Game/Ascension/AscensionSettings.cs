using System;
using System.Collections.Generic;
using UnityEngine;

// One step of difficulty, added on top of the ones of the lower levels
[Serializable]
public class AscensionLevel
{
    public string title;

    // Shown to the player when choosing the level and during the run
    [TextArea] public string description;

    // What the level changes, everything left to its default changes nothing

    [Header("Map")]
    // Applied to the weight of the elite rooms on the map
    [Min(0f)] public float eliteWeightMultiplier = 1f;

    [Header("Character")]
    // Given to the character at the start of the run, shown with its other items (e.g. less mana back before each
    // fight, a smaller rest heal)
    public List<AItemFactory> characterItems = new List<AItemFactory>();

    [Header("Enemies")]
    // Given to every enemy of the fights in the enemyItemRooms (e.g. more health in the normal fights)
    public List<AItemFactory> enemyItems = new List<AItemFactory>();
    // The rooms whose enemies get the enemyItems, every fight when empty
    public List<MapNodeType> enemyItemRooms = new List<MapNodeType>();

    [Header("Rest")]
    // Applied to a unit resurrected at a rest room once placed on the board (e.g. a part of its health lost)
    public List<AConsumerFactory> resurrectConsumers = new List<AConsumerFactory>();
}

// Ascension levels a character unlocks one by one by winning a run. Level 0 plays the game without any malus,
// level N plays it with the maluses of the levels 1 to N, levels[0] being the level 1
[CreateAssetMenu(menuName = "Custom/AscensionSettings")]
public class AscensionSettings : ScriptableObject
{
    public List<AscensionLevel> levels = new List<AscensionLevel>();

    public int maxLevel => levels.Count;

    // Between 0 (no malus) and the highest level
    public int ClampLevel(int level)
    {
        return Mathf.Clamp(level, 0, maxLevel);
    }

    // The levels whose malus applies at this level, from the level 1 to this one
    public List<AscensionLevel> GetActiveLevels(int level)
    {
        return levels.GetRange(0, ClampLevel(level));
    }

    // The elite weight multipliers of the levels up to this one, multiplied together
    public float GetEliteWeightMultiplier(int level)
    {
        float multiplier = 1f;
        foreach (AscensionLevel active in GetActiveLevels(level))
        {
            multiplier *= active.eliteWeightMultiplier;
        }
        return multiplier;
    }

    public List<AItemFactory> GetCharacterItems(int level)
    {
        List<AItemFactory> items = new List<AItemFactory>();
        foreach (AscensionLevel active in GetActiveLevels(level))
        {
            items.AddRange(active.characterItems.FindAll(item => item != null));
        }
        return items;
    }

    // The enemy items of the levels up to this one given in a fight of this room type (Combat for a normal fight)
    public List<AItemFactory> GetEnemyItems(int level, MapNodeType roomType)
    {
        List<AItemFactory> items = new List<AItemFactory>();
        foreach (AscensionLevel active in GetActiveLevels(level))
        {
            if (active.enemyItemRooms.Count == 0 || active.enemyItemRooms.Contains(roomType))
            {
                items.AddRange(active.enemyItems.FindAll(item => item != null));
            }
        }
        return items;
    }

    public List<AConsumerFactory> GetResurrectConsumers(int level)
    {
        List<AConsumerFactory> consumers = new List<AConsumerFactory>();
        foreach (AscensionLevel active in GetActiveLevels(level))
        {
            consumers.AddRange(active.resurrectConsumers.FindAll(consumer => consumer != null));
        }
        return consumers;
    }

    // The maluses of the level, one per line, for a tooltip
    public string GetMalusText(int level)
    {
        List<AscensionLevel> active = GetActiveLevels(level);
        if (active.Count == 0)
        {
            return "No malus.";
        }

        List<string> lines = new List<string>(active.Count);
        for (int i = 0; i < active.Count; i++)
        {
            lines.Add($"{i + 1}. <b>{active[i].title}</b>: {active[i].description}");
        }
        return string.Join("\n", lines);
    }

    // The level and its maluses, e.g. on the character cards and in the game HUD
    public string GetTooltip(int level)
    {
        return $"<b>Ascension {ClampLevel(level)}</b>\n{GetMalusText(level)}";
    }

    // The malus of the level malusLevel (1 based) applies in a run played at runLevel
    public static bool IsActive(int malusLevel, int runLevel)
    {
        return malusLevel >= 1 && malusLevel <= runLevel;
    }
}
