using System;
using UnityEngine;

// What a reward item adds on its own, measured in simulated fights between the balance team and the Balance
// Dummies: the damage of the balance team when one of its units holds it, and the effective health of the dummies
// when one of them holds it, each compared with the same fights without item
[Serializable]
public class ItemScore
{
    // Below this share of the value without item, a gain is seen as no effect: these fights can't measure the item,
    // two fights with the same seed already differing by 2 to 4%
    public const float DefaultNoEffectShare = 0.03f;

    // Set in the Items tab of the Scores window
    public static float noEffectShare = DefaultNoEffectShare;

    [Tooltip("Mean damage per second of the balance team against 5 Balance Dummies, during 30s, when one of its units holds the item (each unit in turn, a fight each), or when the character holds it.")]
    public float dps;

    [Tooltip("Damage per second added by the item to the balance team: its damage per second with the item minus without, each fight compared with the fight without item played with the same seed.")]
    public float dpsGain;

    [Tooltip("The damage per second added by the item, as a share of the damage per second of the balance team without item.")]
    public float dpsGainShare;

    [Tooltip("The unit of the balance team adding the most damage per second when it holds the item. Empty for an item of the character.")]
    public string bestHolder;

    [Tooltip("Damage per second added by the item when the best holder holds it.")]
    public float bestHolderDpsGain;

    [Tooltip("Seconds the 5 Balance Dummies survive against the balance team when one of them holds the item (each dummy in turn, a fight each), or when the character holds it.")]
    public float survivalTime;

    [Tooltip("Survival time x damage per second of the balance team without item: the damage needed to kill the 5 dummies when one of them holds the item, counting their health, armor and shields.")]
    public float effectiveHealth;

    [Tooltip("Effective health added by the item to the dummies: their effective health with the item minus without, each fight compared with the fight without item played with the same seed.")]
    public float effectiveHealthGain;

    [Tooltip("The effective health added by the item, as a share of the effective health of the dummies without item.")]
    public float effectiveHealthGainShare;

    [Tooltip("Damage per second x survival time, both with the item, computed as the threat of a wave: the damage the balance team would deal before dying if it had the item both ways.")]
    public float power;

    [Tooltip("Power added by the item: its power minus the power without item (damage per second x survival time, both without item).")]
    public float powerGain;

    [Tooltip("The power added by the item, as a share of the power without item: its damage and robustness gains combined, (1 + damage gain) x (1 + robustness gain) - 1.")]
    public float powerGainShare;

    [Tooltip("The dummies were still alive at the time limit: the survival time and the effective health are only lower bounds.")]
    public bool timedOut;

    [Tooltip("Hash of the item, the measure setup and every data they depend on when it was measured. A different hash means the data changed since: the score is out of date.")]
    public string fingerprint;

    // Whether the item changes the damage of the balance team enough to be measured
    public bool hasDpsEffect => HasEffect(dpsGainShare);

    // Whether the item changes the effective health of the dummies enough to be measured
    public bool hasRobustnessEffect => HasEffect(effectiveHealthGainShare);

    // Whether the item changes its power enough to be measured
    public bool hasPowerEffect => HasEffect(powerGainShare);

    public static bool HasEffect(float gainShare)
    {
        return Mathf.Abs(gainShare) >= noEffectShare;
    }
}
