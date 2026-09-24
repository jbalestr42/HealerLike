using UnityEngine;

public class SandboxPanel : MonoBehaviour
{
    [SerializeField] SandboxButton _buttonPrefab;
    [SerializeField] Transform _controlContainer;
    [SerializeField] Transform _entityContainer;
    [SerializeField] Transform _skillContainer;
    [SerializeField] Transform _itemContainer;

    SandboxGameType _gameType;
    SandboxButton _sideButton;
    SandboxButton _startBattleButton;
    SandboxButton _stopBattleButton;

    public void Init(SandboxGameType gameType, SandboxData data, Character character)
    {
        _gameType = gameType;

        _sideButton = CreateButton(_controlContainer, "", ToggleSide);
        CreateButton(_controlContainer, "Supprimer", _gameType.StartRemovingEntities);
        _startBattleButton = CreateButton(_controlContainer, "Lancer le combat", StartBattle);
        _stopBattleButton = CreateButton(_controlContainer, "Arrêter le combat", StopBattle);
        CreateButton(_controlContainer, "Soigner tout", _gameType.RestoreAll);
        CreateButton(_controlContainer, "Tout effacer", _gameType.ClearAll);

        foreach (EntityData entityData in data.entities)
        {
            CreateButton(_entityContainer, entityData.title, () => _gameType.SelectEntity(entityData));
        }

        foreach (CharacterSkillSlot skillSlot in character.skillSlots)
        {
            CreateButton(_skillContainer, skillSlot.data.name, skillSlot.UseSkill);
        }

        foreach (AItemFactory itemFactory in data.items)
        {
            CreateButton(_itemContainer, itemFactory.title, () => _gameType.GiveItem(itemFactory));
        }

        Refresh();
    }

    SandboxButton CreateButton(Transform container, string label, UnityEngine.Events.UnityAction onClick)
    {
        SandboxButton button = Instantiate(_buttonPrefab, container);
        button.Init(label, onClick);
        return button;
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

    void Refresh()
    {
        _sideButton.SetLabel(_gameType.placementSide == Entity.EntityType.Player ? "Camp : Allié" : "Camp : Ennemi");
        _startBattleButton.SetInteractable(!_gameType.isBattleRunning);
        _stopBattleButton.SetInteractable(_gameType.isBattleRunning);
    }
}
