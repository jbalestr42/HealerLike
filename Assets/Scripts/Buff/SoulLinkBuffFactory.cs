using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/SoulLinkBuff")]
public class SoulLinkBuffFactory : BuffFactory<SoulLinkBuff, SoulLinkBuffData> { }

[Serializable]
public class SoulLinkBuffData
{
}

// The damage dealt by the enemies to the holder is split evenly between it and every other living ally
public class SoulLinkBuff : ABuff<SoulLinkBuffData>
{
    Entity _owner;

    public override bool isStackable => false;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.health.OnBeforeValueApplied.AddListener(OnBeforeValueApplied);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null && _owner.health != null)
        {
            _owner.health.OnBeforeValueApplied.RemoveListener(OnBeforeValueApplied);
        }
    }

    void OnBeforeValueApplied(PendingValue pendingValue)
    {
        // Only the damage of the enemies: the shares, sent by the holder, are never split again
        if (pendingValue.value >= 0f || !IsFromEnemy(pendingValue.resourceModifier))
        {
            return;
        }

        List<Entity> allies = FindLinkedAllies();
        if (allies.Count == 0)
        {
            return;
        }

        float share = pendingValue.value / (allies.Count + 1);
        foreach (Entity ally in allies)
        {
            // Already reduced by the armor of the holder and already critical or not: final damage, but an
            // invincible ally takes nothing
            ResourceModifier sharedDamage = new() { source = _owner.gameObject };
            sharedDamage.consumers.Add(new RuntimeConsumer(share, ignoreDamageReduction: true, ignoreConsumerPrevention: false, canBeCritical: false));
            ally.health.AddResourceModifier(sharedDamage);
        }
        pendingValue.value = share;
    }

    bool IsFromEnemy(ResourceModifier resourceModifier)
    {
        Entity source = resourceModifier.source != null ? resourceModifier.source.GetComponent<Entity>() : null;
        return source == null || source.entityType != _owner.entityType;
    }

    List<Entity> FindLinkedAllies()
    {
        List<Entity> allies = new();
        foreach (GameObject go in GetAllies())
        {
            Entity ally = go != null ? go.GetComponent<Entity>() : null;
            if (ally != null && ally != _owner && ally.health.Value > 0f)
            {
                allies.Add(ally);
            }
        }
        return allies;
    }

    protected virtual List<GameObject> GetAllies()
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null ? entityManager.GetEntities(_owner.entityType) : new List<GameObject>();
    }
}
