using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Reads a simulation (Logs/Balance/sim-*.jsonl) and shows, for every wave on every floor, the mana spent against
// the target of its room type and the defeats, the floors where it fits next to the floors of its wave pools,
// and how each character copes per floor. The score measures of the waves and characters are apart (ScoreWindow)
public class BalanceReportWindow : EditorWindow
{
    enum Tab
    {
        Waves,
        Characters,
    }

    const string ManagersPrefabPath = "Assets/Prefabs/Managers.prefab";
    const float NameWidth = 170f;
    const float ColumnWidth = 80f;
    const float CellWidth = 52f;
    const float RowHeight = 20f;

    static readonly Color FitsColor = new Color(0.35f, 0.75f, 0.4f);
    static readonly Color TooEasyColor = new Color(0.4f, 0.6f, 0.9f);
    static readonly Color TooHardColor = new Color(0.95f, 0.65f, 0.25f);
    static readonly Color LostColor = new Color(0.85f, 0.3f, 0.3f);

    readonly List<string> _files = new List<string>();
    int _fileIndex;
    BalanceReport _report;
    // Floors of the wave pools each wave is in, e.g. "Combat 0-2"
    Dictionary<string, string> _poolFloors = new Dictionary<string, string>();
    Tab _tab;
    // 0 for the average of all the characters
    int _botIndex;
    Vector2 _scroll;
    GUIStyle _cellStyle;

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

    // The pools each wave is in, by wave name, from the game data of the managers
    public static Dictionary<string, List<GameData.WavePool>> LoadWavePools()
    {
        Dictionary<string, List<GameData.WavePool>> wavePools = new Dictionary<string, List<GameData.WavePool>>();
        GameObject managers = AssetDatabase.LoadAssetAtPath<GameObject>(ManagersPrefabPath);
        DataManager dataManager = managers != null ? managers.GetComponentInChildren<DataManager>() : null;
        if (dataManager == null || dataManager.data == null)
        {
            return wavePools;
        }

        foreach (GameData.WavePool pool in dataManager.data.wavePools)
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
        if (_report == null)
        {
            EditorGUILayout.HelpBox($"No simulation in {folder}: play the BalanceSimulation scene first.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"{_report.fights.Count} fights. Mana spent against the target (combat {BalanceReport.CombatManaTarget:P0}, elite and boss {BalanceReport.EliteManaTarget:P0}, ±{BalanceReport.ManaTolerance:P0}): blue too easy, green fits, orange too hard, red lost", EditorStyles.wordWrappedMiniLabel);
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
        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Waves", "Characters" }, EditorStyles.toolbarButton, GUILayout.Width(160f));
        if (_tab == Tab.Waves && _report != null)
        {
            GUILayout.Space(10f);
            string[] bots = new[] { "All characters (average)" }.Concat(_report.bots).ToArray();
            _botIndex = EditorGUILayout.Popup(_botIndex, bots, EditorStyles.toolbarPopup, GUILayout.Width(180f));
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void DrawWaves()
    {
        string bot = _botIndex > 0 ? _report.bots[_botIndex - 1] : null;
        DrawHeader("Wave", "Room", "Pools", "Fits on");
        foreach (string wave in _report.waves)
        {
            string roomType = _report.GetRoomType(wave);
            Rect row = BeginRow(wave, roomType, _poolFloors.TryGetValue(wave, out string pools) ? pools : "-", BalanceReport.FormatFloors(_report.GetRecommendedFloors(wave)));
            foreach (int floor in _report.floors)
            {
                DrawCell(NextCell(ref row), _report.GetCell(wave, floor, bot), roomType);
            }
            EndRow();
        }
    }

    void DrawCharacters()
    {
        DrawHeader("Bot", "Room", "Won", "Mana / HP lost");
        foreach (string bot in _report.bots)
        {
            foreach (string roomType in _report.fights.Select(fight => fight.roomType).Distinct())
            {
                BalanceReport.Cell all = _report.GetBotCell(bot, roomType);
                if (all.fights == 0)
                {
                    continue;
                }

                Rect row = BeginRow(bot, roomType, $"{all.wins}/{all.fights}", $"{all.manaSpent:P0} / {all.healthLost:P0}");
                foreach (int floor in _report.floors)
                {
                    DrawCell(NextCell(ref row), _report.GetBotCell(bot, roomType, floor), roomType);
                }
                EndRow();
            }
        }
    }

    void DrawHeader(string name, string room, string third, string fourth)
    {
        Rect row = BeginRow(name, room, third, fourth, EditorStyles.boldLabel);
        foreach (int floor in _report.floors)
        {
            GUI.Label(NextCell(ref row), $"Floor {floor}", EditorStyles.centeredGreyMiniLabel);
        }
        EndRow();
    }

    // The first columns of a row, the rect left for its floor cells
    Rect BeginRow(string name, string room, string third, string fourth, GUIStyle style = null)
    {
        style ??= EditorStyles.label;
        float width = NameWidth + 3f * ColumnWidth + _report.floors.Count * CellWidth;
        Rect row = GUILayoutUtility.GetRect(width, RowHeight, GUILayout.Width(width));
        GUI.Label(new Rect(row.x, row.y, NameWidth, RowHeight), name, style);
        GUI.Label(new Rect(row.x + NameWidth, row.y, ColumnWidth, RowHeight), room, style);
        GUI.Label(new Rect(row.x + NameWidth + ColumnWidth, row.y, ColumnWidth, RowHeight), third, style);
        GUI.Label(new Rect(row.x + NameWidth + 2f * ColumnWidth, row.y, ColumnWidth, RowHeight), fourth, style);
        return new Rect(row.x + NameWidth + 3f * ColumnWidth, row.y, CellWidth, RowHeight);
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
            + $"Mana spent {cell.manaSpent:P0} (target {BalanceReport.GetManaTarget(roomType):P0})\n"
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
        if (Mathf.Abs(gap) <= BalanceReport.ManaTolerance)
        {
            return FitsColor;
        }
        return gap < 0f ? TooEasyColor : TooHardColor;
    }
}
