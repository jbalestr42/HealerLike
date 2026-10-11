using System.IO;
using UnityEditor;
using UnityEngine;

// Plays the full balance simulation (every wave on every floor, every character) in the simulation scene, then
// leaves the play mode once its last fight is written, for the Balance Report to read it. The scene keeps its own
// plan on the disk. The state survives the domain reloads of the play mode
[InitializeOnLoad]
public static class FullSimulation
{
    public const string PlanPath = "Assets/Data/Balance/AllWavesAllFloorsSimulation.asset";

    const string RunningKey = "HealerLike.FullSimulation.Running";
    // Plan of the scene before the simulation, put back after it
    const string ScenePlanKey = "HealerLike.FullSimulation.ScenePlan";
    // Whether the simulation plays the plan restricted to the FullSimulationSettings
    const string UseSettingsKey = "HealerLike.FullSimulation.UseSettings";

    public static bool isRunning
    {
        get => SessionState.GetBool(RunningKey, false);
        private set => SessionState.SetBool(RunningKey, value);
    }

    static FullSimulation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += OnUpdate;
        // Built again after the domain reload of the play mode, before the simulation starts: the restricted plan
        // only lives in memory
        if (isRunning && SessionState.GetBool(UseSettingsKey, false))
        {
            BalanceSimulator.planOverride = BuildSettingsPlan();
        }
    }

    // The plan at PlanPath restricted to the settings, null without plan or game data
    public static SimulationPlan BuildSettingsPlan()
    {
        SimulationPlan basePlan = AssetDatabase.LoadAssetAtPath<SimulationPlan>(PlanPath);
        GameData data = BalanceReportWindow.LoadGameData();
        return basePlan != null && data != null ? FullSimulationSettings.Load().BuildPlan(basePlan, data) : null;
    }

    // The plan at PlanPath, restricted to the FullSimulationSettings with useSettings
    public static void Start(bool useSettings)
    {
        if (isRunning || ScoreMeasure.isRunning || EditorApplication.isPlayingOrWillChangePlaymode || !ScoreMeasure.OpenScene())
        {
            return;
        }

        SessionState.SetString(ScenePlanKey, ScoreMeasure.GetScenePlan());
        SessionState.SetBool(UseSettingsKey, useSettings);
        ScoreMeasure.SetScenePlan(PlanPath);
        isRunning = true;
        EditorApplication.isPlaying = true;
    }

    // The fights played so far stay in the log
    public static void Stop()
    {
        EditorApplication.isPlaying = false;
    }

    static void OnUpdate()
    {
        if (!isRunning || !EditorApplication.isPlaying)
        {
            return;
        }

        BalanceSimulator simulator = Object.FindAnyObjectByType<BalanceSimulator>();
        if (simulator != null && simulator.IsOver())
        {
            EditorApplication.isPlaying = false;
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode || !isRunning)
        {
            return;
        }

        isRunning = false;
        SessionState.SetBool(UseSettingsKey, false);
        // Never left for another simulation (e.g. a score measure), even when this one stopped before using it
        BalanceSimulator.planOverride = null;
        ScoreMeasure.SetScenePlan(SessionState.GetString(ScenePlanKey, ""));
        Debug.Log($"[FullSimulation] {Path.GetFileNameWithoutExtension(PlanPath)} over: open the latest log in the Balance Report");
    }
}
