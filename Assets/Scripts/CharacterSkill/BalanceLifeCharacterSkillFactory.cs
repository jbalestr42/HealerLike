using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/BalanceLifeCharacterSkill")]
public class BalanceLifeCharacterSkillFactory : CharacterSkillFactory<BalanceLifeCharacterSkill, BalanceLifeCharacterSkillData> {}

[Serializable]
public class BalanceLifeCharacterSkillData : CharacterSkillData
{
    public Entity.EntityType entityType = Entity.EntityType.Player;
}

// Shares the health of the team: every unit ends up at the same health percent
public class BalanceLifeCharacterSkill : ACharacterSkill<BalanceLifeCharacterSkillData>
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        List<ResourceAttribute> healths = new List<ResourceAttribute>();
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            healths.Add(entity.GetComponent<Entity>().health);
        }
        Balance(healths);
        onSkillComplete(true);
    }

    // The total health of the living units is kept and spread by max health. The health is set
    // directly, so armor, invincibility, buffs and debuffs don't change it
    public static void Balance(List<ResourceAttribute> healths)
    {
        List<ResourceAttribute> living = healths.FindAll(health => health != null && health.Value > 0f);

        float total = 0f;
        float totalMax = 0f;
        foreach (ResourceAttribute health in living)
        {
            total += health.Value;
            totalMax += health.Max;
        }
        if (totalMax <= 0f)
        {
            return;
        }

        float percent = total / totalMax;
        foreach (ResourceAttribute health in living)
        {
            health.SetValue(percent * health.Max);
        }
    }
}
