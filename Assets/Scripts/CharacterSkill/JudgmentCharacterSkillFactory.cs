using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/JudgmentCharacterSkill")]
public class JudgmentCharacterSkillFactory : CharacterSkillFactory<JudgmentCharacterSkill, JudgmentCharacterSkillData> {}

[Serializable]
public class JudgmentCharacterSkillData : ApplyConsumerCharacterSkillData
{
    // Spent all at once when the skill is used
    public ResourceType spentResource = ResourceType.Devotion;
    // Added to the multiplier for each point spent (e.g. +0.05 per Devotion)
    public float multiplierPerSpent;
}

// Judgment of the Cleric: spends all the Devotion of the character, then heals every ally, more for each Devotion spent
public class JudgmentCharacterSkill : BaseCharacterSkill<JudgmentCharacterSkillData>
{
    // Read once when the skill is used, for every target: the first one would otherwise empty it for the others
    int _spent;

    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        Spend(source);
        base.Use(source, onSkillComplete);
    }

    public int Spend(GameObject source)
    {
        ResourceAttribute resource = ResourceAttribute.Get(source, data.spentResource);
        _spent = resource != null ? Mathf.RoundToInt(resource.SpendAll(source)) : 0;
        return _spent;
    }

    // With the bonus of the resource spent by the last use
    public float GetMultiplier()
    {
        return data.multiplier + data.multiplierPerSpent * _spent;
    }

    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        target.GetComponent<Entity>().health.AddResourceModifier(ResourceModifier.Create(data.consumer, source, target, GetMultiplier()));
    }
}
