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

    [SerializeField] Vector2 _nodeSize = new Vector2(170f, 50f);
    [SerializeField] float _fontSize = 24f;
    [SerializeField] float _lineWidth = 4f;
    [SerializeField] float _highlightedLineWidth = 7f;
    [SerializeField] Color _lineColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] Color _nextLineColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] Color _visitedLineColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] float _pulseAmplitude = 0.08f;
    [SerializeField] float _pulseSpeed = 5f;

    [SerializeField] Dictionary<MapNodeType, Color> _roomColors = new Dictionary<MapNodeType, Color>
    {
        { MapNodeType.Combat, new Color(0.25f, 0.55f, 0.95f) },
        { MapNodeType.Elite, new Color(0.95f, 0.25f, 0.25f) },
        { MapNodeType.Treasure, new Color(1f, 0.78f, 0.15f) },
        { MapNodeType.Rest, new Color(0.25f, 0.85f, 0.45f) },
        { MapNodeType.Boss, new Color(0.75f, 0.25f, 0.95f) },
    };

    static readonly Color CurrentOutlineColor = new Color(1f, 0.8f, 0.2f, 1f);
    static readonly Color LockedGrey = new Color(0.35f, 0.35f, 0.4f);

    RunState _run;
    bool _canSelect;
    List<GameObject> _elements = new List<GameObject>();
    List<RectTransform> _pulsingNodes = new List<RectTransform>();

    RectTransform container => _container != null ? _container : (RectTransform)transform;

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
            default:
                return type.ToString();
        }
    }

    void Clear()
    {
        foreach (GameObject element in _elements)
        {
            Destroy(element);
        }
        _elements.Clear();
        _pulsingNodes.Clear();
    }

    void Build()
    {
        Vector2 areaSize = container.rect.size;

        // Lines first so the rooms are drawn over them
        foreach (MapNode node in _run.map.GetAllNodes())
        {
            foreach (MapNode next in node.next)
            {
                CreateLine(node, next, areaSize);
            }
        }

        foreach (MapNode node in _run.map.GetAllNodes())
        {
            CreateNode(node, GetNodeAnchor(node, _run.map));
        }
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

        GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(container, false);
        _elements.Add(line);

        Image image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        Vector2 fromAnchor = GetNodeAnchor(node, _run.map);
        Vector2 direction = Vector2.Scale(GetNodeAnchor(next, _run.map) - fromAnchor, areaSize);
        RectTransform rectTransform = (RectTransform)line.transform;
        PlaceAt(rectTransform, fromAnchor);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.sizeDelta = new Vector2(direction.magnitude, width);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    void CreateNode(MapNode node, Vector2 anchor)
    {
        GameObject button = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        button.name = $"Room {node}";
        button.transform.SetParent(container, false);
        _elements.Add(button);

        RectTransform rectTransform = (RectTransform)button.transform;
        PlaceAt(rectTransform, anchor);
        rectTransform.sizeDelta = _nodeSize;

        MapNodeState state = _run.GetNodeState(node);
        Color roomColor = _roomColors.TryGetValue(node.type, out Color typeColor) ? typeColor : Color.gray;
        NodeStyle style = GetNodeStyle(state, roomColor, _canSelect);

        Image image = button.GetComponent<Image>();
        image.color = style.fill;
        if (_nodeSprite != null)
        {
            image.sprite = _nodeSprite;
            image.type = Image.Type.Sliced;
        }
        if (style.hasOutline)
        {
            // Not QuickOutline's 3D Outline
            UnityEngine.UI.Outline outline = button.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = style.outline;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        text.text = GetNodeLabel(node.type);
        text.fontSize = _fontSize;
        text.fontStyle = style.isBold ? FontStyles.Bold : FontStyles.Normal;
        text.color = style.text;

        // The style already shows what can be clicked, the button must not dim it further
        Button buttonComponent = button.GetComponent<Button>();
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
