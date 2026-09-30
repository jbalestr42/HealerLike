using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top right panel of the sandbox: everything known about the selected entity (health, stats and
// their modifiers, targeting, skills, items, active effects, on hit effects), refreshed live.
// The UI is built in code, the scene only needs this component on a RectTransform of a canvas.
public class EntityInfoPanel : APanel
{
    [SerializeField] SandboxButton _buttonPrefab;
    [SerializeField] float _width = 440f;
    [SerializeField] float _maxHeight = 760f;
    [SerializeField] Vector2 _margin = new Vector2(10f, 10f);
    [SerializeField] float _padding = 12f;
    [SerializeField] float _fontSize = 16f;
    [SerializeField] float _buttonHeight = 28f;
    [SerializeField] float _refreshPeriod = 0.1f;
    [SerializeField] Color _backgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.85f);

    RectTransform _rect;
    TMP_Text _title;
    SandboxButton _targetButton;
    SandboxButton _closeButton;
    RectTransform _viewport;
    ScrollRect _scrollRect;
    TMP_Text _body;
    bool _isBuilt = false;

    Entity _entity;
    float _nextRefreshTime = 0f;

    void Awake()
    {
        Build();
    }

    #region APanel

    public override void OnShowUI(GameObject selectedObject)
    {
        Build();
        _entity = selectedObject.GetComponent<Entity>();
        _nextRefreshTime = 0f;
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    public override void UpdateUI(GameObject selectedObject)
    {
        // Unscaled, the panel must stay alive when the game speed is 0
        if (_entity == null || Time.unscaledTime < _nextRefreshTime)
        {
            return;
        }
        _nextRefreshTime = Time.unscaledTime + _refreshPeriod;
        Refresh();
    }

    public override void OnHideUI(GameObject selectedObject)
    {
        _entity = null;
    }

    #endregion

    #region Build

    void Build()
    {
        if (_isBuilt)
        {
            return;
        }
        _isBuilt = true;

        _rect = (RectTransform)transform;
        _rect.anchorMin = Vector2.one;
        _rect.anchorMax = Vector2.one;
        _rect.pivot = Vector2.one;
        _rect.anchoredPosition = -_margin;
        _rect.sizeDelta = new Vector2(_width, _maxHeight);

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = _backgroundColor;

        _title = CreateText("Title", _rect);
        SetTopStretch(_title.rectTransform, _padding, 0f);

        float buttonWidth = (_width - 2f * _padding - 8f) / 2f;
        _targetButton = CreateButton(_padding, buttonWidth, CycleTargetBehaviour);
        _closeButton = CreateButton(_padding + buttonWidth + 8f, buttonWidth, Close);
        _closeButton.SetLabel("Close");

        // Scrollable body, a transparent image catches the mouse wheel
        _viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
        _viewport.SetParent(_rect, false);
        _viewport.anchorMin = Vector2.zero;
        _viewport.anchorMax = Vector2.one;
        // Final width right away, the text wraps on it when measured
        _viewport.offsetMin = new Vector2(_padding, _padding);
        _viewport.offsetMax = new Vector2(-_padding, -_padding);
        _viewport.GetComponent<Image>().color = Color.clear;

        _body = CreateText("Body", _viewport);
        SetTopStretch(_body.rectTransform, 0f, 0f);

        _scrollRect = _viewport.GetComponent<ScrollRect>();
        _scrollRect.viewport = _viewport;
        _scrollRect.content = _body.rectTransform;
        _scrollRect.horizontal = false;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 30f;
    }

    TMP_Text CreateText(string name, Transform parent)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.fontSize = _fontSize;
        text.color = Color.white;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    SandboxButton CreateButton(float x, float width, UnityEngine.Events.UnityAction onClick)
    {
        SandboxButton button = Instantiate(_buttonPrefab, _rect);
        button.Init("", onClick);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, _buttonHeight);
        return button;
    }

    // Anchored on the top edge, stretched between the side paddings
    static void SetTopStretch(RectTransform rect, float sidePadding, float top)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(sidePadding, rect.offsetMin.y);
        rect.offsetMax = new Vector2(-sidePadding, rect.offsetMax.y);
        rect.anchoredPosition = new Vector2(0f, -top);
    }

    #endregion

    #region Actions

    void CycleTargetBehaviour()
    {
        if (_entity == null)
        {
            return;
        }

        TargetProvider targetProvider = _entity.targetProvider;
        targetProvider.targetBehaviourType = ATargetBehaviour.GetNextSupportedType(targetProvider.targetBehaviourType);
        _nextRefreshTime = 0f;
    }

    void Close()
    {
        InteractionManager.instance.CancelSelection();
    }

    #endregion

    #region Content

    void Refresh()
    {
        float textWidth = _width - 2f * _padding;

        _title.text = BuildTitle();
        float titleHeight = _title.GetPreferredValues(_title.text, textWidth, 0f).y;
        _title.rectTransform.sizeDelta = new Vector2(_title.rectTransform.sizeDelta.x, titleHeight);
        _title.rectTransform.anchoredPosition = new Vector2(0f, -_padding);

        float buttonsTop = _padding + titleHeight + 6f;
        _targetButton.SetLabel($"Targeting: {_entity.targetProvider.targetBehaviourType}");
        ((RectTransform)_targetButton.transform).anchoredPosition = new Vector2(((RectTransform)_targetButton.transform).anchoredPosition.x, -buttonsTop);
        ((RectTransform)_closeButton.transform).anchoredPosition = new Vector2(((RectTransform)_closeButton.transform).anchoredPosition.x, -buttonsTop);

        // The rendered text can go a few pixels below its preferred height
        _body.text = BuildBody();
        _body.ForceMeshUpdate();
        float bodyHeight = Mathf.Max(_body.preferredHeight, _body.textBounds.size.y);
        _body.rectTransform.sizeDelta = new Vector2(_body.rectTransform.sizeDelta.x, bodyHeight);

        // The panel grows with its content, then the body scrolls
        float bodyTop = buttonsTop + _buttonHeight + 8f;
        float height = Mathf.Min(bodyTop + bodyHeight + _padding, _maxHeight);
        _rect.sizeDelta = new Vector2(_width, height);
        _viewport.offsetMin = new Vector2(_padding, _padding);
        _viewport.offsetMax = new Vector2(-_padding, -bodyTop);
    }

    string BuildTitle()
    {
        string side = _entity.entityType == Entity.EntityType.Player ? "Ally" : "Enemy";
        string state = _entity.isDraggable ? "placement" : "in battle";
        string title = $"<size={_fontSize + 6f}><b>{_entity.data.title}</b></size>\n<color={EntityInfoFormatter.MutedColor}>{side} · {state}</color>";
        if (!string.IsNullOrEmpty(_entity.data.description))
        {
            title += $"\n<i>{_entity.data.description}</i>";
        }
        return title;
    }

    string BuildBody()
    {
        StringBuilder builder = new StringBuilder();
        GameObject owner = _entity.gameObject;

        EntityInfoFormatter.AppendSection(builder, "Health", GetHealthLines());
        EntityInfoFormatter.AppendSection(builder, "Targeting", GetTargetingLines());
        EntityInfoFormatter.AppendSection(builder, "Stats", EntityInfoFormatter.GetAttributeLines(_entity.attributeManager, owner));
        EntityInfoFormatter.AppendSection(builder, "Skills", GetSkillLines());
        EntityInfoFormatter.AppendSection(builder, "Items", GetItemLines());
        EntityInfoFormatter.AppendSection(builder, "Active effects", EntityInfoFormatter.GetBuffLines(_entity.buffManager, owner));
        EntityInfoFormatter.AppendSection(builder, "On hit", GetOnHitLines());
        return builder.ToString().TrimEnd();
    }

    List<string> GetHealthLines()
    {
        ResourceAttribute health = _entity.health;
        List<string> lines = new List<string>
        {
            $"<b>{EntityInfoFormatter.FormatNumber(health.Value)} / {EntityInfoFormatter.FormatNumber(health.Max)}</b> <color={EntityInfoFormatter.MutedColor}>({Mathf.RoundToInt(health.percent * 100f)} %)</color>",
        };
        if (health.preventConsumers)
        {
            lines.Add($"<color={EntityInfoFormatter.BonusColor}>Invulnerable</color>");
        }
        return lines;
    }

    List<string> GetTargetingLines()
    {
        TargetProvider targetProvider = _entity.targetProvider;
        List<string> lines = new List<string>
        {
            $"{targetProvider.targetBehaviourType} <color={EntityInfoFormatter.MutedColor}>· {targetProvider.targetCount} target(s)</color>",
        };

        foreach (ATargetValidatorFactory validator in _entity.data.targetValidators)
        {
            if (validator != null)
            {
                lines.Add($"<color={EntityInfoFormatter.MutedColor}>    condition: {EntityInfoFormatter.Prettify(validator.GetType().Name, "Factory", "Validator")}</color>");
            }
        }

        List<string> targets = new List<string>();
        List<GameObject> currentTargets = targetProvider.GetTargets();
        if (currentTargets != null)
        {
            foreach (GameObject target in currentTargets)
            {
                if (target != null)
                {
                    targets.Add(EntityInfoFormatter.GetSourceName(target, _entity.gameObject));
                }
            }
        }
        if (targets.Count > 0)
        {
            lines.Add($"Aiming at: {string.Join(", ", targets)}");
        }
        return lines;
    }

    // Skills of the entity and the ones given by its items
    List<string> GetSkillLines()
    {
        List<string> lines = new List<string>();
        foreach (ASkill skill in _entity.GetComponents<ASkill>())
        {
            string line = EntityInfoFormatter.FormatSkill(skill);
            if (!_entity.skills.Contains(skill))
            {
                line += $" <color={EntityInfoFormatter.MutedColor}>(item)</color>";
            }
            lines.Add(line);
        }
        return lines;
    }

    List<string> GetItemLines()
    {
        List<string> lines = new List<string>();
        foreach (AItem item in _entity.items)
        {
            lines.Add(EntityInfoFormatter.FormatItem(item, true));
        }
        foreach (InventoryItemData itemData in _entity.inventoryHandler.items)
        {
            lines.Add(EntityInfoFormatter.FormatItem(itemData.item, false));
        }
        return lines;
    }

    List<string> GetOnHitLines()
    {
        List<string> lines = new List<string>();
        foreach (ABuffHandlerFactory onHitEffect in _entity.GetOnHitEffects())
        {
            lines.Add($"Applies: {EntityInfoFormatter.GetBuffName(onHitEffect)}");
        }
        foreach (AConsumerFactory consumer in _entity.GetOnHitConsumers())
        {
            lines.Add($"Consumer: {EntityInfoFormatter.Prettify(consumer.GetType().Name, "Factory", "Consumer")}");
        }
        foreach (ABuffHandlerFactory projectileBehaviour in _entity.projectileBehaviours)
        {
            lines.Add($"Projectile: {EntityInfoFormatter.GetBuffName(projectileBehaviour)}");
        }
        // An item in the first slots is equipped several times, so its effects are added several times
        return EntityInfoFormatter.GroupDuplicates(lines);
    }

    #endregion
}
