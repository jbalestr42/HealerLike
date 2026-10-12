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


    void Start()
    {
        PlayerBehaviour.instance.OnCharacterInit.AddListener(OnCharacterInit);
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
