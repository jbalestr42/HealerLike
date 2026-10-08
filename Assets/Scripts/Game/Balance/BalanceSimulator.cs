using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Plays the simulated fights one after the other, the scene being reloaded between two of them so each one
// starts from a clean state: the character and its reference team against a wave, sped up, written to the
// combat log of the simulation
public class BalanceSimulator : AGameType
{
    enum State
    {
        WaitForManagers,
        Fighting,
        Done,
    }

    // Unity's default for Time.maximumDeltaTime
    const float DefaultMaximumDeltaTime = 0.3333333f;
    const float ProgressBarWidth = 1000f;
    const float ProgressBarHeight = 40f;
    const int ProgressFontSize = 24;
    const int DetailsFontSize = 20;

    static readonly Color ProgressBackColor = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color ProgressFillColor = new Color(0.35f, 0.75f, 0.4f, 0.9f);

    // Built in the first OnGUI, the only place the GUI skin can be read
    GUIStyle _progressStyle;
    GUIStyle _detailsStyle;

#if UNITY_EDITOR
    // The simulation in the editor's background tasks (status bar), 0 when none: static to outlive the scene reloads
    static int _editorProgressId;
#endif

    // Folder of the next simulation, each plan written to <plan>.jsonl in it (e.g. the folder of a wave score
    // measure). Null for a balance simulation: Logs/Balance/sim-<plan>-<run>.jsonl
    public static string outputFolder;
    // Plan of the next simulation instead of the one of the scene (e.g. the full simulation restricted to some
    // waves and characters, kept in memory only): set by the editor once the domain is reloaded for the play mode
    public static SimulationPlan planOverride;

    [SerializeField] SimulationPlan _plan;

    State _state = State.WaitForManagers;
    int _waitedFrames;
    EntityManager _entities;
    CombatRecorder _recorder;
    HealerBot _bot;
    float _startTime;
    float _maxDuration;

    void Start()
    {
        _entities = EntityManager.instance;
        _entities.OnEntityKilled.AddListener(OnEntityKilled);
        _entities.OnEntitySummoned.AddListener(OnEntitySummoned);
    }

    void OnDestroy()
    {
        if (_entities != null)
        {
            _entities.OnEntityKilled.RemoveListener(OnEntityKilled);
            _entities.OnEntitySummoned.RemoveListener(OnEntitySummoned);
        }
        // The units of this scene are gone with it, their listeners must not be called by the next fight
        AscensionGameType.OnRoundStart.RemoveAllListeners();
        AscensionGameType.OnBattleStart.RemoveAllListeners();
        AscensionGameType.OnRoundEnd.RemoveAllListeners();
    }

    void Update()
    {
        switch (_state)
        {
            case State.WaitForManagers:
                if (SimulationQueue.count == 0 && planOverride != null)
                {
                    _plan = planOverride;
                }
                if (_plan == null)
                {
                    Debug.LogError("[BalanceSimulator] No simulation plan set");
                    EndSimulation();
                    break;
                }

                // Like the sandbox, wait for the first frame so every manager has been started
                if (_waitedFrames++ < 1)
                {
                    break;
                }

                if (SimulationQueue.count == 0)
                {
                    StartSimulation();
                }
                // The scene reloaded from the disk may point to another plan: the simulation keeps its own
                else if (SimulationQueue.plan != null)
                {
                    _plan = SimulationQueue.plan;
                }

                if (SimulationQueue.isRunning)
                {
                    StartFight(SimulationQueue.current);
                    _state = State.Fighting;
                }
                else
                {
                    EndSimulation();
                }
                break;

            case State.Fighting:
                UpdateFight();
                break;
        }
    }

    void StartSimulation()
    {
        List<SimulationJob> jobs = _plan.BuildJobs(DataManager.instance.data);
        string runId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string path = GetOutputPath(_plan.name, runId, outputFolder);
        SimulationQueue.Start(_plan, jobs, path, runId);
        planOverride = null;
        Debug.Log($"[BalanceSimulator] {_plan.name}: {jobs.Count} fights to simulate, written to {path}");
        StartEditorProgress();
    }

    public static string GetOutputPath(string planName, string runId, string folder)
    {
        if (string.IsNullOrEmpty(folder))
        {
            return Path.Combine(Path.GetDirectoryName(CombatLogFile.defaultPath), $"sim-{planName}-{runId}.jsonl");
        }
        return Path.Combine(folder, $"{planName}.jsonl");
    }

    void StartFight(SimulationJob job)
    {
        // The chance (critical hits, projectile angles) drawn from the seed of the fight: a fight played again
        // gives the same result
        Random.InitState(job.seed);

        PlayerBehaviour.instance.Init(job.character);
        Character character = PlayerBehaviour.instance.character;
        foreach (AItemFactory item in job.team.playerItems)
        {
            character.inventoryHandler.AddItem(item.GetItem(), -1);
        }

        PlaceTeam(job.team, job.teamPattern);
        _entities.SpawnWave(job.wave, transform.position, Entity.EntityType.Computer);
        ForEachEntity(KeepAliveIfSimulation);

        CombatStats stats = new CombatStats
        {
            runId = SimulationQueue.runId,
            seed = job.seed,
            floor = job.floor,
            roomType = job.roomType.ToString(),
            wave = job.wave.name,
            character = job.character.title,
        };

        // Without bot (a fixed team), the character casts nothing
        if (job.bot != null)
        {
            GridManager grid = PlayerBehaviour.instance.grid;
            _bot = new HealerBot(job.bot, character, _entities, grid.GetCellCenterFromCoord(new Vector2Int(_plan.frontColumn + 1, grid.height / 2)));
            stats.bot = job.bot.name;
        }

        Time.timeScale = _plan.timeScale;
        // A slow frame (scene load) must not jump the fight forward by seconds once sped up: one frame never
        // lasts more than at normal speed
        Time.maximumDeltaTime = DefaultMaximumDeltaTime / _plan.timeScale;
        AscensionGameType.OnRoundStart.Invoke();
        EnableAllEntities(true);
        AscensionGameType.OnBattleStart.Invoke();

        _recorder = new CombatRecorder(stats, () => Time.time, character.mana);
        ForEachEntity(_recorder.AddUnit);
        _startTime = Time.time;
        _maxDuration = _plan.GetMaxDuration(job.roomType);
    }

    // The tanks closest to the enemies, then the supports, then the damage dealers, each with its items. A team
    // with a pattern is placed as drawn in it
    void PlaceTeam(ReferenceTeam team, WavePatternData pattern)
    {
        GridManager grid = PlayerBehaviour.instance.grid;
        Dictionary<int, Entity> placed = new Dictionary<int, Entity>();
        Dictionary<int, Vector2Int> cells = pattern != null
            ? SimulationFormation.GetPatternCells(pattern, _plan.frontColumn, grid.height / 2)
            : SimulationFormation.GetCells(team.units, _plan.frontColumn, grid.height / 2, _plan.rowsPerColumn);
        foreach (KeyValuePair<int, Vector2Int> cell in cells)
        {
            GameObject unit = _entities.SpawnEntity(team.units[cell.Key], grid.GetCellCenterFromCoord(cell.Value), Entity.EntityType.Player);
            if (unit != null)
            {
                placed[cell.Key] = unit.GetComponent<Entity>();
            }
        }

        foreach (ReferenceTeam.UnitItem unitItem in team.unitItems)
        {
            if (placed.TryGetValue(unitItem.unit, out Entity holder))
            {
                holder.inventoryHandler.AddItem(unitItem.item.GetItem(), holder.inventoryHandler.GetFirstFreeIndex());
            }
        }
    }

    // The units made for the simulations (dummies, the team measuring the waves) never die, on either side
    public static void KeepAliveIfSimulation(Entity entity)
    {
        if (entity.HasTag(TagNames.Simulation))
        {
            entity.AddDeathPrevention(KeepAlive);
        }
    }

    // Back to full health instead of dying: the hit is still counted whole in the damage taken
    public static bool KeepAlive(Entity dying)
    {
        dying.health.SetValue(dying.health.Max);
        return true;
    }

    void UpdateFight()
    {
        _bot?.Update(Time.time);

        bool won = _entities.AreAllEntityDead(Entity.EntityType.Computer);
        bool lost = _entities.AreAllEntityDead(Entity.EntityType.Player);
        bool timedOut = Time.time - _startTime >= _maxDuration;
        if (!won && !lost && !timedOut)
        {
            return;
        }

        CombatStats stats = _recorder.Stop(won);
        stats.timedOut = !won && !lost;
        if (_bot != null)
        {
            foreach (KeyValuePair<string, int> cast in _bot.casts)
            {
                stats.casts.Add(new CombatStats.SkillCasts { skill = cast.Key, count = cast.Value });
            }
        }
        _recorder = null;
        CombatLogFile.Append(SimulationQueue.outputPath, stats);
        Debug.Log($"[BalanceSimulator] {SimulationQueue.index + 1}/{SimulationQueue.count} {stats.character}: {stats.ToSummary()}");

        SimulationQueue.Advance();
        ReportEditorProgress();
        if (SimulationQueue.isRunning)
        {
            ReloadScene();
        }
        else
        {
            EnableAllEntities(false);
            EndSimulation();
        }
    }

    // A tool scene, kept out of the build settings: the editor loads it by its path
    static void ReloadScene()
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
#endif
    }

    void EndSimulation()
    {
        Time.timeScale = 1f;
        Time.maximumDeltaTime = DefaultMaximumDeltaTime;
        if (_state != State.Done)
        {
            Debug.Log($"[BalanceSimulator] Done: {SimulationQueue.count} fights written to {SimulationQueue.outputPath} in {SimulationProgress.FormatDuration(SimulationQueue.elapsedRealtime)}");
        }
        _state = State.Done;
        FinishEditorProgress(true);
    }

    // The progress also shown in the editor's status bar, seen without the Game view
    void StartEditorProgress()
    {
#if UNITY_EDITOR
        FinishEditorProgress(false);
        ReportEditorProgress();
        // Stopping the play mode in the middle of a simulation cancels it
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
#endif
    }

    // The status bar only shows the name of a task, which can't be changed: the task is made again under a name
    // holding the progress, "Simulation 3% 120/3744"
    static void ReportEditorProgress()
    {
#if UNITY_EDITOR
        RemoveEditorProgress();
        int done = SimulationQueue.index;
        _editorProgressId = UnityEditor.Progress.Start($"Simulation {SimulationProgress.DescribeShort(done, SimulationQueue.count)}",
            SimulationProgress.Describe(done, SimulationQueue.count, SimulationQueue.elapsedRealtime));
        UnityEditor.Progress.SetTimeDisplayMode(_editorProgressId, UnityEditor.Progress.TimeDisplayMode.NoTimeShown);
        UnityEditor.Progress.Report(_editorProgressId, SimulationProgress.GetRatio(done, SimulationQueue.count));
#endif
    }

    static void FinishEditorProgress(bool succeeded)
    {
#if UNITY_EDITOR
        if (_editorProgressId != 0 && UnityEditor.Progress.Exists(_editorProgressId))
        {
            UnityEditor.Progress.Finish(_editorProgressId, succeeded ? UnityEditor.Progress.Status.Succeeded : UnityEditor.Progress.Status.Canceled);
        }
        _editorProgressId = 0;
#endif
    }

#if UNITY_EDITOR
    static void RemoveEditorProgress()
    {
        if (_editorProgressId != 0 && UnityEditor.Progress.Exists(_editorProgressId))
        {
            UnityEditor.Progress.Remove(_editorProgressId);
        }
        _editorProgressId = 0;
    }
#endif

#if UNITY_EDITOR
    static void OnQuitting()
    {
        Application.quitting -= OnQuitting;
        FinishEditorProgress(SimulationQueue.count > 0 && !SimulationQueue.isRunning);
    }
#endif

    void OnEntityKilled(Entity entity)
    {
        _recorder?.RecordDeath(entity);
    }

    void OnEntitySummoned(Entity summon)
    {
        _recorder?.AddUnit(summon);
    }

    void EnableAllEntities(bool isEnabled)
    {
        PlayerBehaviour.instance.character.Enable(isEnabled);
        ForEachEntity(entity => entity.Enable(isEnabled));
    }

    void ForEachEntity(System.Action<Entity> action)
    {
        List<GameObject> entities = new List<GameObject>(_entities.GetEntities(Entity.EntityType.Player));
        entities.AddRange(_entities.GetEntities(Entity.EntityType.Computer));
        foreach (GameObject entity in entities)
        {
            action(entity.GetComponent<Entity>());
        }
    }

    void OnGUI()
    {
        if (_plan == null)
        {
            return;
        }

        if (_progressStyle == null)
        {
            _progressStyle = new GUIStyle(GUI.skin.label) { fontSize = ProgressFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _detailsStyle = new GUIStyle(GUI.skin.label) { fontSize = DetailsFontSize };
        }

        // The bar of the fights done, with their count, the time spent and the time left, at the top middle of the
        // screen
        int done = _state == State.Done ? SimulationQueue.count : SimulationQueue.index;
        float ratio = SimulationProgress.GetRatio(done, SimulationQueue.count);
        float width = Mathf.Min(ProgressBarWidth, Screen.width - 20f);
        Rect bar = new Rect((Screen.width - width) / 2f, 10f, width, ProgressBarHeight);
        DrawRect(bar, ProgressBackColor);
        DrawRect(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), ProgressFillColor);
        GUI.Label(new Rect(bar.x + 10f, bar.y, bar.width - 10f, bar.height), SimulationProgress.Describe(done, SimulationQueue.count, SimulationQueue.elapsedRealtime), _progressStyle);

        string text = _state == State.Done
            ? $"Simulation done: {SimulationQueue.count} fights\n{SimulationQueue.outputPath}"
            : $"Fight {SimulationQueue.index + 1}/{SimulationQueue.count}"
                + (SimulationQueue.current != null ? $"\n{(SimulationQueue.current.bot != null ? SimulationQueue.current.bot.name : "No bot")} ({SimulationQueue.current.character.title}), floor {SimulationQueue.current.floor}, {SimulationQueue.current.wave.name}, seed {SimulationQueue.current.seed}" : "")
                + $"\n{Time.time - _startTime:0}s / {_maxDuration:0}s (x{_plan.timeScale:0.#})";
        DrawRect(new Rect(bar.x, bar.yMax + 4f, bar.width, DetailsFontSize * 4.5f), ProgressBackColor);
        GUI.Label(new Rect(bar.x + 10f, bar.yMax + 8f, bar.width - 10f, DetailsFontSize * 4f), text, _detailsStyle);
    }

    static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public override void StartGame()
    {
    }

    public override bool IsOver()
    {
        return _state == State.Done;
    }
}
