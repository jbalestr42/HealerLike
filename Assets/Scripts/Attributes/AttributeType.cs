using UnityEngine;

public enum AttributeType
{
    HealthMax,
    AttackRate,
    Damage,
    Range,
    FlatArmor,
    PercentArmor,
    HitArmor,
    Speed,
    Vulnerability,
    ManaMax,
    HealPower,
    CriticalChance,
    CriticalMultiplier,
    CriticalChanceResist,
    // Multiplier of the heals received, 1 when the entity has no such attribute
    HealingReceived,
    // Multiplier of the character skill cooldowns, 1 when the character has no such attribute
    SkillCooldownMultiplier,
    // Critical chance added to the regular one for the heals sent by the entity
    HealCriticalChance,
    // Item choices added to every reward, 0 when the character has no such attribute
    RewardChoices,
}