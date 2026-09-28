using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/DrainLifeCharacterSkill")]
public class DrainLifeCharacterSkillFactory : CharacterSkillFactory<DrainLifeCharacterSkill, DrainLifeCharacterSkillData> {}

[Serializable]
public class DrainLifeCharacterSkillData : BaseCharacterSkillData
{
    [CreateDataButton]
    public AConsumerFactory consumer;
    public float multiplier = 1f;
    // Part of the damage actually dealt healed on the most wounded ally
    public float healRatio = 1f;
    public Entity.EntityType allyType = Entity.EntityType.Player;
}

// Damages an enemy, then heals the most wounded ally by the damage it actually took (after its armor)
public class DrainLifeCharacterSkill : BaseCharacterSkill<DrainLifeCharacterSkillData>
{
    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        EntityManager entityManager = EntityManager.instance;
        Drain(source, target.GetComponent<Entity>().health, ResourceModifier.Create(data.consumer, source, target, data.multiplier), entityManager.GetEntities(data.allyType), data.healRatio);
    }

    public static void Drain(GameObject source, ResourceAttribute targetHealth, ResourceModifier drain, List<GameObject> allies, float healRatio)
    {
        void OnProcessed(GameObject target, ResourceModifier resourceModifier, float value, bool isCritical)
        {
            if (resourceModifier != drain)
            {
                return;
            }
            targetHealth.OnAllConsumerProcessed.RemoveListener(OnProcessed);
            // Damage is negative
            LifeSteal.HealMostWounded(source, allies, -value * healRatio);
        }

        targetHealth.OnAllConsumerProcessed.AddListener(OnProcessed);
        targetHealth.AddResourceModifier(drain);
    }
}
