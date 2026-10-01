using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/BalanceLifeCharacterSkill")]
public class BalanceLifeCharacterSkillFactory : CharacterSkillFactory<BalanceLifeCharacterSkill, BalanceLifeCharacterSkillData> {}

// How the health percent every unit ends up at is computed
public enum BalanceLifeMode
{
    // The average of the health percents, whatever the max health: 20/200 and 80/100 end up at 45%. Can
    // create or remove health
    Relative,
    // The total health over the total max health: 20/200 and 80/100 end up at 33%. Keeps the total health
    Absolute,
}

[Serializable]
public class BalanceLifeCharacterSkillData : CharacterSkillData
{
    public Entity.EntityType entityType = Entity.EntityType.Player;
    public BalanceLifeMode mode = BalanceLifeMode.Relative;
}

// Shares the health of the team: every unit ends up at the average health percent of the team
public class BalanceLifeCharacterSkill : ACharacterSkill<BalanceLifeCharacterSkillData>
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        List<ResourceAttribute> healths = new List<ResourceAttribute>();
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            healths.Add(entity.GetComponent<Entity>().health);
        }
        Balance(healths, data.mode);
        onSkillComplete(true);
    }

    // Every living unit ends up at the same health percent, computed as the mode says. The health is set
    // directly, so armor, invincibility, buffs and debuffs don't change it
    public static void Balance(List<ResourceAttribute> healths, BalanceLifeMode mode)
    {
        List<ResourceAttribute> living = healths.FindAll(health => health != null && health.Value > 0f && health.Max > 0f);
        if (living.Count == 0)
        {
            return;
        }

        float percent = mode == BalanceLifeMode.Relative ? GetAveragePercent(living) : GetTotalPercent(living);
        foreach (ResourceAttribute health in living)
        {
            health.SetValue(percent * health.Max);
        }
    }

    static float GetAveragePercent(List<ResourceAttribute> healths)
    {
        float totalPercent = 0f;
        foreach (ResourceAttribute health in healths)
        {
            totalPercent += health.percent;
        }
        return totalPercent / healths.Count;
    }

    static float GetTotalPercent(List<ResourceAttribute> healths)
    {
        float total = 0f;
        float totalMax = 0f;
        foreach (ResourceAttribute health in healths)
        {
            total += health.Value;
            totalMax += health.Max;
        }
        return total / totalMax;
    }
}
