using System.Collections.Generic;
using UnityEngine;

// Damage and heals an entity really dealt, over the current fight, its last seconds and the whole run.
// The values are raw: the overkill of a hit and the overflow of a heal count. The times are given by the
// caller (the game time), so it can be tested without a clock. Only what happens during a fight counts
public class CombatMeter
{
    // Seconds of the recent rate
    public const float RecentWindow = 5f;
    // A rate is never computed over less than this, so the first hit of a fight doesn't read as a huge rate
    public const float MinDuration = 1f;

    struct Record
    {
        public float time;
        public float damage;
        public float heal;
    }

    readonly Queue<Record> _recent = new Queue<Record>();

    bool _isInFight;
    bool _hasFought;
    float _fightStart;
    float _fightEnd;
    float _fightDamage;
    float _fightHeal;

    // Fights before the current or last one
    int _pastFights;
    float _pastDuration;
    float _pastDamage;
    float _pastHeal;

    public bool isInFight => _isInFight;
    // A fight started at least once
    public bool hasFought => _hasFought;
    // Fights of the run, the current one included
    public int fightCount => hasFought ? _pastFights + 1 : 0;
    public float fightDamage => _fightDamage;
    public float fightHeal => _fightHeal;
    public float runDamage => _pastDamage + _fightDamage;
    public float runHeal => _pastHeal + _fightHeal;

    // Called again during a fight (e.g. a summon enabled twice), it changes nothing
    public void StartFight(float time)
    {
        if (_isInFight)
        {
            return;
        }

        if (hasFought)
        {
            _pastFights++;
            _pastDuration += GetFightDuration(time);
            _pastDamage += _fightDamage;
            _pastHeal += _fightHeal;
        }
        _isInFight = true;
        _hasFought = true;
        _fightStart = time;
        _fightEnd = time;
        _fightDamage = 0f;
        _fightHeal = 0f;
        _recent.Clear();
    }

    public void EndFight(float time)
    {
        if (!_isInFight)
        {
            return;
        }
        _isInFight = false;
        _fightEnd = time;
    }

    public void AddDamage(float time, float damage)
    {
        if (!_isInFight || damage <= 0f)
        {
            return;
        }
        _fightDamage += damage;
        _recent.Enqueue(new Record { time = time, damage = damage });
    }

    public void AddHeal(float time, float heal)
    {
        if (!_isInFight || heal <= 0f)
        {
            return;
        }
        _fightHeal += heal;
        _recent.Enqueue(new Record { time = time, heal = heal });
    }

    // Duration of the current fight, or of the last one once it ended
    public float GetFightDuration(float time)
    {
        return (_isInFight ? time : _fightEnd) - _fightStart;
    }

    public float GetRunDuration(float time)
    {
        return _pastDuration + (hasFought ? GetFightDuration(time) : 0f);
    }

    public float GetFightDps(float time) => GetRate(_fightDamage, GetFightDuration(time));
    public float GetFightHps(float time) => GetRate(_fightHeal, GetFightDuration(time));
    public float GetRunDps(float time) => GetRate(runDamage, GetRunDuration(time));
    public float GetRunHps(float time) => GetRate(runHeal, GetRunDuration(time));

    // Over the last RecentWindow seconds of the fight, or since its start when it's shorter
    public float GetRecentDps(float time)
    {
        GetRecent(time, out float damage, out float heal, out float duration);
        return GetRate(damage, duration);
    }

    public float GetRecentHps(float time)
    {
        GetRecent(time, out float damage, out float heal, out float duration);
        return GetRate(heal, duration);
    }

    void GetRecent(float time, out float damage, out float heal, out float duration)
    {
        float windowStart = time - RecentWindow;
        while (_recent.Count > 0 && _recent.Peek().time < windowStart)
        {
            _recent.Dequeue();
        }

        damage = 0f;
        heal = 0f;
        foreach (Record record in _recent)
        {
            damage += record.damage;
            heal += record.heal;
        }
        duration = Mathf.Min(RecentWindow, GetFightDuration(time));
    }

    static float GetRate(float amount, float duration)
    {
        return amount / Mathf.Max(duration, MinDuration);
    }
}
