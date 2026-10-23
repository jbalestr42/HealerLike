using System.Collections.Generic;
using UnityEngine;

public class CharacterSkillSlot : MonoBehaviour
{
    [SerializeField] CharacterSkillData _data;
    public CharacterSkillData data => _data;

    List<ACharacterSkillValidator> _validators = new List<ACharacterSkillValidator>();
    ACharacterSkill _skill;
    // Off outside of a battle (the preparation of a round included): the skill can't be used, whatever its validators
    bool _isEnabled = true;
    public bool isEnabled { get { return _isEnabled; } set { _isEnabled = value; } }

    // Its cost and cooldown, as its validators give them
    CharacterSkillState _state = new CharacterSkillState();
    public CharacterSkillState state => _state;

    public void Init(ACharacterSkill skill, bool useValidators = true)
    {
        _skill = skill;
        _data = _skill.GetData();

        foreach (ACharacterSkillValidatorFactory validatorFactory in data.validators)
        {
            ACharacterSkillValidator validator = validatorFactory.Create();
            // Without restriction (the sandbox): neither cost nor cooldown, but the gains as in a run
            if (!useValidators && !validator.IsGain(gameObject))
            {
                continue;
            }
            validator.Init(_state, gameObject);
            _validators.Add(validator);
        }
    }

    void Update()
    {
        foreach (ACharacterSkillValidator validator in _validators)
        {
            validator.Update(_state);
        }
    }

    // Enabled, and every validator allows it (cost, cooldown, ...)
    public bool CanUseSkill()
    {
        if (!_isEnabled)
        {
            return false;
        }
        foreach (ACharacterSkillValidator validator in _validators)
        {
            if (!validator.IsValid(gameObject))
            {
                return false;
            }
        }
        return true;
    }

    public void UseSkill()
    {
        if (CanUseSkill())
        {
            _skill.Use(gameObject, (bool isDone) =>
            {
                if (isDone)
                {
                    foreach (ACharacterSkillValidator validator in _validators)
                    {
                        validator.OnSkillUsed(gameObject);
                    }
                    Character character = GetComponent<Character>();
                    if (character != null)
                    {
                        character.OnSkillUsed.Invoke(_data);
                    }
                }
            });
        }
    }
}
