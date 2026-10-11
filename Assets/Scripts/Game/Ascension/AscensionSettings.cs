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
