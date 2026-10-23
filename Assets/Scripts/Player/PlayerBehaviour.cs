using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerBehaviour : Singleton<PlayerBehaviour>
{
    [SerializeField] List<InputActionReference> _skillInputs = new List<InputActionReference>();

    public UnityEvent<Character> OnCharacterInit = new UnityEvent<Character>();

    [SerializeField] Character _character;
    public Character character { get { return _character; } set { _character = value; } }

    // The items the player keeps to give to the units
    InventoryHandler _stash = new InventoryHandler();
    public InventoryHandler stash => _stash;

    // The units the player can place on the board
    UnitRoster _roster = new UnitRoster();
    public UnitRoster roster => _roster;

    void Start()
    {
        _skillInputs.ForEach(input => input.asset.Enable());
    }

    public void Init(CharacterData characterData)
    {
        _character.data = characterData;
        _character.Init();

        // Skills without an input are still usable with their button
        if (_skillInputs.Count < character.skillSlots.Count)
        {
            Debug.LogWarning($"Not enough inputs: only the first {_skillInputs.Count} of {character.skillSlots.Count} skills have a shortcut");
        }
        for (int i = 0; i < Mathf.Min(_skillInputs.Count, character.skillSlots.Count); i++)
        {
            var skillSlot = character.skillSlots[i];
            var input = _skillInputs[i];

            input.action.started += (InputAction.CallbackContext ctx) => skillSlot.UseSkill();
        }

        OnCharacterInit.Invoke(_character);
    }

    // The items added to the unit (not its innate ones) come back to the stash
    public void TakeItemsOf(Entity entity)
    {
        _stash.TakeAllItems(entity.inventoryHandler);
    }
}