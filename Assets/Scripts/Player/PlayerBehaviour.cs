using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerBehaviour : Singleton<PlayerBehaviour>
{
    [SerializeField] List<InputActionReference> _skillInputs = new List<InputActionReference>();

    public UnityEvent<int> OnGoldChanged = new UnityEvent<int>();
    public UnityEvent<Character> OnCharacterInit = new UnityEvent<Character>();

    [SerializeField] Character _character;
    public Character character { get { return _character; } set { _character = value; } }

    [SerializeField] GridManager _grid;
    public GridManager grid { get { return _grid; } set { _grid = value; } }

    [SerializeField] GridGenerator _gridGenerator;
    public GridGenerator gridGenerator { get { return _gridGenerator; } set { _gridGenerator = value; } }

    int _gold;
    public int gold
    {
        get { return _gold; } 
        set
        {
            _gold = value;
            OnGoldChanged.Invoke(_gold);
        }
    }

    void Start()
    {
        _grid.Generate();
        _skillInputs.ForEach(input => input.asset.Enable());
    }

    public void Init(CharacterData characterData)
    {
        gold = DataManager.instance.data.gold;

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

    public bool HasEnoughGold(int value)
    {
        return _gold >= value;
    }
}