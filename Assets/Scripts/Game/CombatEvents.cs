using UnityEngine;
using UnityEngine.Events;

// The events of a fight, raised by whatever plays it (a run, the sandbox, the balance simulator) and listened to by the
// buffs, the items, the combat log and the visuals, so that none of them depends on a game type or on the entity manager
public static class CombatEvents
{
    // Every entity once initialized, placed outside a battle too
    public static readonly UnityEvent<Entity> OnEntitySpawned = new UnityEvent<Entity>();
    // Raised before the projectile is initialized (no source yet)
    public static readonly UnityEvent<GameObject> OnProjectileSpawned = new UnityEvent<GameObject>();
    // The units can be placed, before the battle
    public static readonly UnityEvent OnRoundStart = new UnityEvent();
    public static readonly UnityEvent OnBattleStart = new UnityEvent();
    // The fight is over, won when every enemy is dead (lost when the team is dead or the fight is stopped)
    public static readonly UnityEvent<bool> OnBattleEnd = new UnityEvent<bool>();
    // After the battle, once the summons are gone and the units reset
    public static readonly UnityEvent OnRoundEnd = new UnityEvent();
    public static readonly UnityEvent<Entity> OnEntityKilled = new UnityEvent<Entity>();
    // An entity summoned during a battle, once it's tagged as a summon
    public static readonly UnityEvent<Entity> OnEntitySummoned = new UnityEvent<Entity>();

    // The units of a scene are gone with it: their listeners must not be called by the next fight
    public static void Clear()
    {
        OnEntitySpawned.RemoveAllListeners();
        OnProjectileSpawned.RemoveAllListeners();
        OnRoundStart.RemoveAllListeners();
        OnBattleStart.RemoveAllListeners();
        OnBattleEnd.RemoveAllListeners();
        OnRoundEnd.RemoveAllListeners();
        OnEntityKilled.RemoveAllListeners();
        OnEntitySummoned.RemoveAllListeners();
    }
}
