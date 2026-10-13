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
    [SerializeField] UnityEngine.UI.Toggle _markEntityToggle;
    public UnityEngine.UI.Toggle markEntityToggle { get { return _markEntityToggle; } }
    [SerializeField] ResourceView _manaBar;

    // Optional, the ascension level of the run and its maluses
    [SerializeField] AscensionIndicator _ascensionIndicator;
    public AscensionIndicator ascensionIndicator { get { return _ascensionIndicator; } }

    // Optional, the text of the tooltip shown when hovering the mana bar (none without it), read on each hover since
    // the max mana and the refill change with the items
    public Func<string> manaTooltip;

    bool _isManaTooltipShown;

    void Start()
    {
        PlayerBehaviour.instance.OnCharacterInit.AddListener(OnCharacterInit);
        AddManaTooltip();
    }

    void AddManaTooltip()
    {
        HoverTooltipTrigger trigger = _manaBar.gameObject.AddComponent<HoverTooltipTrigger>();
        trigger.onEnter = ShowManaTooltip;
        trigger.onExit = HideManaTooltip;
    }

    // The text of the tooltip on the mana bar, null when there is none
    public string GetManaTooltipText()
    {
        return manaTooltip?.Invoke();
    }

    void ShowManaTooltip(RectTransform target)
    {
        string text = GetManaTooltipText();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        toolTip.SetText(text);
        toolTip.ShowBelow(target);
        _isManaTooltipShown = true;
    }

    // Only the tooltip it showed: another element may be showing its own
    void HideManaTooltip()
    {
        if (!_isManaTooltipShown)
        {
            return;
        }

        _isManaTooltipShown = false;
        UIManager.instance.GetView<GameView>(ViewType.Game).toolTip.Show(false);
    }

#if UNITY_EDITOR
    // A button for testing in the editor only, never in a build: a copy of the next wave button, on its right
    public UnityEngine.UI.Button CreateEditorButton(string label)
    {
        UnityEngine.UI.Button button = Instantiate(_nextWaveButton, _nextWaveButton.transform.parent);
        button.name = $"{label} (Editor)";
        // The copy would also call the listeners set on the original in the prefab
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.interactable = true;
        RectTransform rect = (RectTransform)button.transform;
        rect.anchoredPosition += new Vector2(rect.sizeDelta.x + 10f, 0f);
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
    }

    void OnManaChanged(ResourceAttribute health)
    {
        _manaBar.SetResource(health.Value, health.Max);
    }
}
