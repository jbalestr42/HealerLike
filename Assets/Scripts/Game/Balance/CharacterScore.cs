using System;
using UnityEngine;

// How strong a character is when a run starts (its starting units, items and skills), measured in simulated fights
// against the same teams as the waves: the Punching Bags and the balance team as enemies
[Serializable]
public class CharacterScore
{
    [Tooltip("Mean damage per second dealt by the starting units to 5 Punching Bags, during 30s, averaged over the seeds. The character casts nothing.")]
    public float dps;

    [Tooltip("Seconds the starting units survive against the balance team (5 invincible attackers) when the character casts nothing, averaged over the seeds.")]
    public float survivalTime;

    [Tooltip("Survival time x damage per second of the balance team against the Punching Bags: the damage needed to kill the starting units without any heal, counting their health, armor and shields.")]
    public float effectiveHealth;

    [Tooltip("Seconds the starting units survive against the balance team when the character casts its skills with its healer bot, averaged over the seeds.")]
    public float healedSurvivalTime;

    [Tooltip("The same as the effective health, with the skills of the character: the damage needed to kill the team while the healer plays.")]
    public float healedEffectiveHealth;

    [Tooltip("Damage per second x survival time: the damage the starting units deal before dying when the character casts nothing, to compare with the effective health of a wave.")]
    public float threat;

    [Tooltip("Damage per second x healed survival time: the damage the starting units deal before dying while the character heals them, to compare with the effective health of a wave.")]
    public float healedThreat;

    [Tooltip("Health given back by the skills of the character per second, overheal excluded, while healing against the balance team.")]
    public float healingPerSecond;

    [Tooltip("Mana spent by the character per second while healing against the balance team.")]
    public float manaPerSecond;

    [Tooltip("The starting units were still alive at the time limit: the survival times and effective health are only lower bounds.")]
    public bool timedOut;

    [Tooltip("Hash of the character, its healer bot and every data they depend on when it was measured. A different hash means the data changed since: the score is out of date.")]
    public string fingerprint;
}
