using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Run map overlay: one button per room and a line per path, from the first floor at the bottom
// to the boss at the top. Only the rooms the player can travel to are clickable.
public class MapView : AView
{
    public UnityEvent<MapNode> OnNodeSelected = new UnityEvent<MapNode>();

    // Area where the map is drawn, the view itself when not set
    [SerializeField] RectTransform _container;
    [SerializeField] Button _closeButton;

    [SerializeField] Vector2 _nodeSize = new Vector2(120f, 40f);
    [SerializeField] float _lineWidth = 4f;
    [SerializeField] Color _lineColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] Color _visitedLineColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField, Range(0f, 1f)] float _lockedAlpha = 0.35f;

    [SerializeField] Dictionary<MapNodeType, Color> _nodeColors = new Dictionary<MapNodeType, Color>
    {
        { MapNodeType.Combat, new Color(0.55f, 0.55f, 0.6f) },
        { MapNodeType.Elite, new Color(0.8f, 0.25f, 0.2f) },
        { MapNodeType.Treasure, new Color(0.9f, 0.7f, 0.2f) },
        { MapNodeType.Rest, new Color(0.3f, 0.7f, 0.35f) },
        { MapNodeType.Boss, new Color(0.5f, 0.1f, 0.5f) },
    };

    RunState _run;
    bool _canSelect;
    List<GameObject> _elements = new List<GameObject>();

    RectTransform container => _container != null ? _container : (RectTransform)transform;

    void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(() => UIManager.instance.PopCurrentView());
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
    }

    void Build()
    {
        Vector2 areaSize = container.rect.size;

        // Lines first so the rooms are drawn over them
        foreach (MapNode node in _run.map.GetAllNodes())
        {
            foreach (MapNode next in node.next)
            {
                bool isVisitedPath = _run.IsVisited(node) && _run.IsVisited(next);
                CreateLine(GetNodeAnchor(node, _run.map), GetNodeAnchor(next, _run.map), areaSize, isVisitedPath ? _visitedLineColor : _lineColor);
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

    void CreateLine(Vector2 fromAnchor, Vector2 toAnchor, Vector2 areaSize, Color color)
    {
        GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(container, false);
        _elements.Add(line);

        Image image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        Vector2 direction = Vector2.Scale(toAnchor - fromAnchor, areaSize);
        RectTransform rectTransform = (RectTransform)line.transform;
        PlaceAt(rectTransform, fromAnchor);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.sizeDelta = new Vector2(direction.magnitude, _lineWidth);
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
        Color color = _nodeColors.TryGetValue(node.type, out Color typeColor) ? typeColor : Color.gray;
        if (state == MapNodeState.Locked)
        {
            color.a = _lockedAlpha;
        }
        button.GetComponent<Image>().color = color;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        text.text = GetNodeLabel(node.type);
        text.fontStyle = state == MapNodeState.Current ? FontStyles.Bold | FontStyles.Underline : FontStyles.Normal;
        text.color = state == MapNodeState.Locked ? new Color(0f, 0f, 0f, _lockedAlpha) : Color.black;

        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.interactable = _canSelect && state == MapNodeState.Available;
        buttonComponent.onClick.AddListener(() => OnNodeSelected.Invoke(node));
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
