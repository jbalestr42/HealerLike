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

    [SerializeField] SimulationPlan _plan;

    State _state = State.WaitForManagers;
    int _waitedFrames;
    EntityManager _entities;
    CombatRecorder _recorder;
    float _startTime;

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
        string path = Path.Combine(Path.GetDirectoryName(CombatLogFile.defaultPath), $"sim-{_plan.name}-{runId}.jsonl");
        SimulationQueue.Start(jobs, path, runId);
        Debug.Log($"[BalanceSimulator] {_plan.name}: {jobs.Count} fights to simulate, written to {path}");
    }

    void StartFight(SimulationJob job)
    {
        PlayerBehaviour.instance.Init(job.character);
        Character character = PlayerBehaviour.instance.character;
        foreach (AItemFactory item in job.team.playerItems)
        {
            character.inventoryHandler.AddItem(item.GetItem(), -1);
        }

        PlaceTeam(job.team);
        _entities.SpawnWave(job.wave, transform.position, Entity.EntityType.Computer);

        CombatStats stats = new CombatStats
        {
            runId = SimulationQueue.runId,
            seed = job.seed,
            floor = job.floor,
            roomType = job.roomType.ToString(),
            wave = job.wave.name,
            character = job.character.title,
        };

        Time.timeScale = _plan.timeScale;
        AscensionGameType.OnRoundStart.Invoke();
        EnableAllEntities(true);
        AscensionGameType.OnBattleStart.Invoke();

        _recorder = new CombatRecorder(stats, () => Time.time, character.mana);
        ForEachEntity(_recorder.AddUnit);
        _startTime = Time.time;
    }

    // The tanks closest to the enemies, then the supports, then the damage dealers, each with its items
    void PlaceTeam(ReferenceTeam team)
    {
        GridManager grid = PlayerBehaviour.instance.grid;
        Dictionary<int, Entity> placed = new Dictionary<int, Entity>();
        foreach (KeyValuePair<int, Vector2Int> cell in SimulationFormation.GetCells(team.units, _plan.frontColumn, grid.height / 2, _plan.rowsPerColumn))
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

    void UpdateFight()
    {
        bool won = _entities.AreAllEntityDead(Entity.EntityType.Computer);
        bool lost = _entities.AreAllEntityDead(Entity.EntityType.Player);
        bool timedOut = Time.time - _startTime >= _plan.maxDuration;
        if (!won && !lost && !timedOut)
        {
            return;
        }

        CombatStats stats = _recorder.Stop(won);
        stats.timedOut = !won && !lost;
        _recorder = null;
        CombatLogFile.Append(SimulationQueue.outputPath, stats);
        Debug.Log($"[BalanceSimulator] {SimulationQueue.index + 1}/{SimulationQueue.count} {stats.character}: {stats.ToSummary()}");

        SimulationQueue.Advance();
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
        if (_state != State.Done)
        {
            Debug.Log($"[BalanceSimulator] Done: {SimulationQueue.count} fights written to {SimulationQueue.outputPath}");
        }
        _state = State.Done;
    }

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

        string text = _state == State.Done
            ? $"Simulation done: {SimulationQueue.count} fights\n{SimulationQueue.outputPath}"
            : $"Fight {SimulationQueue.index + 1}/{SimulationQueue.count}"
                + (SimulationQueue.current != null ? $"\n{SimulationQueue.current.character.title}, floor {SimulationQueue.current.floor}, {SimulationQueue.current.wave.name}, seed {SimulationQueue.current.seed}" : "")
                + $"\n{Time.time - _startTime:0}s / {_plan.maxDuration:0}s (x{_plan.timeScale:0.#})";
        GUI.Label(new Rect(10f, 10f, 600f, 60f), text);
    }

    public override void StartGame()
    {
    }

    public override bool IsOver()
    {
        return _state == State.Done;
    }
}
