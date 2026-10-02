using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

// Map generation test scene: draws a run map with a copy of the settings that can be changed while playing,
// with the sliders of the panel or in the inspector (the settings asset itself is never modified). A changed
// setting redraws the same seed, to see what the change does; "New map" draws another seed.
public class MapGenerationPreview : MonoBehaviour
{
    // The panel is laid out for a 1080p screen, then scaled to the actual one
    const float ReferenceHeight = 1080f;
    const float PanelWidth = 420f;
    const float Margin = 16f;

    static readonly MapNodeType[] RoomTypes = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Treasure, MapNodeType.Rest, MapNodeType.Event };

    // One line of the room table
    public struct RoomRow
    {
        public MapNodeType type;
        public float weight;
        // Share of the random rooms the weight gives, from 0 to 1
        public float share;
        // Rooms of the type in the map drawn, fixed floors included, and their share of all its rooms
        public int count;
        public float mapShare;
    }

    [SerializeField] MapGenerationSettings _sourceSettings;
    [SerializeField] MapView _mapView;

    // Upper bounds of the sliders
    [SerializeField, Min(1)] int _maxFloorCount = 25;
    [SerializeField, Min(1)] int _maxColumnCount = 12;
    [SerializeField, Min(1)] int _maxPathCount = 12;

    // Copy of the source settings made when playing, the one the sliders change
    [SerializeField, InlineEditor(InlineEditorObjectFieldModes.Foldout), HideInEditorMode]
    MapGenerationSettings _settings;

    int _seed;
    // Room type edited under the table, an index in the settings
    int _selectedRoomType = 0;
    string _lastSettings;
    string _error;
    Dictionary<MapNodeType, int> _counts = new Dictionary<MapNodeType, int>();
    int _roomCount;
    Vector2 _scroll;
    string _message;
    float _messageTime;
    Vector2Int _screenSize;
    float _mapAreaLeft;
    System.Random _seedRandom = new System.Random();

    GUIStyle _panelStyle;
    GUIStyle _titleStyle;
    GUIStyle _sectionStyle;
    GUIStyle _labelStyle;
    GUIStyle _valueStyle;
    GUIStyle _buttonStyle;
    GUIStyle _smallButtonStyle;
    GUIStyle _toggleStyle;
    GUIStyle _headerStyle;
    GUIStyle _headerLeftStyle;
    GUIStyle _cellStyle;
    GUIStyle _cellValueStyle;
    GUIStyle _rowStyle;
    GUIStyle _selectedRowStyle;
    List<Texture2D> _textures = new List<Texture2D>();
    Dictionary<MapNodeType, Texture2D> _swatches = new Dictionary<MapNodeType, Texture2D>();

    float scale => Screen.height / ReferenceHeight;

    void Start()
    {
        _settings = Instantiate(_sourceSettings);
        _settings.name = _sourceSettings.name + " (copy)";
        _lastSettings = JsonUtility.ToJson(_settings);
        _mapAreaLeft = _mapView.mapArea.offsetMin.x;
        FitMapNextToThePanel();
        NewMap();
    }

    void OnDestroy()
    {
        if (_settings != null)
        {
            Destroy(_settings);
        }
        foreach (Texture2D texture in _textures)
        {
            Destroy(texture);
        }
    }

    void Update()
    {
        // The rooms are sized from the map area when drawn: a resized screen needs a new drawing
        if (_screenSize.x != Screen.width || _screenSize.y != Screen.height)
        {
            FitMapNextToThePanel();
            Generate();
        }

        // A setting changed, by a slider or in the inspector
        string settings = JsonUtility.ToJson(_settings);
        if (settings != _lastSettings)
        {
            _lastSettings = settings;
            Generate();
        }
    }

    // The map starts right of the panel instead of under it
    void FitMapNextToThePanel()
    {
        _screenSize = new Vector2Int(Screen.width, Screen.height);
        Canvas canvas = _mapView.GetComponentInParent<Canvas>();
        float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
        float panelRight = (PanelWidth + 2f * Margin) * scale / canvasScale;

        RectTransform mapArea = _mapView.mapArea;
        mapArea.offsetMin = new Vector2(Mathf.Max(_mapAreaLeft, panelRight), mapArea.offsetMin.y);
        Canvas.ForceUpdateCanvases();
    }

    [Button, HideInEditorMode]
    public void NewMap()
    {
        _seed = _seedRandom.Next(1, int.MaxValue);
        Generate();
    }

    void Generate()
    {
        RunMap map;
        try
        {
            map = MapGenerator.Generate(_settings, _seed);
        }
        catch (System.ArgumentException exception)
        {
            _error = exception.Message;
            return;
        }

        _error = null;
        _mapView.Display(new RunState(map), false);
        _counts = CountRooms(map, out _roomCount);
    }

    // How many rooms of each type the map has, the boss aside
    public static Dictionary<MapNodeType, int> CountRooms(RunMap map, out int roomCount)
    {
        Dictionary<MapNodeType, int> counts = new Dictionary<MapNodeType, int>();
        roomCount = 0;
        foreach (MapNode node in map.GetAllNodes())
        {
            if (node == map.boss)
            {
                continue;
            }
            counts.TryGetValue(node.type, out int count);
            counts[node.type] = count + 1;
            roomCount++;
        }
        return counts;
    }

    // The seed, then how many rooms of each type the map has (the boss aside) and their share
    public static string GetStats(RunMap map, int seed)
    {
        Dictionary<MapNodeType, int> counts = CountRooms(map, out int roomCount);

        StringBuilder stats = new StringBuilder();
        stats.AppendLine($"Seed {seed}");
        stats.AppendLine($"{roomCount} rooms");
        foreach (MapNodeType type in RoomTypes)
        {
            counts.TryGetValue(type, out int count);
            stats.AppendLine($"{MapView.GetNodeLabel(type)}: {count} ({GetShare(count, roomCount):0}%)");
        }
        return stats.ToString().TrimEnd();
    }

    // A row per room type of the settings: its weight and share, and what the map drawn got
    public static List<RoomRow> GetRoomRows(MapGenerationSettings settings, Dictionary<MapNodeType, int> counts, int roomCount)
    {
        List<float> weights = settings.roomTypes.ConvertAll(roomType => roomType.weight);
        List<RoomRow> rows = new List<RoomRow>();
        for (int i = 0; i < settings.roomTypes.Count; i++)
        {
            MapNodeType type = settings.roomTypes[i].type;
            int count = 0;
            if (counts != null)
            {
                counts.TryGetValue(type, out count);
            }
            rows.Add(new RoomRow
            {
                type = type,
                weight = weights[i],
                share = WeightShares.GetShare(weights, i),
                count = count,
                mapShare = roomCount > 0 ? (float)count / roomCount : 0f,
            });
        }
        return rows;
    }

    static float GetShare(int count, int total)
    {
        return total > 0 ? 100f * count / total : 0f;
    }

    #region Panel

    void OnGUI()
    {
        if (_settings == null)
        {
            return;
        }
        CreateStyles();

        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float height = Screen.height / scale - 2f * Margin;
        GUILayout.BeginArea(new Rect(Margin, Margin, PanelWidth, height), _panelStyle);
        _scroll = GUILayout.BeginScrollView(_scroll, GUIStyle.none, GUI.skin.verticalScrollbar);

        GUILayout.Label("Map generation", _titleStyle);
        DrawStats();
        GUILayout.Space(8f);
        if (GUILayout.Button("New map", _buttonStyle, GUILayout.Height(40f)))
        {
            NewMap();
        }
        DrawSaveButtons();

        Section("Layout");
        _settings.floorCount = IntSlider("Floors", _settings.floorCount, 1, _maxFloorCount);
        _settings.columnCount = IntSlider("Columns", _settings.columnCount, 1, _maxColumnCount);
        _settings.pathCount = IntSlider("Paths", _settings.pathCount, 1, _maxPathCount);
        // At most one start room per path and per column
        int maxStartRooms = Mathf.Max(1, Mathf.Min(_settings.pathCount, _settings.columnCount));
        _settings.startRoomCount = IntSlider("Start rooms", _settings.startRoomCount, 1, maxStartRooms);
        _settings.maxRoomsPerFloor = IntSlider("Max rooms per floor", _settings.maxRoomsPerFloor, 0, _settings.columnCount, "No limit");

        Section("Rooms");
        DrawRoomTable();
        _selectedRoomType = Mathf.Clamp(_selectedRoomType, 0, _settings.roomTypes.Count - 1);
        if (_settings.roomTypes.Count > 0)
        {
            DrawRoomType(_selectedRoomType, _settings.floorCount);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.matrix = Matrix4x4.identity;
    }

    // Save writes the tuned settings into the source asset, Revert brings its values back
    void DrawSaveButtons()
    {
        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
#if UNITY_EDITOR
        if (GUILayout.Button("Save to settings", _buttonStyle, GUILayout.Height(30f)))
        {
            SaveToSourceSettings();
        }
#endif
        if (GUILayout.Button("Revert", _buttonStyle, GUILayout.Height(30f)))
        {
            _settings.CopyFrom(_sourceSettings);
            ShowMessage("Settings reverted");
        }
        GUILayout.EndHorizontal();

        if (_message != null && Time.unscaledTime - _messageTime < 3f)
        {
            GUILayout.Label(_message, _valueStyle);
        }
    }

#if UNITY_EDITOR
    // Changes made to an asset while playing are kept, it's then written to disk right away
    void SaveToSourceSettings()
    {
        UnityEditor.Undo.RecordObject(_sourceSettings, "Save map generation settings");
        _sourceSettings.CopyFrom(_settings);
        UnityEditor.EditorUtility.SetDirty(_sourceSettings);
        UnityEditor.AssetDatabase.SaveAssetIfDirty(_sourceSettings);
        ShowMessage($"Saved to {_sourceSettings.name}");
    }
#endif

    void ShowMessage(string message)
    {
        _message = message;
        _messageTime = Time.unscaledTime;
    }

    void DrawStats()
    {
        if (_error != null)
        {
            GUILayout.Label(_error, _labelStyle);
            return;
        }

        Row("Seed", _seed.ToString());
        Row("Rooms", _roomCount.ToString());
    }

    // Type, weight, share of the random rooms, rooms of the type in the map drawn; a click on a row selects
    // the type edited under the table
    void DrawRoomTable()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(20f);
        GUILayout.Label("Type", _headerLeftStyle, GUILayout.Width(100f));
        GUILayout.Label("Weight", _headerStyle, GUILayout.Width(70f));
        GUILayout.Label("Share", _headerStyle, GUILayout.Width(70f));
        GUILayout.Label("In this map", _headerStyle, GUILayout.Width(100f));
        GUILayout.EndHorizontal();

        List<RoomRow> rows = GetRoomRows(_settings, _counts, _roomCount);
        for (int i = 0; i < rows.Count; i++)
        {
            RoomRow row = rows[i];
            GUILayout.BeginHorizontal(i == _selectedRoomType ? _selectedRowStyle : _rowStyle);
            DrawSwatch(row.type, 14f);
            GUILayout.Space(8f);
            GUILayout.Label(MapView.GetNodeLabel(row.type), _cellStyle, GUILayout.Width(100f));
            GUILayout.Label(row.weight.ToString("0.00"), _cellValueStyle, GUILayout.Width(70f));
            GUILayout.Label($"{row.share * 100f:0}%", _cellValueStyle, GUILayout.Width(70f));
            GUILayout.Label($"{row.count} ({row.mapShare * 100f:0}%)", _cellValueStyle, GUILayout.Width(100f));
            GUILayout.EndHorizontal();

            Rect rowRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                _selectedRoomType = i;
                Event.current.Use();
            }
        }
    }

    void Section(string title)
    {
        GUILayout.Space(14f);
        GUILayout.Label(title.ToUpperInvariant(), _sectionStyle);
    }

    void Row(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, _labelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, _valueStyle);
        GUILayout.EndHorizontal();
    }

    int IntSlider(string label, int value, int min, int max, string minLabel = null)
    {
        value = Mathf.Clamp(value, min, max);
        Row(label, value == min && minLabel != null ? minLabel : value.ToString());
        return Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));
    }

    // The share of the random rooms the type gets, by steps of 1%; changing it rescales the other types so
    // the shares stay at 100%
    void ShareSlider(int index)
    {
        List<float> weights = _settings.roomTypes.ConvertAll(roomType => roomType.weight);
        float share = WeightShares.GetShare(weights, index);
        Row("Weight", weights[index].ToString("0.00"));
        Row("Share of random rooms", $"{share * 100f:0}%");
        float newShare = Mathf.Round(GUILayout.HorizontalSlider(share, 0f, 1f) * 100f) / 100f;
        if (!Mathf.Approximately(newShare, Mathf.Round(share * 100f) / 100f))
        {
            WeightShares.SetShare(weights, index, newShare);
            for (int i = 0; i < weights.Count; i++)
            {
                _settings.roomTypes[i].weight = weights[i];
            }
        }
    }

    // Floors shown from 1 like in game, stored from 0
    void DrawRoomType(int index, int floorCount)
    {
        RoomTypeSettings roomType = _settings.roomTypes[index];
        GUILayout.Space(14f);
        GUILayout.BeginHorizontal();
        DrawSwatch(roomType.type, 14f);
        GUILayout.Space(6f);
        GUILayout.Label(MapView.GetNodeLabel(roomType.type).ToUpperInvariant(), _sectionStyle);
        GUILayout.EndHorizontal();

        // Changed after the loop, the list being drawn
        int removedIndex = -1;
        for (int i = 0; i < roomType.fixedFloors.Count; i++)
        {
            int floor = Mathf.Clamp(roomType.fixedFloors[i] + 1, 1, floorCount);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fixed floor", _labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(floor.ToString(), _valueStyle);
            if (GUILayout.Button("x", GUILayout.Width(22f)))
            {
                removedIndex = i;
            }
            GUILayout.EndHorizontal();
            roomType.fixedFloors[i] = Mathf.RoundToInt(GUILayout.HorizontalSlider(floor, 1, floorCount)) - 1;
        }
        if (removedIndex >= 0)
        {
            roomType.fixedFloors.RemoveAt(removedIndex);
        }
        if (GUILayout.Button("+ Fixed floor", _smallButtonStyle))
        {
            roomType.fixedFloors.Add(floorCount - 1);
        }

        GUILayout.Space(4f);
        roomType.firstFloor = IntSlider("First floor", roomType.firstFloor + 1, 1, floorCount) - 1;
        ShareSlider(index);
        roomType.canFollowItself = GUILayout.Toggle(roomType.canFollowItself, "  Can follow itself", _toggleStyle);
    }

    // A square of the room color, the 1 pixel texture stretched over it, centered on the line
    void DrawSwatch(MapNodeType type, float size)
    {
        Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        rect.y += Mathf.Max(0f, (_cellStyle.lineHeight - size) * 0.5f) + _cellStyle.padding.top;
        GUI.DrawTexture(rect, GetSwatch(type), ScaleMode.StretchToFill);
    }

    Texture2D GetSwatch(MapNodeType type)
    {
        if (!_swatches.TryGetValue(type, out Texture2D swatch))
        {
            swatch = CreateTexture(_mapView.GetRoomColor(type));
            _swatches[type] = swatch;
        }
        return swatch;
    }

    Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        _textures.Add(texture);
        return texture;
    }

    void CreateStyles()
    {
        if (_panelStyle != null)
        {
            return;
        }

        _panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(16, 16, 14, 14) };
        _panelStyle.normal.background = CreateTexture(new Color(0.09f, 0.09f, 0.12f, 0.92f));

        _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        _titleStyle.normal.textColor = Color.white;
        _titleStyle.margin.bottom = 10;

        _sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
        _sectionStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);

        _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
        _labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);

        _valueStyle = new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        _valueStyle.normal.textColor = Color.white;

        _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        _smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13 };

        _toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 15 };
        _toggleStyle.normal.textColor = _labelStyle.normal.textColor;
        _toggleStyle.onNormal.textColor = Color.white;

        _headerStyle = new GUIStyle(_labelStyle) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        _headerStyle.normal.textColor = new Color(0.6f, 0.6f, 0.7f);
        _headerLeftStyle = new GUIStyle(_headerStyle) { alignment = TextAnchor.MiddleLeft };
        _cellStyle = new GUIStyle(_labelStyle) { fontSize = 14 };
        _cellValueStyle = new GUIStyle(_valueStyle) { fontSize = 14 };

        _rowStyle = new GUIStyle { padding = new RectOffset(4, 4, 3, 3) };
        _selectedRowStyle = new GUIStyle(_rowStyle);
        _selectedRowStyle.normal.background = CreateTexture(new Color(1f, 1f, 1f, 0.12f));
    }

    #endregion
}
