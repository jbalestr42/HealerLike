using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Reads a simulation (Logs/Balance/sim-*.jsonl) and shows, for every wave on every floor, the mana spent against
// the target of its room type and the defeats, the floors where it fits next to the floors of its wave pools and
// its measured threat, and how each character copes per floor. The score measures of the waves and characters are
// apart (ScoreWindow)
public class BalanceReportWindow : EditorWindow
{
    enum Tab
    {
        Waves,
        Characters,
        Difficulty,
        // What the simulation run from this window plays
        Simulation,
    }

    const string ManagersPrefabPath = "Assets/Prefabs/Managers.prefab";
    const float NameWidth = 170f;
    const float ColumnWidth = 80f;
    const float CellWidth = 60f;
    const float RowHeight = 20f;

    static readonly Color FitsColor = new Color(0.35f, 0.75f, 0.4f);
    static readonly Color TooEasyColor = new Color(0.4f, 0.6f, 0.9f);
    static readonly Color TooHardColor = new Color(0.95f, 0.65f, 0.25f);
    static readonly Color LostColor = new Color(0.85f, 0.3f, 0.3f);

    // One row of a tab: its first columns, the values they are sorted by (null when missing) and its floor cells
    class Row
    {
        public object[] columns;
        public IComparable[] keys;
        public string roomType;
        public Func<int, BalanceReport.Cell> getCell;
    }

    readonly List<string> _files = new List<string>();
    int _fileIndex;
    BalanceReport _report;
    // Floors of the wave pools each wave is in, e.g. "Combat 0-2"
    Dictionary<string, string> _poolFloors = new Dictionary<string, string>();
    // The pools each wave is in, by wave name, to sort the waves by their first floor
    Dictionary<string, List<GameData.WavePool>> _wavePools = new Dictionary<string, List<GameData.WavePool>>();
    // Measured threat of each wave (ScoreWindow), by wave name: the text shown and the value it is sorted by
    readonly Dictionary<string, GUIContent> _threats = new Dictionary<string, GUIContent>();
    readonly Dictionary<string, float> _threatValues = new Dictionary<string, float>();
    readonly HashSet<string> _outOfDateThreats = new HashSet<string>();
    // Column each tab is sorted by (-1 for the order of the simulation) and its direction
    readonly int[] _sortColumns = { -1, -1, -1 };
    readonly bool[] _sortAscending = { true, true, true };
    Tab _tab;
    // 0 for the average of all the characters
    int _botIndex;
    // What the full simulation plays
    FullSimulationSettings _simulationSettings;
    Vector2 _scroll;
    GUIStyle _cellStyle;
    bool _wasSimulating;

    static string folder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "Balance");

    [MenuItem("Tools/Balance Report")]
    static void Open()
    {
        GetWindow<BalanceReportWindow>("Balance Report");
    }

    void OnEnable()
    {
        RefreshFiles();
        Load();
    }

    void OnInspectorUpdate()
    {
        // The simulation just over is shown
        if (_wasSimulating && !FullSimulation.isRunning)
        {
            RefreshFiles();
            _fileIndex = 0;
            Load();
            Repaint();
        }
        // The count of fights played goes on
        else if (FullSimulation.isRunning)
        {
            Repaint();
        }
        _wasSimulating = FullSimulation.isRunning;
    }

    // The simulations, the latest first
    void RefreshFiles()
    {
        string selected = _fileIndex < _files.Count ? _files[_fileIndex] : null;
        _files.Clear();
        if (Directory.Exists(folder))
        {
            _files.AddRange(Directory.GetFiles(folder, "sim-*.jsonl").OrderByDescending(File.GetLastWriteTime));
        }
        _fileIndex = Mathf.Max(0, _files.IndexOf(selected));
    }

    void Load()
    {
        _report = _fileIndex < _files.Count ? new BalanceReport(BalanceReport.Parse(File.ReadLines(_files[_fileIndex]))) : null;
        _botIndex = 0;
        _poolFloors = LoadPoolFloors();
        _wavePools = LoadWavePools();
        LoadThreats();
    }

    // The threat stored in each wave by the last score measure, checked against the current data of the wave
    void LoadThreats()
    {
        _threats.Clear();
        _threatValues.Clear();
        _outOfDateThreats.Clear();
        List<WavePatternData> waves = ScoreMeasure.FindWaves();
        Dictionary<WavePatternData, string> fingerprints = ScoreFingerprint.ComputeAll(waves.FindAll(wave => wave.score.measured), ScoreMeasure.GetMeasureSetup(ScoreMeasure.Kind.Waves));
        foreach (WavePatternData wave in waves)
        {
            WaveScore score = wave.score;
            string fingerprint = fingerprints.TryGetValue(wave, out string current) ? current : null;
            string tooltip = !score.measured
                ? "Never measured: Tools > Scores, Waves"
                : $"Damage dealt before dying: {score.dps:0.0} DPS x {score.survivalTime:0.0}s survived"
                    + (score.timedOut ? "\nTimed out: only a lower bound" : "")
                    + (score.IsUpToDate(fingerprint) ? "" : "\nThe wave changed since the measure: out of date");
            _threats[wave.name] = new GUIContent(score.FormatThreat(fingerprint), tooltip);
            if (score.measured)
            {
                _threatValues[wave.name] = score.threat;
                if (!score.IsUpToDate(fingerprint))
                {
                    _outOfDateThreats.Add(wave.name);
                }
            }
        }
    }

    // From the game data of the managers, the one the game plays with
    public static Dictionary<string, string> LoadPoolFloors()
    {
        Dictionary<string, string> poolFloors = new Dictionary<string, string>();
        foreach (KeyValuePair<string, List<GameData.WavePool>> wave in LoadWavePools())
        {
            poolFloors[wave.Key] = string.Join(", ", wave.Value.ConvertAll(pool => $"{pool.roomType} {FormatPoolFloors(pool)}"));
        }
        return poolFloors;
    }

    // The game data of the managers, the one the simulations play
    public static GameData LoadGameData()
    {
        GameObject managers = AssetDatabase.LoadAssetAtPath<GameObject>(ManagersPrefabPath);
        DataManager dataManager = managers != null ? managers.GetComponentInChildren<DataManager>() : null;
        return dataManager != null ? dataManager.data : null;
    }

    // The pools each wave is in, by wave name, from the game data of the managers
    public static Dictionary<string, List<GameData.WavePool>> LoadWavePools()
    {
        Dictionary<string, List<GameData.WavePool>> wavePools = new Dictionary<string, List<GameData.WavePool>>();
        GameData data = LoadGameData();
        if (data == null)
        {
            return wavePools;
        }

        foreach (GameData.WavePool pool in data.wavePools)
        {
            foreach (WavePatternData wave in pool.wavePatterns)
            {
                if (wave == null)
                {
                    continue;
                }
                if (!wavePools.TryGetValue(wave.name, out List<GameData.WavePool> pools))
                {
                    pools = new List<GameData.WavePool>();
                    wavePools[wave.name] = pools;
                }
                pools.Add(pool);
            }
        }
        return wavePools;
    }

    // "3" or "2-5"
    public static string FormatPoolFloors(GameData.WavePool pool)
    {
        return pool.minFloor == pool.maxFloor ? pool.minFloor.ToString() : $"{pool.minFloor}-{pool.maxFloor}";
    }

    void OnGUI()
    {
        // Black on the colored cells in every state: the hover one of the skin turns it white
        _cellStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.black },
            hover = { textColor = Color.black },
            active = { textColor = Color.black },
            focused = { textColor = Color.black },
        };

        DrawToolbar();
        if (_tab == Tab.Simulation)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSimulationSettings();
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_tab == Tab.Difficulty)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawDifficulty();
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_report == null)
        {
            EditorGUILayout.HelpBox($"No simulation in {folder}: play the BalanceSimulation scene first.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"{_report.fights.Count} fights. Mana spent against the target (combat {BalanceReport.manaTargets.combatMin:P0} to {BalanceReport.manaTargets.combatMax:P0}, elite and boss {BalanceReport.manaTargets.eliteMin:P0} to {BalanceReport.manaTargets.eliteMax:P0}, set in the Difficulty tab): blue too easy, green fits, orange too hard, red lost", EditorStyles.wordWrappedMiniLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_tab == Tab.Waves)
        {
            DrawWaves();
        }
        else
        {
            DrawCharacters();
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        int fileIndex = EditorGUILayout.Popup(_fileIndex, _files.Select(Path.GetFileName).ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(360f));
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            RefreshFiles();
            fileIndex = _fileIndex;
            _report = null;
        }
        // Without any simulation, nothing to load again on every repaint
        if (fileIndex != _fileIndex || (_report == null && _fileIndex < _files.Count))
        {
            _fileIndex = fileIndex;
            Load();
        }

        GUILayout.Space(10f);
        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Waves", "Characters", "Difficulty", "Simulation" }, EditorStyles.toolbarButton, GUILayout.Width(320f));
        if (_tab == Tab.Waves && _report != null)
        {
            GUILayout.Space(10f);
            string[] bots = new[] { "All characters (average)" }.Concat(_report.bots).ToArray();
            _botIndex = EditorGUILayout.Popup(_botIndex, bots, EditorStyles.toolbarPopup, GUILayout.Width(180f));
        }
        GUILayout.FlexibleSpace();
        DrawSimulationButton();
        EditorGUILayout.EndHorizontal();
    }

    void DrawSimulationButton()
    {
        if (FullSimulation.isRunning)
        {
            GUILayout.Label($"Simulating {SimulationQueue.index}/{SimulationQueue.count} fights", EditorStyles.miniLabel);
            if (GUILayout.Button(new GUIContent("Stop", "Leaves the play mode: the fights played so far stay in the log"), EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                FullSimulation.Stop();
            }
            return;
        }

        // Only to count the fights: the simulation builds it again once the play mode started
        SimulationPlan plan = BuildSelectedPlan(out int fights);
        bool hasPlan = plan != null;
        if (hasPlan)
        {
            DestroyImmediate(plan);
        }
        using (new EditorGUI.DisabledScope(!hasPlan || fights == 0 || ScoreMeasure.isRunning || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            string tooltip = $"Plays {fights} fights of {Path.GetFileNameWithoutExtension(FullSimulation.PlanPath)} in the simulation scene, as picked in the Simulation tab, then shows them here";
            if (GUILayout.Button(new GUIContent($"Run simulation ({fights} fights)", tooltip), EditorStyles.toolbarButton, GUILayout.Width(170f)))
            {
                FullSimulation.Start(true);
            }
        }
    }

    // The full simulation plan restricted to the settings and its count of fights, null without plan or game data
    // or when no wave is picked (an empty list of waves would play them all)
    SimulationPlan BuildSelectedPlan(out int fights)
    {
        fights = 0;
        _simulationSettings ??= FullSimulationSettings.Load();
        SimulationPlan basePlan = AssetDatabase.LoadAssetAtPath<SimulationPlan>(FullSimulation.PlanPath);
        GameData data = LoadGameData();
        if (basePlan == null || data == null)
        {
            return null;
        }

        SimulationPlan plan = _simulationSettings.BuildPlan(basePlan, data);
        if (plan.waves.Count == 0)
        {
            DestroyImmediate(plan);
            return null;
        }
        fights = plan.CountJobs(data);
        return plan;
    }

    void DrawSimulationSettings()
    {
        SimulationPlan basePlan = AssetDatabase.LoadAssetAtPath<SimulationPlan>(FullSimulation.PlanPath);
        GameData data = LoadGameData();
        if (basePlan == null || data == null)
        {
            EditorGUILayout.HelpBox($"No plan at {FullSimulation.PlanPath} or no game data in {ManagersPrefabPath}", MessageType.Warning);
            return;
        }

        FullSimulationSettings settings = _simulationSettings ??= FullSimulationSettings.Load();
        EditorGUILayout.LabelField("What the Run simulation button plays: the characters and waves ticked, each wave on every floor of the full simulation or only around the floors of its pools, with the seeds given. Saved in the editor preferences.", EditorStyles.wordWrappedMiniLabel);
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (HealerBotProfile bot in basePlan.healerBots.Where(bot => bot != null))
        {
            string label = bot.character != null ? bot.character.title : bot.name;
            settings.SetBotPlayed(bot, EditorGUILayout.ToggleLeft(label, settings.IsBotPlayed(bot), GUILayout.Width(120f)));
        }
        EditorGUILayout.EndHorizontal();

        List<SimulatedWave> waves = basePlan.GetWaves(data);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel, GUILayout.Width(60f));
        if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(50f)))
        {
            waves.ForEach(wave => settings.SetWavePlayed(wave.wave, true));
        }
        if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.Width(50f)))
        {
            waves.ForEach(wave => settings.SetWavePlayed(wave.wave, false));
        }
        EditorGUILayout.EndHorizontal();
        const int columns = 4;
        for (int i = 0; i < waves.Count; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = i; j < Mathf.Min(i + columns, waves.Count); j++)
            {
                SimulatedWave wave = waves[j];
                string floors = wave.hasPoolFloors ? $" ({wave.roomType}, {wave.minFloor}-{wave.maxFloor})" : "";
                settings.SetWavePlayed(wave.wave, EditorGUILayout.ToggleLeft(wave.wave.name.Replace("Wave_", "") + floors, settings.IsWavePlayed(wave.wave), GUILayout.Width(230f)));
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.LabelField("Floors and seeds", EditorStyles.boldLabel);
        settings.onlyPoolFloors = EditorGUILayout.ToggleLeft(new GUIContent("Each wave only on the floors of its pools", "Far quicker than every floor of the plan; a wave in no pool is still played on every floor"), settings.onlyPoolFloors);
        using (new EditorGUI.DisabledScope(!settings.onlyPoolFloors))
        {
            settings.poolFloorMargin = EditorGUILayout.IntSlider(new GUIContent("Floors around the pools", "Floors played before and after those of the pools"), settings.poolFloorMargin, 0, 5, GUILayout.Width(400f));
        }
        settings.seedCount = EditorGUILayout.IntSlider(new GUIContent("Seeds", "Reference teams (and chance) per character"), settings.seedCount, 1, 5, GUILayout.Width(400f));

        if (EditorGUI.EndChangeCheck())
        {
            settings.Save();
        }

        int full = basePlan.CountJobs(data);
        SimulationPlan plan = BuildSelectedPlan(out int fights);
        bool hasPlan = plan != null;
        if (hasPlan)
        {
            DestroyImmediate(plan);
        }
        EditorGUILayout.LabelField(!hasPlan ? "No wave picked" : $"{fights} fights picked, against {full} for every wave on every floor", EditorStyles.miniLabel);
        EditorGUILayout.Space();
    }

    void DrawWaves()
    {
        string bot = _botIndex > 0 ? _report.bots[_botIndex - 1] : null;
        List<Row> rows = new List<Row>();
        foreach (string wave in _report.waves)
        {
            string roomType = _report.GetRoomType(wave);
            List<int> fitsOn = _report.GetRecommendedFloors(wave);
            List<GameData.WavePool> pools = _wavePools.TryGetValue(wave, out List<GameData.WavePool> found) ? found : null;
            rows.Add(new Row
            {
                columns = new object[]
                {
                    wave,
                    roomType,
                    _poolFloors.TryGetValue(wave, out string poolFloors) ? poolFloors : "-",
                    BalanceReport.FormatFloors(fitsOn),
                    _threats.TryGetValue(wave, out GUIContent threat) ? threat : new GUIContent("-"),
                },
                keys = new IComparable[]
                {
                    wave,
                    roomType,
                    pools != null ? (IComparable)pools.Min(pool => pool.minFloor) : null,
                    fitsOn.Count > 0 ? (IComparable)fitsOn.Min() : null,
                    _threatValues.TryGetValue(wave, out float threatValue) ? (IComparable)threatValue : null,
                },
                roomType = roomType,
                getCell = floor => _report.GetCell(wave, floor, bot),
            });
        }
        DrawTable(rows, "Wave", "Room", "Pools", "Fits on", new GUIContent("Threat", "Damage the wave deals before dying (Tools > Scores): ≥ only a lower bound (timed out), * out of date"));
    }

    // The waves of the pools that were measured, once per pool
    void DrawDifficulty()
    {
        List<DifficultyCurveView.Wave> waves = new List<DifficultyCurveView.Wave>();
        foreach (KeyValuePair<string, List<GameData.WavePool>> wave in _wavePools)
        {
            if (!_threatValues.TryGetValue(wave.Key, out float threat))
            {
                continue;
            }
            foreach (GameData.WavePool pool in wave.Value)
            {
                waves.Add(new DifficultyCurveView.Wave
                {
                    name = wave.Key,
                    roomType = pool.roomType,
                    threat = threat,
                    upToDate = !_outOfDateThreats.Contains(wave.Key),
                    minFloor = pool.minFloor,
                    maxFloor = pool.maxFloor,
                });
            }
        }
        DifficultyCurveView.Draw(waves, Mathf.Max(600f, position.width - 30f));
    }

    void DrawCharacters()
    {
        List<Row> rows = new List<Row>();
        foreach (string bot in _report.bots)
        {
            foreach (string roomType in _report.fights.Select(fight => fight.roomType).Distinct())
            {
                BalanceReport.Cell all = _report.GetBotCell(bot, roomType);
                if (all.fights == 0)
                {
                    continue;
                }

                rows.Add(new Row
                {
                    columns = new object[] { bot, roomType, $"{all.wins}/{all.fights}", $"{all.manaSpent:P0} / {all.healthLost:P0}" },
                    keys = new IComparable[] { bot, roomType, (float)all.wins / all.fights, all.manaSpent },
                    roomType = roomType,
                    getCell = floor => _report.GetBotCell(bot, roomType, floor),
                });
            }
        }
        DrawTable(rows, "Bot", "Room", "Won", new GUIContent("Mana / HP lost", "Sorted by the mana spent"));
    }

    // The header, whose first columns sort the rows when clicked (again for the other direction), then the rows
    void DrawTable(List<Row> rows, params object[] headers)
    {
        int tab = (int)_tab;
        DrawHeader(headers, tab);
        if (_sortColumns[tab] >= 0)
        {
            int column = _sortColumns[tab];
            rows = TableSort.Sort(rows, row => row.keys[column], _sortAscending[tab]);
        }

        foreach (Row row in rows)
        {
            Rect cells = BeginRow(EditorStyles.label, row.columns);
            foreach (int floor in _report.floors)
            {
                DrawCell(NextCell(ref cells), row.getCell(floor), row.roomType);
            }
            EndRow();
        }
    }

    // On the waves, each floor shows under it the threat the difficulty curve targets there (combat / elite and boss)
    void DrawHeader(object[] headers, int tab)
    {
        bool showTargets = _tab == Tab.Waves;
        float height = showTargets ? 2f * RowHeight - 6f : RowHeight;
        float width = NameWidth + (headers.Length - 1) * ColumnWidth + _report.floors.Count * CellWidth;
        Rect row = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width));
        float x = row.x;
        for (int i = 0; i < headers.Length; i++)
        {
            float columnWidth = i == 0 ? NameWidth : ColumnWidth;
            GUIContent header = new GUIContent(ToContent(headers[i]));
            if (_sortColumns[tab] == i)
            {
                header.text += _sortAscending[tab] ? " ▲" : " ▼";
            }
            if (GUI.Button(new Rect(x, row.y, columnWidth, RowHeight), header, EditorStyles.boldLabel))
            {
                _sortAscending[tab] = _sortColumns[tab] != i || !_sortAscending[tab];
                _sortColumns[tab] = i;
            }
            x += columnWidth;
        }

        DifficultyCurve curve = DifficultyCurveView.LoadCurve();
        Rect cell = new Rect(x, row.y, CellWidth, RowHeight);
        foreach (int floor in _report.floors)
        {
            Rect rect = NextCell(ref cell);
            GUI.Label(rect, $"Floor {floor}", EditorStyles.centeredGreyMiniLabel);
            if (showTargets)
            {
                float combat = curve.GetThreat(floor);
                float elite = combat * DifficultyCurveView.eliteFactor;
                string tooltip = $"Target threat on floor {floor} (Difficulty tab): combat {combat:0}, elite and boss {elite:0}";
                GUI.Label(new Rect(rect.x, rect.y + RowHeight - 6f, rect.width, RowHeight), new GUIContent($"{combat:0}/{elite:0}", tooltip), EditorStyles.centeredGreyMiniLabel);
            }
        }
        EndRow();
    }

    static GUIContent ToContent(object column)
    {
        return column as GUIContent ?? new GUIContent(column?.ToString());
    }

    // The first columns of a row (the name, then texts or GUIContents with a tooltip), the rect left for its floor cells
    Rect BeginRow(GUIStyle style, object[] columns)
    {
        float width = NameWidth + (columns.Length - 1) * ColumnWidth + _report.floors.Count * CellWidth;
        Rect row = GUILayoutUtility.GetRect(width, RowHeight, GUILayout.Width(width));
        float x = row.x;
        for (int i = 0; i < columns.Length; i++)
        {
            float columnWidth = i == 0 ? NameWidth : ColumnWidth;
            GUI.Label(new Rect(x, row.y, columnWidth, RowHeight), ToContent(columns[i]), style);
            x += columnWidth;
        }
        return new Rect(x, row.y, CellWidth, RowHeight);
    }

    static void EndRow()
    {
        GUILayout.Space(1f);
    }

    static Rect NextCell(ref Rect cell)
    {
        Rect current = new Rect(cell.x + 1f, cell.y, cell.width - 2f, cell.height);
        cell.x += cell.width;
        return current;
    }

    void DrawCell(Rect rect, BalanceReport.Cell cell, string roomType)
    {
        if (cell.fights == 0)
        {
            return;
        }

        EditorGUI.DrawRect(rect, GetColor(cell, roomType));
        string text = cell.allWon ? $"{cell.manaSpent:P0}" : $"{cell.wins}/{cell.fights}";
        string tooltip = $"Won {cell.wins}/{cell.fights}{(cell.timeouts > 0 ? $" ({cell.timeouts} timed out)" : "")}\n"
            + $"Mana spent {cell.manaSpent:P0} (target {BalanceReport.GetManaTargetRange(roomType).min:P0} to {BalanceReport.GetManaTargetRange(roomType).max:P0})\n"
            + $"Health lost {cell.healthLost:P0}, deaths {cell.deaths:0.#}\n"
            + $"Duration {cell.duration:0}s";
        GUI.Label(rect, new GUIContent(text, tooltip), _cellStyle);
    }

    static Color GetColor(BalanceReport.Cell cell, string roomType)
    {
        if (!cell.allWon)
        {
            return LostColor;
        }

        float gap = BalanceReport.GetManaGap(cell, roomType);
        if (gap == 0f)
        {
            return FitsColor;
        }
        return gap < 0f ? TooEasyColor : TooHardColor;
    }
}
