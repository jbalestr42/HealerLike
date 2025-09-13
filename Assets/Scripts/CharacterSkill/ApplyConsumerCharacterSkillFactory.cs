using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/ApplyConsumerCharacterSkill")]
public class ApplyConsumerCharacterSkillFactory : CharacterSkillFactory<ApplyConsumerCharacterSkill, ApplyConsumerCharacterSkillData> {}

[Serializable]
public class ApplyConsumerCharacterSkillData : BaseCharacterSkillData
{
    [CreateDataButton]
    public AConsumerFactory consumer;
    public float multiplier = 1f;
}

public class ApplyConsumerCharacterSkill : BaseCharacterSkill<ApplyConsumerCharacterSkillData>
{
    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        ResourceModifier resourceModifier = new ResourceModifier();
        resourceModifier.consumers.Add(data.consumer.GetConsumer(source, target));
        resourceModifier.multiplier = data.multiplier;
        resourceModifier.source = source;

        target.GetComponent<Entity>().health.AddResourceModifier(resourceModifier);
    }
}