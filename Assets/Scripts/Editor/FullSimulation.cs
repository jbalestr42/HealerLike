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

    public static bool isRunning
    {
        get => SessionState.GetBool(RunningKey, false);
        private set => SessionState.SetBool(RunningKey, value);
    }

    static FullSimulation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += OnUpdate;
    }

    public static void Start()
    {
        if (isRunning || ScoreMeasure.isRunning || EditorApplication.isPlayingOrWillChangePlaymode || !ScoreMeasure.OpenScene())
        {
            return;
        }

        SessionState.SetString(ScenePlanKey, ScoreMeasure.GetScenePlan());
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
        ScoreMeasure.SetScenePlan(SessionState.GetString(ScenePlanKey, ""));
        Debug.Log($"[FullSimulation] {Path.GetFileNameWithoutExtension(PlanPath)} over: open the latest log in the Balance Report");
    }
}
