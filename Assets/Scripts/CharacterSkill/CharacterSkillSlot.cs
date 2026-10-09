
using System.Collections.Generic;
using UnityEngine;

public class CharacterSkillSlot : MonoBehaviour
{
    [SerializeField] CharacterSkillData _data;
    public CharacterSkillData data => _data;

    List<ACharacterSkillValidator> _validators = new List<ACharacterSkillValidator>();
    ACharacterSkill _skill;
    UseCharacterSkillButton _skillButton;

    public void Init(ACharacterSkill skill, UseCharacterSkillButton skillButton, bool useValidators = true)
    {
        _skill = skill;
        _data = _skill.GetData();
        _skillButton = skillButton;
        _skillButton.data = _data;

        _skillButton.button.onClick.AddListener(UseSkill);

        if (useValidators)
        {
            foreach (ACharacterSkillValidatorFactory validatorFactory in data.validators)
            {
                ACharacterSkillValidator validator = validatorFactory.Create();
                validator.Init(_skillButton, gameObject);
                _validators.Add(validator);
            }
        }

        _skillButton.SetName(_data.name);
    }

    void Update()
    {
        foreach (ACharacterSkillValidator validator in _validators)
        {
            validator.Update(_skillButton);
        }
        _skillButton.Enable(CanUseSkill());
    }

    // Every validator allows it (cost, cooldown, ...)
    public bool CanUseSkill()
    {
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
                }
            });
        }
    }
}