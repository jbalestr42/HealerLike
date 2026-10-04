using System;
using System.Collections.Generic;
using UnityEngine;

// How dangerous a wave is, measured in simulated fights against teams that don't depend on any character,
// stored in the wave so the game can use it (e.g. to pick waves by threat)
[Serializable]
public class WaveScore
{
    [Tooltip("Mean damage per second dealt by the wave to 5 Punching Bags placed like a team, during 30s (60s for an elite or a boss), averaged over the seeds. The bags have no armor: it is the damage before any reduction.")]
    public float dps;

    [Tooltip("Most damage dealt by the wave within 3s against the same bags, per second: the bursts a healer must answer quickly.")]
    public float peakDps;

    [Tooltip("Seconds the wave survives against the balance team (5 invincible attackers dealing different damage, some of it on an area), summons included, averaged over the seeds.")]
    public float survivalTime;

    [Tooltip("Survival time x damage per second of the balance team against the Punching Bags: the damage needed to kill the wave, counting its health, armor, heals, shields and summons.")]
    public float effectiveHealth;

    [Tooltip("Damage per second x survival time: the damage the wave deals before dying, to compare with the health of a team and the mana of a healer.")]
    public float threat;

    [Tooltip("The balance team didn't kill the wave before the time limit: the survival time, the effective health and the threat are only lower bounds.")]
    public bool timedOut;

    [Tooltip("Hash of the wave and of every data it depends on (enemies, skills, items) when it was measured. A different hash means the data changed since: the score is out of date. Empty when never measured.")]
    public string fingerprint;

    public bool measured => !string.IsNullOrEmpty(fingerprint);

    public static float GetThreat(float dps, float survivalTime)
    {
        return dps * survivalTime;
    }

    public static float GetEffectiveHealth(float survivalTime, float balanceTeamDps)
    {
        return survivalTime * balanceTeamDps;
    }

    // Hash of the contents of the data a wave depends on, given in a stable order
    public static string ComputeFingerprint(IEnumerable<string> contents)
    {
        Hash128 hash = new Hash128();
        foreach (string content in contents)
        {
            hash.Append(content);
        }
        return hash.ToString();
    }

    public bool IsUpToDate(string currentFingerprint)
    {
        return measured && fingerprint == currentFingerprint;
    }
}
