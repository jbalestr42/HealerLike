using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ShieldOnOverhealBuff")]
public class ShieldOnOverhealBuffFactory : BuffFactory<ShieldOnOverhealBuff, ShieldOnOverhealBuffData> { }

[Serializable]
public class ShieldOnOverhealBuffData
{
    // Part of the heal above the max health of a unit turned into shield on it
    [Min(0)]
    public float ratio = 1f;
    // The shield it gives never takes the shield of a unit above this part of its max health
    [Range(0f, 1f)]
    public float maxHealthRatio = 0.2f;
    // Side of the units whose overheal becomes shield
    public Entity.EntityType healedType = Entity.EntityType.Player;
}

// Held by the character (e.g. the Relic of Light): each heal above the max health of a unit of the healed side becomes
// a shield on it, taking the damage before its health
public class ShieldOnOverhealBuff : ABuff<ShieldOnOverhealBuffData>
{
    // What it listens to on each unit, to stop listening
    struct Listeners
    {
        public UnityAction<GameObject, ConsumerResult> onHeal;
        public UnityAction<PendingValue> onShieldGain;
    }

    GameObject _owner;
    readonly Dictionary<Entity, Listeners> _listened = new Dictionary<Entity, Listeners>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target;
        EntityManager entityManager = EntityManager.instance;
        foreach (GameObject entity in new List<GameObject>(entityManager.GetEntities(data.healedType)))
        {
            Listen(entity != null ? entity.GetComponent<Entity>() : null);
        }
        entityManager.OnEntitySpawned.AddListener(Listen);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntitySpawned.RemoveListener(Listen);
        }
        StopListening();
    }

    // Listens to the heals of the unit when it's of the healed side and has a shield
    public void Listen(Entity entity)
    {
        if (entity == null || entity.shield == null || entity.entityType != data.healedType || _listened.ContainsKey(entity))
        {
            return;
        }

        Listeners listeners = new Listeners
        {
            onHeal = (source, heal) => AddShield(entity, heal),
            onShieldGain = pending => LimitGain(entity, pending),
        };
        entity.OnHealReceived.AddListener(listeners.onHeal);
        entity.shield.OnBeforeValueApplied.AddListener(listeners.onShieldGain);
        _listened.Add(entity, listeners);
    }

    public void StopListening()
    {
        foreach (KeyValuePair<Entity, Listeners> listened in _listened)
        {
            // The dead units are destroyed along with their listeners
            if (listened.Key == null)
            {
                continue;
            }
            listened.Key.OnHealReceived.RemoveListener(listened.Value.onHeal);
            listened.Key.shield.OnBeforeValueApplied.RemoveListener(listened.Value.onShieldGain);
        }
        _listened.Clear();
    }

    // Through a consumer, as any gain: the overflow is already final (crit and heals received applied), so it can't
    // crit again
    public void AddShield(Entity entity, ConsumerResult heal)
    {
        float gain = heal.overflow * data.ratio;
        if (entity == null || entity.shield == null || gain <= 0f)
        {
            return;
        }
        ResourceModifier shieldModifier = new ResourceModifier { source = _owner };
        shieldModifier.consumers.Add(new RuntimeConsumer(gain, canBeCritical: false));
        entity.shield.AddResourceModifier(shieldModifier);
    }

    // Each of its gains, when applied (after the ones of the same frame), stops at its part of the max health
    void LimitGain(Entity entity, PendingValue pending)
    {
        if (pending.resourceModifier.source != _owner || pending.value <= 0f)
        {
            return;
        }
        float room = entity.health.Max * data.maxHealthRatio - entity.shield.Value;
        pending.value = Mathf.Clamp(pending.value, 0f, Mathf.Max(0f, room));
    }
}
