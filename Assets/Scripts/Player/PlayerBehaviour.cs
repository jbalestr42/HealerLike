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

    public void Init()
    {
        gold = DataManager.instance.data.gold;

        _character.data = DataManager.instance.GetRandomCharacter();
        _character.Init();

        for (int i = 0; i < character.skillSlots.Count; i++)
        {
            if (_skillInputs.Count >= character.skillSlots.Count)
            {
                var skillSlot = character.skillSlots[i];
                var input = _skillInputs[i];

                input.action.started += (InputAction.CallbackContext ctx) => skillSlot.UseSkill();
            }
            else
            {
                Debug.LogError("Not Enough inputs");
                break;
            }
        }

        OnCharacterInit.Invoke(_character);
    }

    public bool HasEnoughGold(int value)
    {
        return _gold >= value;
    }
}