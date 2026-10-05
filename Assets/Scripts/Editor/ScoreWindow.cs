using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// The scores of the waves of the pools and of the characters in their initial state, whether they are up to date,
// and the buttons measuring them (ScoreMeasure). Apart from the Balance Report, which reads the balance
// simulations of the characters along a run
public class ScoreWindow : EditorWindow
{
    // A wave or a character: its name links to its asset, the status is the last column
    class Row
    {
        public UnityEngine.Object asset;
        // Index in StatusNames
        public int status;
        public string[] cells;
        // What each column is sorted by
        public IComparable[] keys;
    }

    // A sortable table of rows, its header kept between two repaints
    class Table
    {
        public MultiColumnHeader header;
        public List<Row> rows;
        // Date of the latest finished measure, empty without any
        public string lastMeasure = "";

        public Table(MultiColumnHeaderState.Column[] columns, int sortedColumn)
        {
            header = new MultiColumnHeader(new MultiColumnHeaderState(columns)) { height = RowHeight + 4f };
            header.SetSorting(sortedColumn, false);
            header.sortingChanged += _ => Sort();
        }

        public void Sort()
        {
            if (rows == null)
            {
                return;
            }

            int column = header.sortedColumnIndex;
            bool ascending = header.IsSortedAscending(column);
            rows.Sort((a, b) => ascending ? a.keys[column].CompareTo(b.keys[column]) : b.keys[column].CompareTo(a.keys[column]));
        }
    }

    const float RowHeight = 20f;

    static readonly string[] StatusNames = { "Up to date", "Lower bound", "Out of date", "Not measured" };
    static readonly Color[] StatusColors = { new Color(0.35f, 0.75f, 0.4f), new Color(0.4f, 0.6f, 0.9f), new Color(0.95f, 0.65f, 0.25f), new Color(0.85f, 0.3f, 0.3f) };
    const string StatusTooltip = "Up to date, lower bound (the fight reached its time limit), out of date (its data or the measures changed since), or never measured";

    ScoreMeasure.Kind _tab;
    // Computed on demand: the fingerprints read every data file
    Table _waves;
    Table _characters;
    bool _wasMeasuring;
    Vector2 _scroll;
    GUIStyle _statusStyle;
    GUIStyle _numberStyle;

    Table current => _tab == ScoreMeasure.Kind.Waves ? _waves : _characters;

    [MenuItem("Tools/Scores")]
    static void Open()
    {
        GetWindow<ScoreWindow>("Scores");
    }

    void OnGUI()
    {
        // Black on the colored cell in every state: the hover one of the skin turns it white
        _statusStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.black },
            hover = { textColor = Color.black },
            active = { textColor = Color.black },
            focused = { textColor = Color.black },
        };
        _numberStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
        _waves ??= new Table(CreateWaveColumns(), 7);
        _characters ??= new Table(CreateCharacterColumns(), 8);

        // The scores just written are shown
        if (_wasMeasuring && !ScoreMeasure.isRunning)
        {
            _waves.rows = null;
            _characters.rows = null;
        }
        _wasMeasuring = ScoreMeasure.isRunning;

        // Before the rows: the tab may change in it
        DrawToolbar();
        if (current.rows == null)
        {
            current.rows = _tab == ScoreMeasure.Kind.Waves ? BuildWaveRows(_waves) : BuildCharacterRows(_characters);
            current.Sort();
        }
        EditorGUILayout.LabelField(_tab == ScoreMeasure.Kind.Waves
            ? "Damage of each wave against 5 dummies, and how long it survives the balance team: the threat is the damage it deals before dying. Click a header to sort, hover it for the details."
            : "The starting units, items and skills of each character: their damage against 5 dummies, and how long they survive the balance team, without then with the skills of the character played by its healer bot. Click a header to sort, hover it for the details.", EditorStyles.wordWrappedMiniLabel);
        DrawTable(current);
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _tab = (ScoreMeasure.Kind)GUILayout.Toolbar((int)_tab, new[] { "Waves", "Characters" }, EditorStyles.toolbarButton, GUILayout.Width(160f));
        GUILayout.Space(10f);

        if (ScoreMeasure.isRunning)
        {
            GUILayout.Label(ScoreMeasure.status, EditorStyles.miniLabel);
            if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                ScoreMeasure.Cancel();
            }
        }
        else
        {
            string root = ScoreMeasure.GetRoot(_tab);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                string tooltip = _tab == ScoreMeasure.Kind.Waves
                    ? $"Plays the three measures in the simulation scene (about 10 minutes at x20), then writes the scores into the waves. Logs and scores in {root}"
                    : $"Plays the four measures in the simulation scene (a few minutes at x20). Logs and scores in {root}";
                if (GUILayout.Button(new GUIContent(_tab == ScoreMeasure.Kind.Waves ? "Measure every wave" : "Measure every character", tooltip), EditorStyles.toolbarButton, GUILayout.Width(150f)))
                {
                    ScoreMeasure.Start(_tab);
                }
            }
            if (GUILayout.Button(new GUIContent("Check", "Computes the fingerprints again, after a change of the data"), EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                current.rows = null;
            }
            if (GUILayout.Button(new GUIContent("Open folder", root), EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                Directory.CreateDirectory(root);
                EditorUtility.RevealInFinder(root);
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label(string.IsNullOrEmpty(current.lastMeasure) ? "Never measured" : $"Last measure: {current.lastMeasure}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    static ScoreFile.Measure ReadLatest(ScoreMeasure.Kind kind, Table table)
    {
        string latest = ScoreFile.FindLatest(ScoreMeasure.GetRoot(kind));
        ScoreFile.Measure measure = latest != null ? ScoreFile.Read(latest) : null;
        table.lastMeasure = measure != null ? measure.date : "";
        return measure;
    }

    // The waves of the pools, with the score stored in each of them
    static List<Row> BuildWaveRows(Table table)
    {
        ReadLatest(ScoreMeasure.Kind.Waves, table);
        Dictionary<string, List<GameData.WavePool>> wavePools = BalanceReportWindow.LoadWavePools();
        List<WavePatternData> waves = ScoreMeasure.FindWaves().FindAll(wave => wavePools.ContainsKey(wave.name));
        Dictionary<WavePatternData, string> fingerprints = ScoreFingerprint.ComputeAll(waves.FindAll(wave => wave.score.measured), ScoreMeasure.GetMeasureSetup(ScoreMeasure.Kind.Waves));
        List<Row> rows = new List<Row>();
        foreach (WavePatternData wave in waves)
        {
            WaveScore score = wave.score;
            bool measured = score.measured;
            int status = !measured ? 3 : !score.IsUpToDate(fingerprints[wave]) ? 2 : score.timedOut ? 1 : 0;
            List<GameData.WavePool> pools = wavePools[wave.name];
            string types = string.Join(", ", pools.Select(pool => pool.roomType.ToString()).Distinct());
            string floors = string.Join(", ", pools.Select(BalanceReportWindow.FormatPoolFloors));
            rows.Add(new Row
            {
                asset = wave,
                status = status,
                cells = new[]
                {
                    wave.name,
                    types,
                    floors,
                    measured ? $"{score.survivalTime:0.0}s" : "-",
                    measured ? $"{score.dps:0.0}" : "-",
                    measured ? $"{score.peakDps:0.0}" : "-",
                    measured ? $"{score.effectiveHealth:0}" : "-",
                    measured ? $"{score.threat:0}" : "-",
                    StatusNames[status],
                },
                keys = new IComparable[] { wave.name, types, floors, score.survivalTime, score.dps, score.peakDps, score.effectiveHealth, score.threat, status },
            });
        }
        return rows;
    }

    // The characters of the healer bots of the measure, with their score in the latest measure
    static List<Row> BuildCharacterRows(Table table)
    {
        ScoreFile.Measure measure = ReadLatest(ScoreMeasure.Kind.Characters, table);
        List<Row> rows = new List<Row>();
        foreach (HealerBotProfile bot in ScoreMeasure.GetCharacterBots())
        {
            CharacterScore score = measure?.characters.Find(entry => entry.character == bot.character.title)?.score;
            bool measured = score != null && !string.IsNullOrEmpty(score.fingerprint);
            int status = !measured ? 3 : score.fingerprint != ScoreMeasure.ComputeCharacterFingerprint(bot) ? 2 : score.timedOut ? 1 : 0;
            score ??= new CharacterScore();
            rows.Add(new Row
            {
                asset = bot.character,
                status = status,
                cells = new[]
                {
                    bot.character.title,
                    measured ? $"{score.survivalTime:0.0}s" : "-",
                    measured ? $"{score.healingPerSecond:0.0}" : "-",
                    measured ? $"{score.manaPerSecond:0.0}" : "-",
                    measured ? $"{score.dps:0.0}" : "-",
                    measured ? $"{score.effectiveHealth:0}" : "-",
                    measured ? $"{score.healedEffectiveHealth:0}" : "-",
                    measured ? $"{score.threat:0}" : "-",
                    measured ? $"{score.healedThreat:0}" : "-",
                    StatusNames[status],
                },
                keys = new IComparable[] { bot.character.title, score.survivalTime, score.healingPerSecond, score.manaPerSecond, score.dps, score.effectiveHealth, score.healedEffectiveHealth, score.threat, score.healedThreat, status },
            });
        }
        return rows;
    }

    static MultiColumnHeaderState.Column[] CreateWaveColumns()
    {
        return new[]
        {
            CreateColumn(new GUIContent("Wave", "Click to select the wave asset"), 170f),
            CreateColumn(new GUIContent("Type", "Room type of the wave pools it is in (combat, elite, boss)"), 80f),
            CreateColumn(new GUIContent("Pools", "Floors of the wave pools it is in"), 90f),
            CreateColumn(Header<WaveScore>("Survival", "survivalTime"), 70f),
            CreateColumn(Header<WaveScore>("DPS", "dps"), 70f),
            CreateColumn(Header<WaveScore>("Peak DPS", "peakDps"), 75f),
            CreateColumn(Header<WaveScore>("Eff. health", "effectiveHealth"), 80f),
            CreateColumn(Header<WaveScore>("Threat", "threat"), 70f),
            CreateColumn(new GUIContent("Status", StatusTooltip), 100f),
        };
    }

    static MultiColumnHeaderState.Column[] CreateCharacterColumns()
    {
        return new[]
        {
            CreateColumn(new GUIContent("Character", "Click to select the character asset"), 120f),
            CreateColumn(Header<CharacterScore>("Survival", "survivalTime"), 70f),
            CreateColumn(Header<CharacterScore>("Heal/s", "healingPerSecond"), 65f),
            CreateColumn(Header<CharacterScore>("Mana/s", "manaPerSecond"), 65f),
            CreateColumn(Header<CharacterScore>("DPS", "dps"), 70f),
            CreateColumn(Header<CharacterScore>("Eff. health", "effectiveHealth"), 80f),
            CreateColumn(Header<CharacterScore>("Healed eff. HP", "healedEffectiveHealth"), 95f),
            CreateColumn(Header<CharacterScore>("Threat", "threat"), 70f),
            CreateColumn(Header<CharacterScore>("Healed threat", "healedThreat"), 90f),
            CreateColumn(new GUIContent("Status", StatusTooltip), 100f),
        };
    }

    static MultiColumnHeaderState.Column CreateColumn(GUIContent content, float width)
    {
        return new MultiColumnHeaderState.Column
        {
            headerContent = content,
            width = width,
            minWidth = 50f,
            canSort = true,
            autoResize = false,
            allowToggleVisibility = false,
        };
    }

    // The tooltip of the score field
    static GUIContent Header<T>(string text, string field)
    {
        TooltipAttribute tooltip = (TooltipAttribute)System.Attribute.GetCustomAttribute(typeof(T).GetField(field), typeof(TooltipAttribute));
        return new GUIContent(text, tooltip != null ? tooltip.tooltip : "");
    }

    void DrawTable(Table table)
    {
        Rect headerRect = GUILayoutUtility.GetRect(0f, table.header.height, GUILayout.ExpandWidth(true));
        table.header.OnGUI(headerRect, _scroll.x);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        float width = table.header.state.widthOfAllVisibleColumns;
        int[] visibleColumns = table.header.state.visibleColumns;
        int statusColumn = table.header.state.columns.Length - 1;
        for (int i = 0; i < table.rows.Count; i++)
        {
            Rect rowRect = GUILayoutUtility.GetRect(width, RowHeight, GUILayout.Width(width));
            if (i % 2 == 1)
            {
                EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.08f));
            }

            Row row = table.rows[i];
            for (int visible = 0; visible < visibleColumns.Length; visible++)
            {
                int column = visibleColumns[visible];
                Rect cell = table.header.GetCellRect(visible, rowRect);
                Rect inner = new Rect(cell.x + 4f, cell.y, cell.width - 8f, cell.height);
                if (column == statusColumn)
                {
                    EditorGUI.DrawRect(new Rect(cell.x + 1f, cell.y + 1f, cell.width - 2f, cell.height - 2f), StatusColors[row.status]);
                    GUI.Label(cell, row.cells[column], _statusStyle);
                }
                else if (column == 0)
                {
                    // A link to the asset: selects it in the Inspector and pings it in the Project window
                    if (EditorGUI.LinkButton(inner, row.cells[column]))
                    {
                        Selection.activeObject = row.asset;
                        EditorGUIUtility.PingObject(row.asset);
                    }
                }
                else
                {
                    // Text columns (the pools) are left aligned, the numbers right aligned
                    GUI.Label(inner, row.cells[column], row.keys[column] is string ? EditorStyles.label : _numberStyle);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
