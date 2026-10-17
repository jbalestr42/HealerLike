using System.Collections.Generic;
using UnityEngine;

// How the health percent every unit ends up at is computed
public enum BalanceLifeMode
{
    // The average of the health percents, whatever the max health: 20/200 and 80/100 end up at 45%. Can
    // create or remove health
    Relative,
    // The total health over the total max health: 20/200 and 80/100 end up at 33%. Keeps the total health
    Absolute,
}

// Sets units to the same health percent (e.g. the Balance Life spell of the Druid, the Balance of the Arbiter)
public static class HealthBalance
{
    // Every living unit ends up at the same health percent, computed as the mode says, the dead ones left out
    public static void Balance(List<GameObject> units, BalanceLifeMode mode)
    {
        List<ResourceAttribute> healths = new List<ResourceAttribute>();
        foreach (GameObject unit in units)
        {
            Entity entity = unit != null ? unit.GetComponent<Entity>() : null;
            if (entity != null)
            {
                healths.Add(entity.health);
            }
        }
        Balance(healths, mode);
    }

    // Every living unit ends up at the same health percent, computed as the mode says. The health is set
    // directly, so armor, invincibility, shields, buffs and debuffs don't change it
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
