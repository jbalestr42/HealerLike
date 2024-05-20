using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/HealCharacterSkill")]
public class HealCharacterSkillFactory : CharacterSkillFactory<HealCharacterSkill, HealCharacterSkillData> {}

[Serializable]
public class HealCharacterSkillData : BaseCharacterSkillData
{
    [CreateDataButton]
    public AConsumerFactory consumer;
    public float multiplier = 1f;
}

public class HealCharacterSkill : BaseCharacterSkill<HealCharacterSkillData>
{
    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        ResourceModifier resourceModifier = new ResourceModifier();
        resourceModifier.consumers.Add(data.consumer.GetConsumer(source, target));
        resourceModifier.multiplier = -1f * data.multiplier;
        resourceModifier.source = source;

        target.GetComponent<Entity>().health.AddResourceModifier(resourceModifier);
    }
}