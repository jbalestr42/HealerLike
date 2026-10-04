using System;
using System.Collections.Generic;
using UnityEngine;

// What a condition of a healer bot rule looks at. Health and mana values are fractions of the max (0.7 = 70%)
public enum HealerBotConditionType
{
    // At least count living allies below value
    AlliesBelowHealth,
    // Average health of the living allies below value
    TeamHealthBelow,
    // Average health of the living allies above value
    TeamHealthAbove,
    // Gap between the most and the least healthy living allies above value
    HealthSpreadAbove,
    // Mana of the character below value
    ManaBelow,
}

// Who the skill is cast on, for the skills asking for a target
public enum HealerBotTarget
{
    // The skill needs no target (every ally, every enemy, ...)
    None,
    // The living ally with the lowest health
    LowestHealthAlly,
    // The living ally tagged Tank with the most max health, or the one with the most max health
    Tank,
    LowestHealthEnemy,
    HighestHealthEnemy,
    // A free cell in front of the team (summons)
    FrontCell,
}

[Serializable]
public struct HealerBotCondition
{
    public HealerBotConditionType type;
    [Range(0f, 1f)] public float value;
    // Allies needed by AlliesBelowHealth
    [Min(1)] public int count;
}

// Cast the skill when every condition is met, on the target
[Serializable]
public class HealerBotRule
{
    public ACharacterSkillFactory skill;
    public List<HealerBotCondition> conditions = new List<HealerBotCondition>();
    public HealerBotTarget target;
    // Skips the targets already holding the buff of the skill (e.g. no second Rejuvenation on the same ally)
    public bool skipTargetsWithItsBuff;
}
