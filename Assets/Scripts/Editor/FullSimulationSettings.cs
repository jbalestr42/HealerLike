using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// What the full simulation of the Balance Report plays: the characters and the waves picked, each wave on every
// floor or only on the floors of its pools, and the seeds. Saved in the editor preferences
[Serializable]
public class FullSimulationSettings
{
    const string PrefsKey = "HealerLike.FullSimulation.Settings";

    // By name, so a new bot or a new wave is played until it is unticked
    public List<string> excludedBots = new List<string>();
    public List<string> excludedWaves = new List<string>();
    public bool onlyPoolFloors = true;
    public int poolFloorMargin = 1;
    public int seedCount = 3;

    public static FullSimulationSettings Load()
    {
        string json = EditorPrefs.GetString(PrefsKey, "");
        FullSimulationSettings settings = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<FullSimulationSettings>(json);
        return settings ?? new FullSimulationSettings();
    }

    public void Save()
    {
        EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
    }

    public bool IsBotPlayed(HealerBotProfile bot) => bot != null && !excludedBots.Contains(bot.name);
    public bool IsWavePlayed(WavePatternData wave) => wave != null && !excludedWaves.Contains(wave.name);

    public void SetBotPlayed(HealerBotProfile bot, bool isPlayed) => SetExcluded(excludedBots, bot.name, !isPlayed);
    public void SetWavePlayed(WavePatternData wave, bool isPlayed) => SetExcluded(excludedWaves, wave.name, !isPlayed);

    static void SetExcluded(List<string> excluded, string name, bool isExcluded)
    {
        excluded.Remove(name);
        if (isExcluded)
        {
            excluded.Add(name);
        }
    }

    // A copy of the full simulation plan restricted to the settings, kept in memory only
    public SimulationPlan BuildPlan(SimulationPlan basePlan, GameData data)
    {
        SimulationPlan plan = UnityEngine.Object.Instantiate(basePlan);
        plan.name = basePlan.name;
        plan.hideFlags = HideFlags.DontSave;
        plan.healerBots = basePlan.healerBots.Where(IsBotPlayed).ToList();
        plan.waves = basePlan.GetWaves(data).Select(simulated => simulated.wave).Where(IsWavePlayed).ToList();
        plan.onlyPoolFloors = onlyPoolFloors;
        plan.poolFloorMargin = Mathf.Max(0, poolFloorMargin);
        plan.seedCount = Mathf.Max(1, seedCount);
        return plan;
    }
}
