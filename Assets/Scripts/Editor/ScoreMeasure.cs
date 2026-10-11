using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Measures the scores of the waves or of the characters, in the simulation scene, one plan after the other:
// - the waves: the balance team against the dummies, the waves against the dummies, then the waves against the
//   balance team; the scores and their fingerprint are written into the waves
// - the characters (their starting units, items and skills): the balance team against the dummies, the starting
//   units against the dummies, then against the balance team without and with the skills of the character
// - the reward items, one by one: the balance team against the dummies, one of its units holding the item, then
//   the dummies against the balance team, one of them holding the item
// Each measure has its own folder (Logs/WaveScore/<date>/, Logs/CharacterScore/<date>/ or Logs/ItemScore/<date>/), apart from the balance
// simulations: one log per plan, and scores.json once every plan played to its end. A measure stopped before is
// deleted. The step survives the domain reloads of the play mode
[InitializeOnLoad]
public static class ScoreMeasure
{
    public enum Kind
    {
        Waves,
        Characters,
        Items,
    }

    public const string ScenePath = "Assets/Scenes/BalanceSimulation.unity";
    public const string TeamDpsPlanPath = "Assets/Data/Balance/BalanceTeamDpsSimulation.asset";
    public const string WaveDpsPlanPath = "Assets/Data/Balance/WaveDpsSimulation.asset";
    public const string WaveRobustnessPlanPath = "Assets/Data/Balance/WaveRobustnessSimulation.asset";
    public const string CharacterDpsPlanPath = "Assets/Data/Balance/CharacterDpsSimulation.asset";
    public const string CharacterRobustnessPlanPath = "Assets/Data/Balance/CharacterRobustnessSimulation.asset";
    public const string CharacterSpellDpsPlanPath = "Assets/Data/Balance/CharacterSpellDpsSimulation.asset";
    public const string CharacterSpellRobustnessPlanPath = "Assets/Data/Balance/CharacterSpellRobustnessSimulation.asset";
    public const string ItemDpsPlanPath = "Assets/Data/Balance/ItemDpsSimulation.asset";
    public const string ItemRobustnessPlanPath = "Assets/Data/Balance/ItemRobustnessSimulation.asset";
    static readonly string[] WavePlanPaths = { TeamDpsPlanPath, WaveDpsPlanPath, WaveRobustnessPlanPath };
    static readonly string[] CharacterPlanPaths = { TeamDpsPlanPath, CharacterDpsPlanPath, CharacterRobustnessPlanPath, CharacterSpellDpsPlanPath, CharacterSpellRobustnessPlanPath };
    static readonly string[] ItemPlanPaths = { ItemDpsPlanPath, ItemRobustnessPlanPath };

    // -1 when not measuring
    const string StepKey = "HealerLike.ScoreMeasure.Step";
    const string KindKey = "HealerLike.ScoreMeasure.Kind";
    // Plan of the scene before the measure, put back after it
    const string ScenePlanKey = "HealerLike.ScoreMeasure.ScenePlan";
    // Whether the simulation of the step played to its end: leaving the play mode before cancels the measure
    const string StepOverKey = "HealerLike.ScoreMeasure.StepOver";
    // Folder of the running measure
    const string FolderKey = "HealerLike.ScoreMeasure.Folder";
    // Asset path of the only wave, character or item measured, empty to measure every one
    const string TargetKey = "HealerLike.ScoreMeasure.Target";
    // Folder of a previous item measure whose fights without item are taken again, empty to play them
    const string BaselineKey = "HealerLike.ScoreMeasure.Baseline";

    static int step
    {
        get => SessionState.GetInt(StepKey, -1);
        set => SessionState.SetInt(StepKey, value);
    }

    static string folder
    {
        get => SessionState.GetString(FolderKey, "");
        set => SessionState.SetString(FolderKey, value);
    }

    public static Kind runningKind
    {
        get => (Kind)SessionState.GetInt(KindKey, 0);
        private set => SessionState.SetInt(KindKey, (int)value);
    }

    // The only wave, character or item measured, null when measuring every one
    public static UnityEngine.Object target
    {
        get
        {
            string path = SessionState.GetString(TargetKey, "");
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }

    public static bool isRunning => step >= 0;
    static string[] planPaths => GetPlanPaths(runningKind);
    static string measuredName => target != null ? target.name : "the " + runningKind.ToString().ToLower();
    public static string status => isRunning ? $"Measuring {measuredName} {step + 1}/{planPaths.Length}: {Path.GetFileNameWithoutExtension(planPaths[step])}" : "";

    static ScoreMeasure()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += OnUpdate;
        // Loaded again after the domain reload of the play mode, before the simulation starts, with the plan
        // restricted to the target, which only lives in memory
        BalanceSimulator.outputFolder = isRunning ? folder : null;
        if (isRunning)
        {
            BalanceSimulator.planOverride = BuildStepPlan();
        }
    }

    public static string[] GetPlanPaths(Kind kind)
    {
        switch (kind)
        {
            case Kind.Waves:
                return WavePlanPaths;
            case Kind.Characters:
                return CharacterPlanPaths;
            default:
                return ItemPlanPaths;
        }
    }

    public static string GetRoot(Kind kind)
    {
        switch (kind)
        {
            case Kind.Waves:
                return ScoreFile.waveRoot;
            case Kind.Characters:
                return ScoreFile.characterRoot;
            default:
                return ScoreFile.itemRoot;
        }
    }

    // targetAsset: the only wave, character or item of the kind to measure, null for every one
    public static void Start(Kind kind, UnityEngine.Object targetAsset = null)
    {
        if (isRunning || FullSimulation.isRunning || EditorApplication.isPlayingOrWillChangePlaymode || !OpenScene())
        {
            return;
        }

        SessionState.SetString(ScenePlanKey, GetScenePlan());
        runningKind = kind;
        SessionState.SetString(TargetKey, targetAsset != null ? AssetDatabase.GetAssetPath(targetAsset) : "");
        // A single item: the fights without item of the latest measure, when its setup didn't change since
        string baseline = kind == Kind.Items && targetAsset != null ? ScoreFile.FindItemBaseline(ScoreFile.itemRoot, ComputeItemSetupFingerprint(), GetLogNames(ItemPlanPaths)) : null;
        SessionState.SetString(BaselineKey, baseline ?? "");
        folder = Path.Combine(GetRoot(kind), DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        step = 0;
        PlayStep();
    }

    public static void Cancel()
    {
        // Seen as stopped before its end when the play mode is left
        SessionState.SetBool(StepOverKey, false);
        EditorApplication.isPlaying = false;
    }

    static void PlayStep()
    {
        SetScenePlan(planPaths[step]);
        SessionState.SetBool(StepOverKey, false);
        BalanceSimulator.outputFolder = folder;
        BalanceSimulator.planOverride = BuildStepPlan();
        EditorApplication.isPlaying = true;
    }

    // The simulation scene active, false when the user keeps the current one or it has no simulator
    public static bool OpenScene()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }
            EditorSceneManager.OpenScene(ScenePath);
        }

        if (UnityEngine.Object.FindAnyObjectByType<BalanceSimulator>() == null)
        {
            Debug.LogError($"[ScoreMeasure] No BalanceSimulator in {ScenePath}");
            return false;
        }
        return true;
    }

    // The path of the plan of the scene, empty without one
    public static string GetScenePlan()
    {
        BalanceSimulator simulator = UnityEngine.Object.FindAnyObjectByType<BalanceSimulator>();
        UnityEngine.Object plan = simulator != null ? new SerializedObject(simulator).FindProperty("_plan").objectReferenceValue : null;
        return plan != null ? AssetDatabase.GetAssetPath(plan) : "";
    }

    // Not saved: the scene keeps its own plan on the disk
    public static void SetScenePlan(string planPath)
    {
        BalanceSimulator simulator = UnityEngine.Object.FindAnyObjectByType<BalanceSimulator>();
        if (simulator == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(simulator);
        serialized.FindProperty("_plan").objectReferenceValue = string.IsNullOrEmpty(planPath) ? null : AssetDatabase.LoadAssetAtPath<SimulationPlan>(planPath);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void OnUpdate()
    {
        if (!isRunning || !EditorApplication.isPlaying)
        {
            return;
        }

        BalanceSimulator simulator = UnityEngine.Object.FindAnyObjectByType<BalanceSimulator>();
        if (simulator != null && simulator.IsOver())
        {
            SessionState.SetBool(StepOverKey, true);
            EditorApplication.isPlaying = false;
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode || !isRunning)
        {
            return;
        }

        // Cancelled or stopped by hand: its logs only hold the fights played so far
        if (!SessionState.GetBool(StepOverKey, false))
        {
            Debug.LogWarning($"[ScoreMeasure] {Path.GetFileNameWithoutExtension(planPaths[step])} stopped before its end: measure cancelled, no score written");
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            End();
            return;
        }

        step++;
        if (step < planPaths.Length)
        {
            // Not while the play mode is still being left
            EditorApplication.delayCall += PlayStep;
            return;
        }

        string measured = folder;
        Kind kind = runningKind;
        bool partial = target != null;
        string baselineFolder = SessionState.GetString(BaselineKey, "");
        End();
        int written = kind == Kind.Waves ? WriteWaveScores(measured, partial) : kind == Kind.Characters ? WriteCharacterScores(measured, partial) : WriteItemScores(measured, partial, baselineFolder);
        Debug.Log($"[ScoreMeasure] Scores of {written} {kind.ToString().ToLower()} written into {Path.Combine(measured, ScoreFile.FileName)}");
    }

    // The plan of the step restricted to the target, null when measuring every one (the plan of the scene)
    static SimulationPlan BuildStepPlan()
    {
        UnityEngine.Object measured = target;
        SimulationPlan plan = measured != null ? AssetDatabase.LoadAssetAtPath<SimulationPlan>(planPaths[step]) : null;
        return plan != null ? plan.CopyFor(measured, !string.IsNullOrEmpty(SessionState.GetString(BaselineKey, ""))) : null;
    }

    static void End()
    {
        step = -1;
        folder = "";
        SessionState.SetString(TargetKey, "");
        SessionState.SetString(BaselineKey, "");
        BalanceSimulator.outputFolder = null;
        BalanceSimulator.planOverride = null;
        SetScenePlan(SessionState.GetString(ScenePlanKey, ""));
    }

    // The fights of every plan of the measure, null when a log is missing
    static List<List<CombatStats>> ReadLogs(string measureFolder, string[] plans)
    {
        List<string> logs = plans.Select(plan => BalanceSimulator.GetOutputPath(Path.GetFileNameWithoutExtension(plan), "", measureFolder)).ToList();
        if (logs.Any(log => !File.Exists(log)))
        {
            Debug.LogError($"[ScoreMeasure] A log is missing in {measureFolder}");
            return null;
        }
        return logs.Select(log => BalanceReport.Parse(File.ReadLines(log))).ToList();
    }

    // Into every wave measured both ways and into scores.json. The number of waves written
    static int WriteWaveScores(string measureFolder, bool partial)
    {
        List<List<CombatStats>> fights = ReadLogs(measureFolder, WavePlanPaths);
        if (fights == null)
        {
            return 0;
        }

        float teamDps = WaveScoreCalculator.GetTeamDps(fights[0]);
        Dictionary<string, WaveScore> scores = WaveScoreCalculator.Compute(fights[1], fights[2], teamDps);
        UnityEngine.Object[] setup = GetMeasureSetup(Kind.Waves);
        ScoreFile.Measure measure = new ScoreFile.Measure { date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), teamDps = teamDps };

        foreach (WavePatternData wave in FindWaves())
        {
            if (!scores.TryGetValue(wave.name, out WaveScore score))
            {
                continue;
            }

            score.fingerprint = ScoreFingerprint.Compute(wave, setup);
            SerializedObject serialized = new SerializedObject(wave);
            serialized.FindProperty("score.dps").floatValue = score.dps;
            serialized.FindProperty("score.peakDps").floatValue = score.peakDps;
            serialized.FindProperty("score.survivalTime").floatValue = score.survivalTime;
            serialized.FindProperty("score.effectiveHealth").floatValue = score.effectiveHealth;
            serialized.FindProperty("score.threat").floatValue = score.threat;
            serialized.FindProperty("score.timedOut").boolValue = score.timedOut;
            serialized.FindProperty("score.fingerprint").stringValue = score.fingerprint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(wave);
            measure.waves.Add(new ScoreFile.WaveEntry { wave = wave.name, score = score });
        }

        WriteMeasure(Kind.Waves, measureFolder, measure, partial);
        return measure.waves.Count;
    }

    // Into scores.json only: the characters hold no score. The number of characters written
    static int WriteCharacterScores(string measureFolder, bool partial)
    {
        List<List<CombatStats>> fights = ReadLogs(measureFolder, CharacterPlanPaths);
        if (fights == null)
        {
            return 0;
        }

        float teamDps = WaveScoreCalculator.GetTeamDps(fights[0]);
        Dictionary<string, CharacterScore> scores = CharacterScoreCalculator.Compute(fights[1], fights[2], fights[3], fights[4], teamDps);
        ScoreFile.Measure measure = new ScoreFile.Measure { date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), teamDps = teamDps };

        foreach (HealerBotProfile bot in GetCharacterBots())
        {
            if (!scores.TryGetValue(bot.character.title, out CharacterScore score))
            {
                continue;
            }

            score.fingerprint = ComputeCharacterFingerprint(bot);
            measure.characters.Add(new ScoreFile.CharacterEntry { character = bot.character.title, score = score });
        }

        WriteMeasure(Kind.Characters, measureFolder, measure, partial);
        return measure.characters.Count;
    }

    // Into scores.json only: the items hold no score. The number of items written
    // baselineFolder: the measure the fights without item are taken from, empty when played in this one
    static int WriteItemScores(string measureFolder, bool partial, string baselineFolder)
    {
        List<List<CombatStats>> fights = ReadLogs(measureFolder, ItemPlanPaths);
        if (fights == null)
        {
            return 0;
        }

        if (!string.IsNullOrEmpty(baselineFolder))
        {
            List<List<CombatStats>> previous = ReadLogs(baselineFolder, ItemPlanPaths);
            if (previous == null)
            {
                return 0;
            }
            for (int plan = 0; plan < fights.Count; plan++)
            {
                fights[plan].AddRange(previous[plan].Where(fight => string.IsNullOrEmpty(fight.item)));
            }
        }

        Dictionary<string, ItemScore> scores = ItemScoreCalculator.Compute(fights[0], fights[1], out ItemScore baseline);
        ScoreFile.Measure measure = new ScoreFile.Measure
        {
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            teamDps = baseline.dps,
            itemBaseline = baseline,
            itemSetupFingerprint = ComputeItemSetupFingerprint(),
            itemBaselineFolder = Path.GetFileName(string.IsNullOrEmpty(baselineFolder) ? measureFolder : baselineFolder),
        };
        List<AItemFactory> items = GetRewardItems();
        Dictionary<AItemFactory, string> fingerprints = ComputeItemFingerprints(items);
        foreach (AItemFactory item in items)
        {
            if (!scores.TryGetValue(item.name, out ItemScore score))
            {
                continue;
            }

            score.fingerprint = fingerprints[item];
            measure.items.Add(new ScoreFile.ItemEntry { item = item.name, score = score });
        }

        WriteMeasure(Kind.Items, measureFolder, measure, partial);
        return measure.items.Count;
    }

    // A measure of a single wave, character or item keeps the other scores of the latest measure, the window
    // reading the latest one only
    static void WriteMeasure(Kind kind, string measureFolder, ScoreFile.Measure measure, bool partial)
    {
        if (partial)
        {
            string latest = ScoreFile.FindLatest(GetRoot(kind));
            ScoreFile.Merge(measure, latest != null ? ScoreFile.Read(latest) : null);
        }
        ScoreFile.Write(measureFolder, measure);
    }

    // The setup of the item measure alone: the plans, the balance team, the dummies and the data they use
    static string ComputeItemSetupFingerprint()
    {
        return ScoreFingerprint.ComputeForData(new UnityEngine.Object[0], GetMeasureSetup(Kind.Items));
    }

    // The file names of the logs of the plans
    static string[] GetLogNames(string[] plans)
    {
        return plans.Select(plan => Path.GetFileName(BalanceSimulator.GetOutputPath(Path.GetFileNameWithoutExtension(plan), "", "folder"))).ToArray();
    }

    // The unit then the player items of the rewards, from the game data the simulations play
    public static List<AItemFactory> GetRewardItems()
    {
        GameData data = BalanceReportWindow.LoadGameData();
        if (data == null)
        {
            return new List<AItemFactory>();
        }

        RewardPools rewards = RewardPools.Create(data, null);
        return rewards.unitItems.Concat(rewards.playerItems).Distinct().ToList();
    }

    // Each item with the measure setup
    public static Dictionary<AItemFactory, string> ComputeItemFingerprints(IEnumerable<AItemFactory> items)
    {
        Dictionary<UnityEngine.Object, string> fingerprints = ScoreFingerprint.ComputeAllForData(items.Cast<UnityEngine.Object>(), GetMeasureSetup(Kind.Items));
        return fingerprints.ToDictionary(fingerprint => (AItemFactory)fingerprint.Key, fingerprint => fingerprint.Value);
    }

    // What every score of the kind also depends on: the plans, and through them the dummies and the balance team
    public static UnityEngine.Object[] GetMeasureSetup(Kind kind)
    {
        return GetPlanPaths(kind).Select(AssetDatabase.LoadAssetAtPath<SimulationPlan>).Cast<UnityEngine.Object>().ToArray();
    }

    public static List<WavePatternData> FindWaves()
    {
        return AssetDatabase.FindAssets("t:WavePatternData", new[] { "Assets/Data" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<WavePatternData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(wave => wave != null)
            .ToList();
    }

    // The healer bots of the character measure, one per character, the bots without character skipped
    public static List<HealerBotProfile> GetCharacterBots()
    {
        SimulationPlan plan = AssetDatabase.LoadAssetAtPath<SimulationPlan>(CharacterSpellRobustnessPlanPath);
        return plan != null ? plan.healerBots.Where(bot => bot != null && bot.character != null).Distinct().ToList() : new List<HealerBotProfile>();
    }

    // The character, its bot and the measure setup, the other characters listed by the plans left out
    public static string ComputeCharacterFingerprint(HealerBotProfile bot)
    {
        UnityEngine.Object[] setup = GetMeasureSetup(Kind.Characters);
        HashSet<UnityEngine.Object> others = new HashSet<UnityEngine.Object>();
        foreach (SimulationPlan plan in setup.OfType<SimulationPlan>())
        {
            foreach (HealerBotProfile other in plan.healerBots.Where(other => other != null && other != bot))
            {
                others.Add(other);
                if (other.character != bot.character)
                {
                    others.Add(other.character);
                }
            }
        }
        return ScoreFingerprint.ComputeForData(new UnityEngine.Object[] { bot, bot.character }, setup, others);
    }
}
