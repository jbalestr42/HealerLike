using System;
using System.Collections.Generic;
using UnityEngine;

// What the healer bot knows about a unit when deciding
public struct BotUnit
{
    public float health;
    public float maxHealth;
    public bool isTank;
    // Marked by a telegraphed strike landing in strikeIn seconds, taking strikeShare of it (1: the whole strike)
    public bool isMarked;
    public float strikeIn;
    public float strikeShare;

    public bool isAlive => health > 0f;
    public float healthPercent => maxHealth > 0f ? health / maxHealth : 0f;
}

// The decisions of the healer bot, without any scene object: the units are given by index
public static class HealerBotBrain
{
    // Decisions keep their rhythm whatever the frame length (an update of 0.17s at x10 must not turn a 0.25s
    // interval into 0.33s), without piling up the ones missed when the bot is far behind
    public static float GetNextDecisionTime(float previous, float time, float interval)
    {
        return Mathf.Max(previous + interval, time);
    }

    public static bool AreMet(IReadOnlyList<HealerBotCondition> conditions, IReadOnlyList<BotUnit> allies, float manaPercent)
    {
        foreach (HealerBotCondition condition in conditions)
        {
            if (!IsMet(condition, allies, manaPercent))
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsMet(HealerBotCondition condition, IReadOnlyList<BotUnit> allies, float manaPercent)
    {
        switch (condition.type)
        {
            case HealerBotConditionType.AlliesBelowHealth:
                return CountAlliesBelow(allies, condition.value) >= Mathf.Max(1, condition.count);
            case HealerBotConditionType.TeamHealthBelow:
                return HasLivingAlly(allies) && GetAverageHealth(allies) < condition.value;
            case HealerBotConditionType.TeamHealthAbove:
                return HasLivingAlly(allies) && GetAverageHealth(allies) > condition.value;
            case HealerBotConditionType.HealthSpreadAbove:
                return GetHealthSpread(allies) > condition.value;
            case HealerBotConditionType.ManaBelow:
                return manaPercent < condition.value;
            case HealerBotConditionType.StrikeWithin:
                return HasLiving(allies, null, unit => unit.isMarked && unit.strikeIn <= condition.value);
            default:
                return false;
        }
    }

    public static int CountAlliesBelow(IReadOnlyList<BotUnit> allies, float healthPercent)
    {
        int count = 0;
        foreach (BotUnit ally in allies)
        {
            if (ally.isAlive && ally.healthPercent < healthPercent)
            {
                count++;
            }
        }
        return count;
    }

    // Average of the health percents of the living allies
    public static float GetAverageHealth(IReadOnlyList<BotUnit> allies)
    {
        float total = 0f;
        int count = 0;
        foreach (BotUnit ally in allies)
        {
            if (ally.isAlive)
            {
                total += ally.healthPercent;
                count++;
            }
        }
        return count > 0 ? total / count : 0f;
    }

    // Most healthy minus least healthy living ally, 0 with less than two of them
    public static float GetHealthSpread(IReadOnlyList<BotUnit> allies)
    {
        float min = float.MaxValue;
        float max = float.MinValue;
        int count = 0;
        foreach (BotUnit ally in allies)
        {
            if (ally.isAlive)
            {
                min = Mathf.Min(min, ally.healthPercent);
                max = Mathf.Max(max, ally.healthPercent);
                count++;
            }
        }
        return count >= 2 ? max - min : 0f;
    }

    // Index of the unit picked among the living ones not excluded, -1 when there is none. LowestHealthAlly, Tank and
    // MarkedAlly look at allies, the enemy targets at enemies; ties go to the first unit
    public static int PickTarget(HealerBotTarget target, IReadOnlyList<BotUnit> allies, IReadOnlyList<BotUnit> enemies, Func<int, bool> isExcluded = null)
    {
        switch (target)
        {
            case HealerBotTarget.LowestHealthAlly:
                return PickBest(allies, isExcluded, unit => -unit.healthPercent);
            case HealerBotTarget.Tank:
                // Only the tanks when there is one, even excluded: an excluded tank means no target
                bool hasTank = HasLiving(allies, null, unit => unit.isTank);
                return PickBest(allies, i => (hasTank && !allies[i].isTank) || (isExcluded != null && isExcluded(i)), unit => unit.maxHealth);
            case HealerBotTarget.LowestHealthEnemy:
                return PickBest(enemies, isExcluded, unit => -unit.health);
            case HealerBotTarget.HighestHealthEnemy:
                return PickBest(enemies, isExcluded, unit => unit.health);
            case HealerBotTarget.MarkedAlly:
                return PickMarkedAlly(allies, isExcluded);
            default:
                return -1;
        }
    }

    // The marked ally struck first, the one taking the biggest part of the strike among the ones struck together
    static int PickMarkedAlly(IReadOnlyList<BotUnit> allies, Func<int, bool> isExcluded)
    {
        int best = -1;
        for (int i = 0; i < allies.Count; i++)
        {
            BotUnit ally = allies[i];
            if (!ally.isAlive || !ally.isMarked || (isExcluded != null && isExcluded(i)))
            {
                continue;
            }
            if (best < 0 || ally.strikeIn < allies[best].strikeIn || (Mathf.Approximately(ally.strikeIn, allies[best].strikeIn) && ally.strikeShare > allies[best].strikeShare))
            {
                best = i;
            }
        }
        return best;
    }

    static int PickBest(IReadOnlyList<BotUnit> units, Func<int, bool> isExcluded, Func<BotUnit, float> score)
    {
        int best = -1;
        for (int i = 0; i < units.Count; i++)
        {
            if (!units[i].isAlive || (isExcluded != null && isExcluded(i)))
            {
                continue;
            }
            if (best < 0 || score(units[i]) > score(units[best]))
            {
                best = i;
            }
        }
        return best;
    }

    static bool HasLiving(IReadOnlyList<BotUnit> units, Func<int, bool> isExcluded, Func<BotUnit, bool> predicate)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].isAlive && (isExcluded == null || !isExcluded(i)) && predicate(units[i]))
            {
                return true;
            }
        }
        return false;
    }

    static bool HasLivingAlly(IReadOnlyList<BotUnit> allies)
    {
        foreach (BotUnit ally in allies)
        {
            if (ally.isAlive)
            {
                return true;
            }
        }
        return false;
    }
}
