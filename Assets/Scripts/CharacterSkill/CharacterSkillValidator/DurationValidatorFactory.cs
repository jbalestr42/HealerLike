using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkillValidators/DurationValidator")]
public class DurationValidatorFactory : CharacterSkillValidatorFactory<DurationValidator, DurationValidatorData> {}

[Serializable]
public class DurationValidatorData
{
    public float duration;
}

public class DurationValidator : ACharacterSkillValidator<DurationValidatorData>
{
    float _startTimer = 0f;

    public override void Init(UseCharacterSkillButton skillButton, GameObject owner)
    {
        _startTimer = Time.realtimeSinceStartup - data.duration;
        skillButton.hasCooldown = true;
    }

    public override void Update(UseCharacterSkillButton skillButton)
    {
        skillButton.SetCooldown(Mathf.Max(data.duration - (Time.realtimeSinceStartup - _startTimer), 0f), data.duration);
    }

    public override bool IsValid(GameObject owner)
    {
        return Time.realtimeSinceStartup >= (_startTimer + data.duration);
    }

    public override void OnSkillUsed(GameObject owner)
    {
        _startTimer = Time.realtimeSinceStartup;
    }
}