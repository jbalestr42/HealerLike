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
    const float PanelWidth = 340f;
    const float Margin = 16f;

    static readonly MapNodeType[] RoomTypes = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Treasure, MapNodeType.Rest };

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

        foreach (RoomTypeSettings roomType in _settings.roomTypes)
        {
            DrawRoomType(roomType, _settings.floorCount);
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
        foreach (MapNodeType type in RoomTypes)
        {
            _counts.TryGetValue(type, out int count);
            GUILayout.BeginHorizontal();
            GUILayout.Box(GetSwatch(type), GUIStyle.none, GUILayout.Width(14f), GUILayout.Height(14f));
            GUILayout.Space(6f);
            GUILayout.Label(MapView.GetNodeLabel(type), _labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{count}   {GetShare(count, _roomCount):0}%", _valueStyle);
            GUILayout.EndHorizontal();
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

    float WeightSlider(float value)
    {
        Row("Weight", value.ToString("0.00"));
        return Mathf.Round(GUILayout.HorizontalSlider(value, 0f, 1f) * 100f) / 100f;
    }

    // Floors shown from 1 like in game, stored from 0
    void DrawRoomType(RoomTypeSettings roomType, int floorCount)
    {
        GUILayout.Space(14f);
        GUILayout.BeginHorizontal();
        GUILayout.Box(GetSwatch(roomType.type), GUIStyle.none, GUILayout.Width(12f), GUILayout.Height(12f));
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
        roomType.weight = WeightSlider(roomType.weight);
        roomType.canFollowItself = GUILayout.Toggle(roomType.canFollowItself, "  Can follow itself", _toggleStyle);
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
    }

    #endregion
}
