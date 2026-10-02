using UnityEngine;

public class SandboxPanel : MonoBehaviour
{
    [SerializeField] SandboxButton _buttonPrefab;
    [SerializeField] Transform _controlContainer;
    [SerializeField] Transform _entityContainer;
    [SerializeField] Transform _skillContainer;
    [SerializeField] Transform _itemContainer;
    [SerializeField] Transform _playerItemContainer;
    [SerializeField] GameObject _wavePanel;
    [SerializeField] Transform _waveContainer;
    [SerializeField] EntityInfoPanel _entityInfoPanel;

    SandboxGameType _gameType;
    SandboxButton _sideButton;
    SandboxButton _startBattleButton;
    SandboxButton _stopBattleButton;
    SandboxButton _pauseButton;
    SandboxButton _speedButton;
    readonly SandboxTimeControl _timeControl = new SandboxTimeControl();

    public void Init(SandboxGameType gameType, SandboxData data, Character character)
    {
        _gameType = gameType;

        // Clicking an entity shows the detailed sandbox panel instead of the game one
        UIManager.instance.GetView<GameView>(ViewType.Game).panels[PanelType.Entity] = _entityInfoPanel;

        CharacterData played = _gameType.playedCharacter;
        CreateButton(_controlContainer, "Character: " + (played != null ? played.title : "All skills"), _gameType.PlayNextCharacter);
        _sideButton = CreateButton(_controlContainer, "", ToggleSide);
        CreateButton(_controlContainer, "Remove", _gameType.StartRemovingEntities);
        _startBattleButton = CreateButton(_controlContainer, "Start battle", StartBattle);
        _stopBattleButton = CreateButton(_controlContainer, "Stop battle", StopBattle);
        _pauseButton = CreateButton(_controlContainer, "", TogglePause);
        _speedButton = CreateButton(_controlContainer, "", NextSpeed);
        Time.timeScale = _timeControl.timeScale;
        CreateButton(_controlContainer, "Heal all", _gameType.RestoreAll);
        CreateButton(_controlContainer, "Clear all", _gameType.ClearAll);
        CreateButton(_controlContainer, "Load a wave", () => _wavePanel.SetActive(true));

        foreach (EntityData entityData in data.entities)
        {
            CreateButton(_entityContainer, entityData.title, () => _gameType.SelectEntity(entityData));
        }

        InitWavePanel(data);

        foreach (CharacterSkillSlot skillSlot in character.skillSlots)
        {
            CreateButton(_skillContainer, skillSlot.data.name, skillSlot.UseSkill);
        }

        foreach (AItemFactory itemFactory in data.items)
        {
            Transform container = IsPlayerItem(itemFactory) ? _playerItemContainer : _itemContainer;
            CreateButton(container, itemFactory.title, () => _gameType.GiveItem(itemFactory));
        }

        Refresh();
    }

    // Player items are equipped on the character, the other ones are given to a unit
    public static bool IsPlayerItem(AItemFactory itemFactory)
    {
        return itemFactory.HasTag(TagNames.Player);
    }

    SandboxButton CreateButton(Transform container, string label, UnityEngine.Events.UnityAction onClick)
    {
        SandboxButton button = Instantiate(_buttonPrefab, container);
        button.Init(label, onClick);
        return button;
    }

    // One button per wave, the panel closes once a wave is loaded
    void InitWavePanel(SandboxData data)
    {
        foreach (WavePatternData waveData in data.waves)
        {
            if (waveData != null)
            {
                CreateButton(_waveContainer, waveData.name, () => LoadWave(waveData));
            }
        }
        CreateButton(_waveContainer, "Cancel", () => _wavePanel.SetActive(false));
        _wavePanel.SetActive(false);
    }

    void LoadWave(WavePatternData waveData)
    {
        _gameType.LoadWave(waveData);
        _wavePanel.SetActive(false);
    }

    void ToggleSide()
    {
        _gameType.placementSide = _gameType.placementSide == Entity.EntityType.Player ? Entity.EntityType.Computer : Entity.EntityType.Player;
        // The side is read when an entity is selected, so cancel the current placement
        InteractionManager.instance.CancelInteraction();
        Refresh();
    }

    void StartBattle()
    {
        _gameType.StartBattle();
        Refresh();
    }

    void StopBattle()
    {
        _gameType.StopBattle();
        Refresh();
    }

    void TogglePause()
    {
        _timeControl.TogglePause();
        Time.timeScale = _timeControl.timeScale;
        Refresh();
    }

    void NextSpeed()
    {
        _timeControl.NextSpeed();
        Time.timeScale = _timeControl.timeScale;
        Refresh();
    }

    void OnDestroy()
    {
        // Time.timeScale outlives the scene: never leave the game paused or slowed down
        Time.timeScale = 1f;
    }

    void Refresh()
    {
        _pauseButton.SetLabel(_timeControl.pauseLabel);
        _speedButton.SetLabel(_timeControl.speedLabel);
        _sideButton.SetLabel(_gameType.placementSide == Entity.EntityType.Player ? "Side: Ally" : "Side: Enemy");
        _startBattleButton.SetInteractable(!_gameType.isBattleRunning);
        _stopBattleButton.SetInteractable(_gameType.isBattleRunning);
    }
}
