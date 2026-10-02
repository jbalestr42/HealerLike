using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Run map overlay: one button per room and a line per path, from the first floor at the bottom
// to the boss at the top. Only the rooms the player can travel to are clickable.
public class MapView : AView
{
    // How a room is drawn, depending on its state
    public struct NodeStyle
    {
        public Color fill;
        public Color text;
        public bool hasOutline;
        public Color outline;
        public bool isBold;
        public bool pulses;
    }

    public UnityEvent<MapNode> OnNodeSelected = new UnityEvent<MapNode>();

    // Area where the map is drawn, the view itself when not set
    [SerializeField] RectTransform _container;
    [SerializeField] Button _closeButton;
    [SerializeField] TMP_Text _title;
    // Rounded sprite of the rooms, sliced; plain rectangles when not set
    [SerializeField] Sprite _nodeSprite;

    // Every room drawn in its own color, as if it could be picked (e.g. the map generation test scene)
    [SerializeField] bool _revealAll = false;

    [SerializeField] Vector2 _nodeSize = new Vector2(170f, 50f);
    [SerializeField] float _fontSize = 24f;
    [SerializeField] float _lineWidth = 4f;
    [SerializeField] float _highlightedLineWidth = 7f;
    [SerializeField] Color _lineColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] Color _nextLineColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] Color _visitedLineColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] float _pulseAmplitude = 0.08f;
    [SerializeField] float _pulseSpeed = 5f;

    [SerializeField] Dictionary<MapNodeType, Color> _roomColors = new Dictionary<MapNodeType, Color>(DefaultRoomColors);

    // Also used for the types missing from the serialized colors (e.g. a type added after the prefab was saved)
    static readonly Dictionary<MapNodeType, Color> DefaultRoomColors = new Dictionary<MapNodeType, Color>
    {
        { MapNodeType.Combat, new Color(0.25f, 0.55f, 0.95f) },
        { MapNodeType.Elite, new Color(0.95f, 0.25f, 0.25f) },
        { MapNodeType.Treasure, new Color(1f, 0.78f, 0.15f) },
        { MapNodeType.Rest, new Color(0.25f, 0.85f, 0.45f) },
        { MapNodeType.Boss, new Color(0.75f, 0.25f, 0.95f) },
        { MapNodeType.Event, new Color(0.2f, 0.85f, 0.9f) },
    };

    static readonly Color CurrentOutlineColor = new Color(1f, 0.8f, 0.2f, 1f);
    static readonly Color LockedGrey = new Color(0.35f, 0.35f, 0.4f);

    RunState _run;
    bool _canSelect;
    List<RectTransform> _pulsingNodes = new List<RectTransform>();

    // Kept from a drawing to the next, the ones a map doesn't need hidden: creating a TextMeshPro button costs
    // about 5 ms, and a map has dozens of rooms
    List<Image> _lines = new List<Image>();
    List<Button> _rooms = new List<Button>();
    int _usedLineCount = 0;
    int _usedRoomCount = 0;
    // The lines under the rooms, whatever order they were created in
    RectTransform _lineLayer;
    RectTransform _roomLayer;

    // The buttons of the rooms of the map drawn
    public IReadOnlyList<Button> roomButtons => _rooms.GetRange(0, _usedRoomCount);

    RectTransform container => _container != null ? _container : (RectTransform)transform;
    // Area where the map is drawn, to make room for something else next to it (e.g. a settings panel)
    public RectTransform mapArea => container;

    public Color GetRoomColor(MapNodeType type)
    {
        if (_roomColors.TryGetValue(type, out Color color) || DefaultRoomColors.TryGetValue(type, out color))
        {
            return color;
        }
        return Color.gray;
    }

    void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(() => UIManager.instance.PopCurrentView());
        }
    }

    // The next rooms breathe to catch the eye, unscaled so it also works when the game is paused
    void Update()
    {
        float scale = 1f + _pulseAmplitude * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _pulseSpeed));
        foreach (RectTransform node in _pulsingNodes)
        {
            node.localScale = new Vector3(scale, scale, 1f);
        }
    }

    // canSelect is false when the map is only opened to look at it
    public void Display(RunState run, bool canSelect)
    {
        _run = run;
        _canSelect = canSelect;
        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(!canSelect);
        }
        if (_title != null)
        {
            _title.text = canSelect ? "Choose your next room" : "Map";
        }
        Clear();
        Build();
    }

    // Normalized position of a room in the map area: columns spread on x, floors on y, boss centered on top
    public static Vector2 GetNodeAnchor(MapNode node, RunMap map)
    {
        float y = (node.floor + 0.5f) / (map.floorCount + 1);
        if (node == map.boss)
        {
            return new Vector2(0.5f, y);
        }
        return new Vector2((node.column + 0.5f) / map.columnCount, y);
    }

    // Available rooms stand out, visited ones fade, locked ones stay readable but greyed out
    public static NodeStyle GetNodeStyle(MapNodeState state, Color roomColor, bool canSelect)
    {
        switch (state)
        {
            case MapNodeState.Available:
                return new NodeStyle
                {
                    fill = roomColor,
                    text = Color.white,
                    hasOutline = true,
                    outline = Color.white,
                    isBold = true,
                    pulses = canSelect,
                };

            case MapNodeState.Current:
                return new NodeStyle
                {
                    fill = roomColor,
                    text = Color.white,
                    hasOutline = true,
                    outline = CurrentOutlineColor,
                    isBold = true,
                };

            case MapNodeState.Visited:
                return new NodeStyle
                {
                    fill = Color.Lerp(roomColor, Color.black, 0.45f),
                    text = new Color(1f, 1f, 1f, 0.75f),
                };

            default:
                return new NodeStyle
                {
                    fill = Color.Lerp(Color.Lerp(roomColor, LockedGrey, 0.6f), Color.black, 0.3f),
                    text = new Color(1f, 1f, 1f, 0.6f),
                };
        }
    }

    public static string GetNodeLabel(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Combat:
                return "Combat";
            case MapNodeType.Elite:
                return "Elite";
            case MapNodeType.Treasure:
                return "Treasure";
            case MapNodeType.Rest:
                return "Rest";
            case MapNodeType.Boss:
                return "Boss";
            case MapNodeType.Event:
                return "Event";
            default:
                return type.ToString();
        }
    }

    // Hides the elements of the previous drawing, to be used again
    void Clear()
    {
        foreach (Image line in _lines)
        {
            line.gameObject.SetActive(false);
        }
        foreach (Button room in _rooms)
        {
            room.gameObject.SetActive(false);
        }
        _usedLineCount = 0;
        _usedRoomCount = 0;
        _pulsingNodes.Clear();
    }

    // A full size child of the container, so the elements in it are placed as if they were in the container
    RectTransform GetLayer(ref RectTransform layer, string name)
    {
        if (layer == null)
        {
            layer = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            layer.SetParent(container, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
        }
        return layer;
    }

    Image TakeLine()
    {
        if (_usedLineCount == _lines.Count)
        {
            GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(GetLayer(ref _lineLayer, "Lines"), false);
            Image image = line.GetComponent<Image>();
            image.raycastTarget = false;
            _lines.Add(image);
        }

        Image taken = _lines[_usedLineCount++];
        taken.gameObject.SetActive(true);
        return taken;
    }

    Button TakeRoom()
    {
        if (_usedRoomCount == _rooms.Count)
        {
            GameObject room = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
            room.transform.SetParent(GetLayer(ref _roomLayer, "Rooms"), false);
            _rooms.Add(room.GetComponent<Button>());
        }

        Button taken = _rooms[_usedRoomCount++];
        taken.gameObject.SetActive(true);
        taken.onClick.RemoveAllListeners();
        taken.transform.localScale = Vector3.one;
        return taken;
    }

    void Build()
    {
        Vector2 areaSize = container.rect.size;
        // Created first, so the rooms are drawn over the lines
        GetLayer(ref _lineLayer, "Lines");
        GetLayer(ref _roomLayer, "Rooms");

        // Lines first so the rooms are drawn over them
        foreach (MapNode node in _run.map.GetAllNodes())
        {
            foreach (MapNode next in node.next)
            {
                CreateLine(node, next, areaSize);
            }
        }

        Vector2 nodeSize = GetNodeSize(_nodeSize, areaSize, _run.map);
        foreach (MapNode node in _run.map.GetAllNodes())
        {
            CreateNode(node, GetNodeAnchor(node, _run.map), nodeSize);
        }
    }

    // The room size, shrunk to leave a gap with its neighbours when the map has too many floors or columns
    // for the area (e.g. the 15 floors of 7 columns of a Slay the Spire map)
    public static Vector2 GetNodeSize(Vector2 maxSize, Vector2 areaSize, RunMap map)
    {
        float cellWidth = areaSize.x / map.columnCount;
        // The floors and the boss on top
        float cellHeight = areaSize.y / (map.floorCount + 1);
        return new Vector2(Mathf.Min(maxSize.x, cellWidth * 0.9f), Mathf.Min(maxSize.y, cellHeight * 0.8f));
    }

    // Elements are anchored on their normalized position, so the map follows the container size
    static void PlaceAt(RectTransform rectTransform, Vector2 anchor)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    // Taken path in gold, paths from the current room to the next ones in white, the others faded
    void CreateLine(MapNode node, MapNode next, Vector2 areaSize)
    {
        bool isVisitedPath = _run.IsVisited(node) && _run.IsVisited(next);
        bool isNextPath = node == _run.currentNode;
        Color color = isVisitedPath ? _visitedLineColor : (isNextPath ? _nextLineColor : _lineColor);
        float width = isVisitedPath || isNextPath ? _highlightedLineWidth : _lineWidth;

        Image image = TakeLine();
        image.color = color;

        Vector2 fromAnchor = GetNodeAnchor(node, _run.map);
        Vector2 direction = Vector2.Scale(GetNodeAnchor(next, _run.map) - fromAnchor, areaSize);
        RectTransform rectTransform = (RectTransform)image.transform;
        PlaceAt(rectTransform, fromAnchor);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.sizeDelta = new Vector2(direction.magnitude, width);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    void CreateNode(MapNode node, Vector2 anchor, Vector2 nodeSize)
    {
        Button buttonComponent = TakeRoom();
        GameObject button = buttonComponent.gameObject;
        button.name = $"Room {node}";

        RectTransform rectTransform = (RectTransform)button.transform;
        PlaceAt(rectTransform, anchor);
        rectTransform.sizeDelta = nodeSize;

        MapNodeState state = _revealAll ? MapNodeState.Available : _run.GetNodeState(node);
        Color roomColor = GetRoomColor(node.type);
        NodeStyle style = GetNodeStyle(state, roomColor, _canSelect);

        Image image = button.GetComponent<Image>();
        image.color = style.fill;
        if (_nodeSprite != null)
        {
            image.sprite = _nodeSprite;
            image.type = Image.Type.Sliced;
        }
        // Not QuickOutline's 3D Outline; kept on a reused room, only shown when the style has one
        UnityEngine.UI.Outline outline = button.GetComponent<UnityEngine.UI.Outline>();
        if (style.hasOutline && outline == null)
        {
            outline = button.AddComponent<UnityEngine.UI.Outline>();
        }
        if (outline != null)
        {
            outline.enabled = style.hasOutline;
            outline.effectColor = style.outline;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        text.text = GetNodeLabel(node.type);
        text.fontSize = _fontSize;
        text.fontStyle = style.isBold ? FontStyles.Bold : FontStyles.Normal;
        text.color = style.text;

        // The style already shows what can be clicked, the button must not dim it further
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        buttonComponent.colors = colors;
        buttonComponent.interactable = _canSelect && state == MapNodeState.Available;
        buttonComponent.onClick.AddListener(() => OnNodeSelected.Invoke(node));

        if (style.pulses)
        {
            _pulsingNodes.Add(rectTransform);
        }
    }

    #region AView

    public override void Show()
    {
        GetComponent<CanvasGroup>().alpha = 1f;
    }

    public override void Hide()
    {
        GetComponent<CanvasGroup>().alpha = 0.1f;
    }

    #endregion
}
