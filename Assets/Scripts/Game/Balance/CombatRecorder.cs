using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Listens to the mana of the character and the health of every unit during one fight, and fills a CombatStats
public class CombatRecorder
{
    readonly CombatStats _stats;
    readonly Func<float> _clock;
    readonly ResourceAttribute _mana;
    readonly List<ResourceAttribute> _allies = new List<ResourceAttribute>();
    readonly HashSet<ResourceAttribute> _watched = new HashSet<ResourceAttribute>();
    readonly List<(ResourceAttribute resource, UnityAction<GameObject, ResourceModifier, ConsumerResult> listener)> _listeners = new List<(ResourceAttribute, UnityAction<GameObject, ResourceModifier, ConsumerResult>)>();

    public CombatStats stats => _stats;

    // stats holds the context of the fight (floor, wave, ...); clock gives the game time
    public CombatRecorder(CombatStats stats, Func<float> clock, ResourceAttribute characterMana)
    {
        _stats = stats;
        _clock = clock;
        _mana = characterMana;
        _stats.Start(_clock(), _mana.Value, _mana.Max);
        Listen(_mana, (_, __, result) => _stats.RecordMana(result.value, result.overflow));
    }

    // A unit already watched is ignored
    public void AddAlly(string allyName, ResourceAttribute health)
    {
        if (!_watched.Add(health))
        {
            return;
        }

        _allies.Add(health);
        _stats.AddAlly(allyName, health.Value, health.Max);
        Listen(health, (_, modifier, result) => _stats.RecordAllyHealth(_clock(), result.value, result.overflow, IsFromCharacter(modifier)));
    }

    public void AddEnemy(ResourceAttribute health)
    {
        if (!_watched.Add(health))
        {
            return;
        }

        _stats.AddEnemy(health.Max);
        Listen(health, (_, __, result) => _stats.RecordEnemyHealth(result.value));
    }

    public void RecordAllyDeath(string allyName)
    {
        _stats.RecordAllyDeath(allyName);
    }

    // Stops listening and returns the stats of the fight
    public CombatStats Stop(bool won)
    {
        foreach (var (resource, listener) in _listeners)
        {
            resource.OnAllConsumerProcessed.RemoveListener(listener);
        }
        _listeners.Clear();
        _watched.Clear();

        _stats.Finish(_clock(), won, _mana != null ? _mana.Value : 0f, GetAllyHealth());
        return _stats;
    }

    // A destroyed or dead ally counts for nothing
    float GetAllyHealth()
    {
        float health = 0f;
        foreach (ResourceAttribute ally in _allies)
        {
            if (ally != null)
            {
                health += ally.Value;
            }
        }
        return health;
    }

    bool IsFromCharacter(ResourceModifier modifier)
    {
        return modifier != null && modifier.source != null && _mana != null && modifier.source == _mana.gameObject;
    }

    void Listen(ResourceAttribute resource, UnityAction<GameObject, ResourceModifier, ConsumerResult> listener)
    {
        resource.OnAllConsumerProcessed.AddListener(listener);
        _listeners.Add((resource, listener));
    }
}
