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
    // Cooldown of the last use, the SkillCooldownMultiplier of the owner applied to the data duration
    float _duration = 0f;

    public override void Init(UseCharacterSkillButton skillButton, GameObject owner)
    {
        _duration = GetDuration(owner);
        _startTimer = Time.realtimeSinceStartup - _duration;
        skillButton.hasCooldown = true;
    }

    public override void Update(UseCharacterSkillButton skillButton)
    {
        skillButton.SetCooldown(Mathf.Max(_duration - (Time.realtimeSinceStartup - _startTimer), 0f), _duration);
    }

    public override bool IsValid(GameObject owner)
    {
        return Time.realtimeSinceStartup >= (_startTimer + _duration);
    }

    public override void OnSkillUsed(GameObject owner)
    {
        _startTimer = Time.realtimeSinceStartup;
        _duration = GetDuration(owner);
    }

    public float GetDuration(GameObject owner)
    {
        return GetDuration(data.duration, owner);
    }

    // The duration with the SkillCooldownMultiplier of the owner applied, unchanged without one
    public static float GetDuration(float duration, GameObject owner)
    {
        AttributeManager attributes = owner != null ? owner.GetComponent<AttributeManager>() : null;
        if (attributes == null || !attributes.Has(AttributeType.SkillCooldownMultiplier))
        {
            return duration;
        }
        return duration * attributes.Get(AttributeType.SkillCooldownMultiplier).Value;
    }
}
