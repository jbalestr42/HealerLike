using System;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Button _inventoryButton;
    public UnityEngine.UI.Button inventoryButton { get { return _inventoryButton; } }

    [SerializeField] UnityEngine.UI.Button _nextWaveButton;
    public UnityEngine.UI.Button nextWaveButton { get { return _nextWaveButton; } }

    // Optional, opens the run map while arranging the units
    [SerializeField] UnityEngine.UI.Button _mapButton;
    public UnityEngine.UI.Button mapButton { get { return _mapButton; } }

    [SerializeField] UnityEngine.UI.Button _playSpeedx0Button;
    public UnityEngine.UI.Button playSpeedx0Button { get { return _playSpeedx0Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx05Button;
    public UnityEngine.UI.Button playSpeedx05Button { get { return _playSpeedx05Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx1Button;
    public UnityEngine.UI.Button playSpeedx1Button { get { return _playSpeedx1Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx2Button;
    public UnityEngine.UI.Button playSpeedx2Button { get { return _playSpeedx2Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx3Button;
    public UnityEngine.UI.Button playSpeedx3Button { get { return _playSpeedx3Button; } }
    [SerializeField] ResourceView _manaBar;
    // Optional, the Devotion of the character, hidden when it builds none
    [SerializeField] DevotionView _devotionBar;

    // Optional, the ascension level of the run and its maluses
    [SerializeField] AscensionIndicator _ascensionIndicator;
    public AscensionIndicator ascensionIndicator { get { return _ascensionIndicator; } }

    // Optional, the text of the tooltip shown when hovering the mana bar (none without it), read on each hover since
    // the max mana and the refill change with the items
    public Func<string> manaTooltip;

#if UNITY_EDITOR
    // Editor buttons created, each one on the right of the previous one
    int _editorButtonCount = 0;
#endif

    // The Devotion of the character shown, null when it builds none
    ResourceAttribute _devotion;
    bool _isTooltipShown;

    void Start()
    {
        PlayerBehaviour.instance.OnCharacterInit.AddListener(OnCharacterInit);
        AddTooltips();
    }

    void AddTooltips()
    {
        AddTooltip(_manaBar.gameObject, GetManaTooltipText);
        if (_devotionBar != null)
        {
            AddTooltip(_devotionBar.gameObject, GetDevotionTooltipText);
        }
    }

    // The text read on each hover, no tooltip while it is empty
    void AddTooltip(GameObject target, Func<string> getText)
    {
        HoverTooltipTrigger trigger = target.AddComponent<HoverTooltipTrigger>();
        trigger.onEnter = rectTransform => ShowTooltip(getText(), rectTransform);
        trigger.onExit = HideTooltip;
    }

    // The text of the tooltip on the mana bar, null when there is none
    public string GetManaTooltipText()
    {
        return manaTooltip?.Invoke();
    }

    // The text of the tooltip on the Devotion, null without a character building it
    public string GetDevotionTooltipText()
    {
        return _devotion != null ? GetDevotionTooltip(_devotion.Value, _devotion.Max) : null;
    }

    public static string GetDevotionTooltip(float value, float max)
    {
        return $"<b>Devotion</b> {Mathf.RoundToInt(value)} / {Mathf.RoundToInt(max)}"
            + "\nSome of your spells give Devotion, others spend it all for a stronger effect."
            + "\nBack to 0 at the start of each fight.";
    }

    void ShowTooltip(string text, RectTransform target)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        toolTip.SetText(text);
        toolTip.ShowBelow(target);
        _isTooltipShown = true;
    }

    // Only the tooltip it showed: another element may be showing its own
    void HideTooltip()
    {
        if (!_isTooltipShown)
        {
            return;
        }

        _isTooltipShown = false;
        UIManager.instance.GetView<GameView>(ViewType.Game).toolTip.Show(false);
    }

#if UNITY_EDITOR
    // A button for testing in the editor only, never in a build: a copy of the next wave button, on its right after
    // the editor buttons already there
    public UnityEngine.UI.Button CreateEditorButton(string label)
    {
        _editorButtonCount++;
        UnityEngine.UI.Button button = Instantiate(_nextWaveButton, _nextWaveButton.transform.parent);
        button.name = $"{label} (Editor)";
        // The copy would also call the listeners set on the original in the prefab
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.interactable = true;
        RectTransform rect = (RectTransform)button.transform;
        rect.anchoredPosition += new Vector2((rect.sizeDelta.x + 10f) * _editorButtonCount, 0f);
        button.GetComponentInChildren<TMPro.TMP_Text>().text = label;
        return button;
    }
#endif

    public void ShowManaBar(bool show)
    {
        _manaBar.gameObject.SetActive(show);
    }

    void OnCharacterInit(Character character)
    {
        PlayerBehaviour.instance.character.mana.OnValueChanged.AddListener(OnManaChanged);
        OnManaChanged(PlayerBehaviour.instance.character.mana);
        ShowDevotion(character.devotion);
    }

    // Kept up to date while the character builds Devotion (not null), hidden otherwise
    public void ShowDevotion(ResourceAttribute devotion)
    {
        _devotion = devotion;
        if (_devotion != null)
        {
            _devotion.OnValueChanged.AddListener(OnDevotionChanged);
        }
        OnDevotionChanged(_devotion);
    }

    void OnDevotionChanged(ResourceAttribute devotion)
    {
        if (_devotionBar != null)
        {
            _devotionBar.SetDevotion(devotion != null ? Mathf.RoundToInt(devotion.Value) : 0, devotion != null ? Mathf.RoundToInt(devotion.Max) : 0);
        }
    }

    void OnManaChanged(ResourceAttribute health)
    {
        _manaBar.SetResource(health.Value, health.Max);
    }
}
