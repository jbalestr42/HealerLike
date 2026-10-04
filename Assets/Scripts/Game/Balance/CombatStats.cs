using System;
using System.Collections.Generic;
using UnityEngine;

// What happened during one fight, written as one line of the combat log to balance the characters and the waves
[Serializable]
public class CombatStats
{
    public const float PeakWindow = 3f;

    [Serializable]
    public struct SkillCasts
    {
        public string skill;
        public int count;
    }

    [Serializable]
    public struct DamageEvent
    {
        public float time;
        public float amount;
    }

    // Context
    public string runId;
    public int seed;
    public int floor;
    public string roomType;
    public string wave;
    public string character;
    public List<string> allies = new List<string>();
    // Healer bot profile playing the character in a simulation, empty in a real run
    public string bot;

    // Result
    public bool won;
    // Stopped before either side died (simulation only), counted as lost
    public bool timedOut;
    public float duration;

    // Mana of the character
    public float manaMax;
    public float manaStart;
    public float manaSpent;
    public float manaGained;
    // Part of the mana gains above the max, lost
    public float manaOverflow;
    public float manaEnd;

    // Allies, summons included
    public float allyHealthMax;
    public float allyHealthStart;
    public float allyHealthEnd;
    public float allyDamageTaken;
    // Most damage taken by the allies within PeakWindow seconds
    public float allyPeakDamage;
    public float characterHeal;
    public float characterOverheal;
    // Heals of the units themselves (healers, life steal, ...)
    public float otherHeal;
    public float otherOverheal;
    public List<string> deadAllies = new List<string>();
    // Change of health neither from a heal nor from damage, e.g. Balance Life setting it, a max health buff,
    // or the damage of a killing blow above the health left
    public float allyHealthOtherChange;

    // Skills cast by the healer bot
    public List<SkillCasts> casts = new List<SkillCasts>();

    // Enemies, summons included
    public float enemyHealthMax;
    public float enemyDamageTaken;

    // Not in the log: only used to find the peak
    [NonSerialized] List<DamageEvent> _allyDamageEvents = new List<DamageEvent>();
    [NonSerialized] float _startTime;

    public float manaSpentShare => manaMax > 0f ? manaSpent / manaMax : 0f;
    public float manaEndShare => manaMax > 0f ? manaEnd / manaMax : 0f;
    public float allyHealthLostShare => allyHealthMax > 0f ? (allyHealthStart - allyHealthEnd) / allyHealthMax : 0f;
    // Damage per second dealt by the enemies to the allies, on the whole fight and at its peak
    public float enemyDps => duration > 0f ? allyDamageTaken / duration : 0f;
    public float enemyPeakDps => allyPeakDamage / PeakWindow;

    public void Start(float time, float mana, float maxMana)
    {
        _startTime = time;
        manaStart = mana;
        manaMax = maxMana;
    }

    public void AddAlly(string allyName, float health, float maxHealth)
    {
        allies.Add(allyName);
        allyHealthStart += health;
        allyHealthMax += maxHealth;
    }

    public void AddEnemy(float maxHealth)
    {
        enemyHealthMax += maxHealth;
    }

    // value: change of the mana, negative when spent; overflow: part of a gain above the max
    public void RecordMana(float value, float overflow)
    {
        if (value < 0f)
        {
            manaSpent -= value;
        }
        else
        {
            manaGained += value - overflow;
            manaOverflow += overflow;
        }
    }

    // value: change of the health, negative for damage; overflow: part of a heal above the max
    public void RecordAllyHealth(float time, float value, float overflow, bool isFromCharacter)
    {
        if (value < 0f)
        {
            allyDamageTaken -= value;
            _allyDamageEvents.Add(new DamageEvent { time = time, amount = -value });
        }
        else if (isFromCharacter)
        {
            characterHeal += value - overflow;
            characterOverheal += overflow;
        }
        else
        {
            otherHeal += value - overflow;
            otherOverheal += overflow;
        }
    }

    public void RecordEnemyHealth(float value)
    {
        if (value < 0f)
        {
            enemyDamageTaken -= value;
        }
    }

    public void RecordAllyDeath(string allyName)
    {
        deadAllies.Add(allyName);
    }

    public void Finish(float time, bool hasWon, float mana, float allyHealth)
    {
        won = hasWon;
        duration = time - _startTime;
        manaEnd = mana;
        allyHealthEnd = allyHealth;
        allyHealthOtherChange = allyHealthEnd - allyHealthStart + allyDamageTaken - characterHeal - otherHeal;
        allyPeakDamage = GetPeakDamage(_allyDamageEvents, PeakWindow);
    }

    // Most damage within window seconds, the events being sorted by time
    public static float GetPeakDamage(IReadOnlyList<DamageEvent> events, float window)
    {
        float peak = 0f;
        float sum = 0f;
        int first = 0;
        for (int i = 0; i < events.Count; i++)
        {
            sum += events[i].amount;
            while (events[i].time - events[first].time >= window)
            {
                sum -= events[first].amount;
                first++;
            }
            peak = Mathf.Max(peak, sum);
        }
        return peak;
    }

    public string ToSummary()
    {
        return $"{(won ? "Won" : timedOut ? "Timed out" : "Lost")} {roomType} '{wave}' floor {floor} in {duration:0.0}s"
            + $" | mana spent {manaSpent:0} ({manaSpentShare:P0} of max), end {manaEnd:0} ({manaEndShare:P0})"
            + $" | allies took {allyDamageTaken:0} (peak {allyPeakDamage:0} in {PeakWindow:0}s), lost {allyHealthLostShare:P0} of their health, {deadAllies.Count} dead, {allyHealthOtherChange:+0;-0;0} health from other sources"
            + $" | healed {characterHeal:0} (+{characterOverheal:0} overheal), units healed {otherHeal:0}";
    }
}
