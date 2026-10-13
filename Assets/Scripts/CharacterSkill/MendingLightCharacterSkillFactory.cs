using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/MendingLightCharacterSkill")]
public class MendingLightCharacterSkillFactory : CharacterSkillFactory<MendingLightCharacterSkill, MendingLightCharacterSkillData> {}

[Serializable]
public class MendingLightCharacterSkillData : BaseCharacterSkillData
{
    // The heal of each unit
    [CreateDataButton]
    public AConsumerFactory consumer;
    public float multiplier = 1f;
    // Allies healed after the target, each time the most wounded one not healed yet
    [Min(0)]
    public int jumps = 3;
    // Given to the character for each unit healed (e.g. 1 Devotion), on its resource of that type
    public ResourceType gainedResource = ResourceType.Devotion;
    [CreateDataButton]
    public AConsumerFactory gainPerHeal;
}

// Heals the target, then jumps right away to the most wounded ally not healed yet, several times. Each unit healed
// counts as a target of the skill (e.g. blessed) and gives the character a resource (e.g. Devotion)
public class MendingLightCharacterSkill : BaseCharacterSkill<MendingLightCharacterSkillData>
{
    // Always on a single target picked by the player, the light jumping from it
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        InteractionManager.instance.SetInteraction(new SingleTargetInteraction((GameObject target) => Mend(source, target), onSkillComplete, data.entityType));
    }

    // The target healed, then each ally the light jumps to
    public void Mend(GameObject source, GameObject target)
    {
        ApplyOnTarget(source, target);
        foreach (Entity jump in GetJumps(target, GetAllies(), data.jumps))
        {
            ApplyOnTarget(source, jump.gameObject);
        }
    }

    protected virtual List<GameObject> GetAllies()
    {
        return EntityManager.instance.GetEntities(data.entityType);
    }

    // The allies the light jumps to after the target, in order: each time the living ally missing the largest part
    // of its health, not healed yet. Fewer when no wounded ally is left
    public static List<Entity> GetJumps(GameObject target, List<GameObject> allies, int jumps)
    {
        List<Entity> result = new List<Entity>();
        List<GameObject> left = allies.FindAll(ally => ally != null && ally != target);
        while (result.Count < jumps)
        {
            Entity mostWounded = LifeSteal.FindMostWounded(left);
            if (mostWounded == null || mostWounded.health.percent >= 1f)
            {
                break;
            }

            result.Add(mostWounded);
            left.Remove(mostWounded.gameObject);
        }
        return result;
    }

    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        target.GetComponent<Entity>().health.AddResourceModifier(ResourceModifier.Create(data.consumer, source, target, data.multiplier));

        ResourceAttribute gained = ResourceAttribute.Get(source, data.gainedResource);
        if (gained != null && data.gainPerHeal != null)
        {
            gained.AddResourceModifier(ResourceModifier.Create(data.gainPerHeal, source, source));
        }
    }
}
