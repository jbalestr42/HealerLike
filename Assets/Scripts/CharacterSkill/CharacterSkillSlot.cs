
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CharacterSkillSlot : MonoBehaviour
{
    [InlineProperty]
    [SerializeField] CharacterSkillSlotData _data;
    public CharacterSkillSlotData data => _data;

    List<ACharacterSkillValidator> _validators = new List<ACharacterSkillValidator>();
    ACharacterSkill _skill;
    UseCharacterSkillButton _skillButton;

    public void Init(CharacterSkillSlotData data, UseCharacterSkillButton skillButton)
    {
        _data = data;
        _skillButton = skillButton;

        foreach (ACharacterSkillValidatorFactory validatorFactory in data.validators)
        {
            ACharacterSkillValidator validator = validatorFactory.Create();
            validator.Init(_skillButton, gameObject);
            _validators.Add(validator);
        }

        _skill = data.skill.Create();
        _skillButton.button.onClick.AddListener(UseSkill);

        _skillButton.SetName(_data.name);
    }

    void Update()
    {
        // TODO update button UI (cooldown + validity with mana cost etc.)
        foreach (ACharacterSkillValidator validator in _validators)
        {
            validator.Update(_skillButton);
        }
        _skillButton.Enable(CanUseSkill());
    }

    bool CanUseSkill()
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