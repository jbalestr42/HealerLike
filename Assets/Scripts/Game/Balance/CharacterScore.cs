using System;
using UnityEngine;

// How strong a character is when a run starts (its starting units, items and skills), measured in simulated fights
// against the same teams as the waves: the Balance Dummies and the balance team as enemies, without then with the
// skills of the character cast by its healer bot
[Serializable]
public class CharacterScore
{
    [Tooltip("Mean damage per second dealt by the starting units to 5 Balance Dummies, during 30s, averaged over the seeds. The character casts nothing.")]
    public float dps;

    [Tooltip("Seconds the starting units survive against the balance team (5 invincible attackers) when the character casts nothing, averaged over the seeds.")]
    public float survivalTime;

    [Tooltip("Survival time x damage per second of the balance team against the Balance Dummies: the damage needed to kill the starting units when the character casts nothing, counting their health, armor and shields.")]
    public float effectiveHealth;

    [Tooltip("Damage per second x survival time: the damage the starting units deal before dying when the character casts nothing, to compare with the effective health of a wave.")]
    public float threat;

    [Tooltip("Health given back by the skills of the character per second, overheal excluded, while its healer bot casts them against the balance team.")]
    public float healingPerSecond;

    [Tooltip("Mana spent by the character per second while its healer bot casts its skills against the balance team.")]
    public float manaPerSecond;

    [Tooltip("Seconds the starting units survive against the balance team while the healer bot of the character casts its skills, averaged over the seeds.")]
    public float spellSurvivalTime;

    [Tooltip("Mean damage per second dealt by the starting units to 5 Balance Dummies, during 30s, while the healer bot of the character casts its skills, averaged over the seeds.")]
    public float spellDps;

    [Tooltip("The same as the effective health, while the healer bot of the character casts its skills: the damage needed to kill the starting units.")]
    public float spellEffectiveHealth;

    [Tooltip("Damage per second x survival time, both while the healer bot of the character casts its skills: the damage the starting units deal before dying, to compare with the effective health of a wave.")]
    public float spellThreat;

    [Tooltip("The starting units were still alive at the time limit: the survival times and effective health are only lower bounds.")]
    public bool timedOut;

    [Tooltip("Hash of the character, its healer bot and every data they depend on when it was measured. A different hash means the data changed since: the score is out of date.")]
    public string fingerprint;
}
