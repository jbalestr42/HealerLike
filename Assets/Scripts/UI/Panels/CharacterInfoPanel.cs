using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Left panel of the game HUD, hidden until its "Character" button is clicked: the mana, stats (with their
// modifiers), items and active effects of the player character, refreshed live.
// The UI is built in code, the scene only needs this component on a RectTransform of a canvas.
public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] SandboxButton _buttonPrefab;
    [SerializeField] Vector2 _buttonPosition = new Vector2(210f, -64f);
    [SerializeField] Vector2 _buttonSize = new Vector2(120f, 30f);
    [SerializeField] Vector2 _panelPosition = new Vector2(15f, -140f);
    [SerializeField] float _width = 380f;
    [SerializeField] float _maxHeight = 560f;
    [SerializeField] float _padding = 12f;
    [SerializeField] float _fontSize = 16f;
    [SerializeField] float _refreshPeriod = 0.1f;
    [SerializeField] Color _backgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.85f);

    RectTransform _panel;
    RectTransform _viewport;
    ScrollRect _scrollRect;
    TMP_Text _body;
    bool _isBuilt = false;
    float _nextRefreshTime = 0f;

    public bool isOpen => _panel != null && _panel.gameObject.activeSelf;

    void Awake()
    {
        Build();
    }

    void Update()
    {
        // Unscaled, the panel must stay alive when the game speed is 0
        if (!isOpen || Time.unscaledTime < _nextRefreshTime)
        {
            return;
        }
        _nextRefreshTime = Time.unscaledTime + _refreshPeriod;
        Refresh();
    }

    public void Toggle()
    {
        Build();
        _panel.gameObject.SetActive(!_panel.gameObject.activeSelf);
        _nextRefreshTime = 0f;
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    #region Build

    void Build()
    {
        if (_isBuilt)
        {
            return;
        }
        _isBuilt = true;

        RectTransform root = (RectTransform)transform;

        SandboxButton button = Instantiate(_buttonPrefab, root);
        button.Init("Character", Toggle);
        SetTopLeft((RectTransform)button.transform, _buttonPosition, _buttonSize);

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        _panel.SetParent(root, false);
        SetTopLeft(_panel, _panelPosition, new Vector2(_width, _maxHeight));
        _panel.GetComponent<Image>().color = _backgroundColor;

        // Scrollable body, a transparent image catches the mouse wheel
        _viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
        _viewport.SetParent(_panel, false);
        _viewport.anchorMin = Vector2.zero;
        _viewport.anchorMax = Vector2.one;
        _viewport.offsetMin = new Vector2(_padding, _padding);
        _viewport.offsetMax = new Vector2(-_padding, -_padding);
        _viewport.GetComponent<Image>().color = Color.clear;

        _body = new GameObject("Body", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        _body.transform.SetParent(_viewport, false);
        _body.fontSize = _fontSize;
        _body.color = Color.white;
        _body.richText = true;
        _body.textWrappingMode = TextWrappingModes.Normal;
        RectTransform bodyRect = _body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        _scrollRect = _viewport.GetComponent<ScrollRect>();
        _scrollRect.viewport = _viewport;
        _scrollRect.content = bodyRect;
        _scrollRect.horizontal = false;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 30f;

        _panel.gameObject.SetActive(false);
    }

    static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    #endregion

    #region Content

    void Refresh()
    {
        Character character = PlayerBehaviour.instance.character;
        _body.text = character != null ? BuildBody(character) : $"<color={EntityInfoFormatter.MutedColor}>No character</color>";
        _body.ForceMeshUpdate();
        float bodyHeight = Mathf.Max(_body.preferredHeight, _body.textBounds.size.y);
        _body.rectTransform.sizeDelta = new Vector2(_body.rectTransform.sizeDelta.x, bodyHeight);

        // The panel grows with its content, then the body scrolls
        _panel.sizeDelta = new Vector2(_width, Mathf.Min(bodyHeight + 2f * _padding, _maxHeight));
    }

    public static string BuildBody(Character character)
    {
        StringBuilder builder = new StringBuilder();
        GameObject owner = character.gameObject;

        string title = character.data != null ? character.data.title : owner.name;
        builder.Append($"<size=+6><b>{title}</b></size>\n");
        EntityInfoFormatter.AppendSection(builder, "Mana", GetManaLines(character));
        EntityInfoFormatter.AppendSection(builder, "Stats", EntityInfoFormatter.GetAttributeLines(character.attributeManager, owner));
        EntityInfoFormatter.AppendSection(builder, "Items", GetItemLines(character));
        EntityInfoFormatter.AppendSection(builder, "Active effects", character.buffManager != null ? EntityInfoFormatter.GetBuffLines(character.buffManager, owner) : new List<string>());
        return builder.ToString().TrimEnd();
    }

    static List<string> GetManaLines(Character character)
    {
        List<string> lines = new List<string>();
        if (character.mana != null)
        {
            lines.Add($"<b>{EntityInfoFormatter.FormatNumber(character.mana.Value)} / {EntityInfoFormatter.FormatNumber(character.mana.Max)}</b>");
        }
        return lines;
    }

    // The items the character starts with, then the ones won during the run
    static List<string> GetItemLines(Character character)
    {
        List<string> lines = new List<string>();
        foreach (AItem item in character.items)
        {
            lines.Add(EntityInfoFormatter.FormatItem(item, true));
        }
        foreach (InventoryItemData itemData in character.inventoryHandler.items)
        {
            lines.Add(EntityInfoFormatter.FormatItem(itemData.item, false));
        }
        return lines;
    }

    #endregion
}
