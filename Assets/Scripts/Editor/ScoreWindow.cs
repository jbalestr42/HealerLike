using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// The scores of the waves of the pools, of the characters in their initial state and of the reward items, whether
// they are up to date,
// and the buttons measuring them (ScoreMeasure). Apart from the Balance Report, which reads the balance
// simulations of the characters along a run
public class ScoreWindow : EditorWindow
{
    // A wave, a character or an item: its name links to its asset, the status is the last column
    class Row
    {
        public UnityEngine.Object asset;
        // Index in StatusNames
        public int status;
        public string[] cells;
        // What each column is sorted by
        public IComparable[] keys;
    }

    // A title over several columns following each other, drawn above their headers
    class ColumnGroup
    {
        public string title;
        public int firstColumn;
        public int columnCount;

        public ColumnGroup(string title, int firstColumn, int columnCount)
        {
            this.title = title;
            this.firstColumn = firstColumn;
            this.columnCount = columnCount;
        }
    }

    // A sortable table of rows, its header kept between two repaints
    class Table
    {
        public MultiColumnHeader header;
        // Empty without any group: no row above the headers
        public ColumnGroup[] groups;
        public List<Row> rows;
        // Date of the latest finished measure, empty without any
        public string lastMeasure = "";
        // The measures without item of the latest item measure, null without any
        public ItemScore baseline;

        public Table(MultiColumnHeaderState.Column[] columns, int sortedColumn, params ColumnGroup[] groups)
        {
            this.groups = groups;
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
    const float MeasureButtonWidth = 22f;
    const string NoEffectShareKey = "HealerLike.ScoreWindow.NoEffectShare";

    static readonly string[] StatusNames = { "Up to date", "Lower bound", "Out of date", "Not measured" };
    static readonly Color[] StatusColors = { new Color(0.35f, 0.75f, 0.4f), new Color(0.4f, 0.6f, 0.9f), new Color(0.95f, 0.65f, 0.25f), new Color(0.85f, 0.3f, 0.3f) };
    const string StatusTooltip = "Up to date, lower bound (the fight reached its time limit), out of date (its data or the measures changed since), or never measured";

    ScoreMeasure.Kind _tab;
    // Computed on demand: the fingerprints read every data file
    Table _waves;
    Table _characters;
    Table _items;
    bool _wasMeasuring;
    Vector2 _scroll;
    GUIStyle _statusStyle;
    GUIStyle _numberStyle;

    Table current => _tab == ScoreMeasure.Kind.Waves ? _waves : _tab == ScoreMeasure.Kind.Characters ? _characters : _items;

    [MenuItem("Tools/Scores")]
    static void Open()
    {
        GetWindow<ScoreWindow>("Scores");
    }

    void OnEnable()
    {
        ItemScore.noEffectShare = EditorPrefs.GetFloat(NoEffectShareKey, ItemScore.DefaultNoEffectShare);
    }

    void OnGUI()
    {
        // Made again at each draw: a cached copy of a skin style can be reset by Unity (a reload of the skin), its text
        // then black and in the top left corner. Black on the colored cell in every state: the hover one of the skin turns it white
        _statusStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.black },
            hover = { textColor = Color.black },
            active = { textColor = Color.black },
            focused = { textColor = Color.black },
        };
        _numberStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
        _waves ??= new Table(CreateWaveColumns(), 7);
        _characters ??= new Table(CreateCharacterColumns(), 10, new ColumnGroup("Without skills", 1, 4), new ColumnGroup("With skills", 5, 6));
        _items ??= new Table(CreateItemColumns(), 12, new ColumnGroup("Damage: held by a unit of the balance team", 2, 4), new ColumnGroup("Robustness: held by a dummy", 6, 4), new ColumnGroup("Power: both", 10, 3));

        // The scores just written are shown
        if (_wasMeasuring && !ScoreMeasure.isRunning)
        {
            _waves.rows = null;
            _characters.rows = null;
            _items.rows = null;
        }
        _wasMeasuring = ScoreMeasure.isRunning;

        // Before the rows: the tab may change in it
        DrawToolbar();
        if (current.rows == null)
        {
            current.rows = _tab == ScoreMeasure.Kind.Waves ? BuildWaveRows(_waves) : _tab == ScoreMeasure.Kind.Characters ? BuildCharacterRows(_characters) : BuildItemRows(_items);
            current.Sort();
        }
        EditorGUILayout.LabelField(GetDescription(), EditorStyles.wordWrappedMiniLabel);
        DrawTable(current);
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _tab = (ScoreMeasure.Kind)GUILayout.Toolbar((int)_tab, new[] { "Waves", "Characters", "Items" }, EditorStyles.toolbarButton, GUILayout.Width(240f));
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
                    : _tab == ScoreMeasure.Kind.Characters
                        ? $"Plays the five measures in the simulation scene (a few minutes at x20). Logs and scores in {root}"
                        : $"Plays the two measures in the simulation scene, each item held by each unit in turn (about 1000 fights at x20, half an hour). Logs and scores in {root}";
                string label = _tab == ScoreMeasure.Kind.Waves ? "Measure every wave" : _tab == ScoreMeasure.Kind.Characters ? "Measure every character" : "Measure every item";
                if (GUILayout.Button(new GUIContent(label, tooltip), EditorStyles.toolbarButton, GUILayout.Width(150f)))
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
        if (_tab == ScoreMeasure.Kind.Items)
        {
            GUILayout.Label(new GUIContent("No effect under", "A gain below this share of the value without item is shown as no effect: two fights with the same seed already differ by 2 to 4%"), EditorStyles.miniLabel);
            float percent = EditorGUILayout.DelayedFloatField(ItemScore.noEffectShare * 100f, EditorStyles.toolbarTextField, GUILayout.Width(40f));
            GUILayout.Label("%", EditorStyles.miniLabel);
            float share = Mathf.Max(0f, percent) / 100f;
            if (!Mathf.Approximately(share, ItemScore.noEffectShare))
            {
                ItemScore.noEffectShare = share;
                EditorPrefs.SetFloat(NoEffectShareKey, share);
                _items.rows = null;
            }
            GUILayout.Space(10f);
        }
        GUILayout.Label(string.IsNullOrEmpty(current.lastMeasure) ? "Never measured" : $"Last measure: {current.lastMeasure}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    string GetDescription()
    {
        switch (_tab)
        {
            case ScoreMeasure.Kind.Waves:
                return "Damage of each wave against 5 dummies, and how long it survives the balance team: the threat is the damage it deals before dying. Click a header to sort, hover it for the details.";
            case ScoreMeasure.Kind.Characters:
                return "The starting units, items and skills of each character: their damage against 5 dummies, and how long they survive the balance team, without then with the skills of the character played by its healer bot. Click a header to sort, hover it for the details.";
            default:
                ItemScore baseline = _items.baseline;
                string reference = baseline != null ? $" Without item: {baseline.dps:0.0} dps, {baseline.effectiveHealth:0} effective health ({baseline.survivalTime:0.0}s), {baseline.power:0} power." : "";
                return "Each reward item on its own, held by each unit in turn: the damage it adds to the balance team against 5 dummies, and the effective health it adds to the dummies against the balance team. Its power is computed as the threat of a wave: damage per second x survival time, both with the item. No effect: a gain under the threshold (top right), lost in the noise of the fights or not measurable by them (e.g. a heal at the end of the round)." + reference + " Click a header to sort, hover it for the details.";
        }
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
                    measured ? $"{score.dps:0.0}" : "-",
                    measured ? $"{score.effectiveHealth:0}" : "-",
                    measured ? $"{score.threat:0}" : "-",
                    measured ? $"{score.healingPerSecond:0.0}" : "-",
                    measured ? $"{score.manaPerSecond:0.0}" : "-",
                    measured ? $"{score.spellSurvivalTime:0.0}s" : "-",
                    measured ? $"{score.spellDps:0.0}" : "-",
                    measured ? $"{score.spellEffectiveHealth:0}" : "-",
                    measured ? $"{score.spellThreat:0}" : "-",
                    StatusNames[status],
                },
                keys = new IComparable[] { bot.character.title, score.survivalTime, score.dps, score.effectiveHealth, score.threat, score.healingPerSecond, score.manaPerSecond, score.spellSurvivalTime, score.spellDps, score.spellEffectiveHealth, score.spellThreat, status },
            });
        }
        return rows;
    }

    // The reward items, with their score in the latest measure
    static List<Row> BuildItemRows(Table table)
    {
        ScoreFile.Measure measure = ReadLatest(ScoreMeasure.Kind.Items, table);
        table.baseline = measure?.itemBaseline;
        List<AItemFactory> items = ScoreMeasure.GetRewardItems();
        Dictionary<AItemFactory, string> fingerprints = measure != null ? ScoreMeasure.ComputeItemFingerprints(items) : new Dictionary<AItemFactory, string>();
        List<Row> rows = new List<Row>();
        foreach (AItemFactory item in items)
        {
            ItemScore score = measure?.items.Find(entry => entry.item == item.name)?.score;
            bool measured = score != null && !string.IsNullOrEmpty(score.fingerprint);
            int status = !measured ? 3 : score.fingerprint != fingerprints[item] ? 2 : score.timedOut ? 1 : 0;
            score ??= new ItemScore();
            string type = item.HasTag(TagNames.Player) ? "Character" : "Unit";
            rows.Add(new Row
            {
                asset = item,
                status = status,
                cells = new[]
                {
                    item.title,
                    type,
                    measured ? $"{score.dps:0.0}" : "-",
                    !measured ? "-" : score.hasDpsEffect ? $"{score.dpsGain:+0.0;-0.0}" : "No effect",
                    !measured ? "-" : score.hasDpsEffect ? $"{score.dpsGainShare:+0%;-0%}" : "No effect",
                    measured && score.hasDpsEffect && !string.IsNullOrEmpty(score.bestHolder) ? $"{score.bestHolder} ({score.bestHolderDpsGain:+0.0;-0.0})" : "-",
                    measured ? $"{score.survivalTime:0.0}s" : "-",
                    measured ? $"{score.effectiveHealth:0}" : "-",
                    !measured ? "-" : score.hasRobustnessEffect ? $"{score.effectiveHealthGain:+0;-0}" : "No effect",
                    !measured ? "-" : score.hasRobustnessEffect ? $"{score.effectiveHealthGainShare:+0%;-0%}" : "No effect",
                    measured ? $"{score.power:0}" : "-",
                    !measured ? "-" : score.hasPowerEffect ? $"{score.powerGain:+0;-0}" : "No effect",
                    !measured ? "-" : score.hasPowerEffect ? $"{score.powerGainShare:+0%;-0%}" : "No effect",
                    StatusNames[status],
                },
                keys = new IComparable[] { item.title, type, score.dps, score.dpsGain, score.dpsGainShare, score.bestHolderDpsGain, score.survivalTime, score.effectiveHealth, score.effectiveHealthGain, score.effectiveHealthGainShare, score.power, score.powerGain, score.powerGainShare, status },
            });
        }
        return rows;
    }

    static MultiColumnHeaderState.Column[] CreateWaveColumns()
    {
        return new[]
        {
            CreateColumn(new GUIContent("Wave", "Click to select the wave asset, the button measures it only"), 196f),
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
            CreateColumn(new GUIContent("Character", "Click to select the character asset, the button measures it only"), 146f),
            // Without skills: the character casts nothing
            CreateColumn(Header<CharacterScore>("Survival", "survivalTime"), 70f),
            CreateColumn(Header<CharacterScore>("DPS", "dps"), 70f),
            CreateColumn(Header<CharacterScore>("Eff. health", "effectiveHealth"), 80f),
            CreateColumn(Header<CharacterScore>("Threat", "threat"), 70f),
            // With skills: its healer bot casts them
            CreateColumn(Header<CharacterScore>("Heal/s", "healingPerSecond"), 65f),
            CreateColumn(Header<CharacterScore>("Mana/s", "manaPerSecond"), 65f),
            CreateColumn(Header<CharacterScore>("Survival", "spellSurvivalTime"), 70f),
            CreateColumn(Header<CharacterScore>("DPS", "spellDps"), 70f),
            CreateColumn(Header<CharacterScore>("Eff. health", "spellEffectiveHealth"), 80f),
            CreateColumn(Header<CharacterScore>("Threat", "spellThreat"), 70f),
            CreateColumn(new GUIContent("Status", StatusTooltip), 100f),
        };
    }

    static MultiColumnHeaderState.Column[] CreateItemColumns()
    {
        return new[]
        {
            CreateColumn(new GUIContent("Item", "Click to select the item asset, the button measures it only"), 186f),
            CreateColumn(new GUIContent("Type", "Unit item (held by a unit) or character item (held by the character)"), 70f),
            // Damage: on the balance team against the dummies
            CreateColumn(Header<ItemScore>("DPS", "dps"), 65f),
            CreateColumn(Header<ItemScore>("Gain", "dpsGain"), 70f),
            CreateColumn(Header<ItemScore>("Gain %", "dpsGainShare"), 70f),
            CreateColumn(Header<ItemScore>("Best holder", "bestHolder"), 150f),
            // Robustness: on the dummies against the balance team
            CreateColumn(Header<ItemScore>("Survival", "survivalTime"), 70f),
            CreateColumn(Header<ItemScore>("Eff. health", "effectiveHealth"), 80f),
            CreateColumn(Header<ItemScore>("Gain", "effectiveHealthGain"), 70f),
            CreateColumn(Header<ItemScore>("Gain %", "effectiveHealthGainShare"), 70f),
            // Power: both ways, as the threat of a wave
            CreateColumn(Header<ItemScore>("Power", "power"), 70f),
            CreateColumn(Header<ItemScore>("Gain", "powerGain"), 70f),
            CreateColumn(Header<ItemScore>("Gain %", "powerGainShare"), 70f),
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
        DrawGroups(table);
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
                    // A button measuring this row only, then a link to the asset: selects it in the Inspector and
                    // pings it in the Project window
                    Rect button = new Rect(inner.x, inner.y + 1f, MeasureButtonWidth, inner.height - 2f);
                    using (new EditorGUI.DisabledScope(ScoreMeasure.isRunning || FullSimulation.isRunning || EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        GUIContent measure = new GUIContent(EditorGUIUtility.IconContent("PlayButton").image, $"Measures {row.cells[column]} only, the other scores of the latest measure kept");
                        if (GUI.Button(button, measure, EditorStyles.miniButton))
                        {
                            ScoreMeasure.Start(_tab, row.asset);
                        }
                    }
                    inner.xMin += MeasureButtonWidth + 4f;
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

    // The titles of the column groups, above their columns and scrolled with them
    void DrawGroups(Table table)
    {
        if (table.groups.Length == 0)
        {
            return;
        }

        Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
        MultiColumnHeaderState.Column[] columns = table.header.state.columns;
        GUI.BeginClip(row);
        foreach (ColumnGroup group in table.groups)
        {
            float x = -_scroll.x;
            for (int column = 0; column < group.firstColumn; column++)
            {
                x += columns[column].width;
            }
            float width = 0f;
            for (int column = group.firstColumn; column < group.firstColumn + group.columnCount; column++)
            {
                width += columns[column].width;
            }

            Rect rect = new Rect(x + 1f, 1f, width - 2f, RowHeight - 2f);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.12f));
            GUI.Label(rect, group.title, EditorStyles.centeredGreyMiniLabel);
        }
        GUI.EndClip();
    }
}
