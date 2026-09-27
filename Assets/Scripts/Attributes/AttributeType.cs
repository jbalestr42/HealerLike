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
}