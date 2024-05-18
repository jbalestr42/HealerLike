using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/HealCharacterSkill")]
public class HealCharacterSkillFactory : CharacterSkillFactory<HealCharacterSkill, HealCharacterSkillData> {}

[Serializable]
public class HealCharacterSkillData
{
    [CreateDataButton]
    public AConsumerFactory consumer;
}

public class HealCharacterSkill : ACharacterSkill<HealCharacterSkillData>
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        UnityAction<GameObject> onTargetSelected = (GameObject target) => 
        {
            ResourceModifier resourceModifier = new ResourceModifier();
            resourceModifier.consumers.Add(data.consumer.GetConsumer(source, target));
            resourceModifier.multiplier = -1f;
            resourceModifier.source = source;

            target.GetComponent<Entity>().health.AddResourceModifier(resourceModifier);
        };

        InteractionManager.instance.SetInteraction(new SingleTargetInteraction(onTargetSelected, onSkillComplete));
    }
}