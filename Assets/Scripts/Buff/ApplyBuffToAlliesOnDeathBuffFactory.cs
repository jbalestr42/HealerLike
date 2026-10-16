using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffToAlliesOnDeathBuff")]
public class ApplyBuffToAlliesOnDeathBuffFactory : BuffFactory<ApplyBuffToAlliesOnDeathBuff, ApplyBuffToAlliesOnDeathBuffData> { }

[Serializable]
public class ApplyBuffToAlliesOnDeathBuffData
{
    // Given to each ally still alive with the tag when the holder dies, from that ally itself so it outlives the
    // holder: its duration and max stacks say how it lasts
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    // Only the allies with this tag get it, none: every ally
    public GameplayTag tag;
}

// Legacy: when the holder dies, its allies (same side, not itself) with a tag get a buff (e.g. each Acolyte handing
// its power down to the Archon)
public class ApplyBuffToAlliesOnDeathBuff : ABuff<ApplyBuffToAlliesOnDeathBuffData>
{
    Entity _owner;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityKilled);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntityKilled.RemoveListener(OnEntityKilled);
        }
    }

    void OnEntityKilled(Entity dead)
    {
        // Safety net if the owner is gone without Remove() being called: stop listening
        if (_owner == null)
        {
            EntityManager.instance.OnEntityKilled.RemoveListener(OnEntityKilled);
            return;
        }

        if (dead != _owner || data.buffHandlerFactory == null)
        {
            return;
        }

        foreach (GameObject entity in new List<GameObject>(EntityManager.instance.GetEntities(_owner.entityType)))
        {
            Entity ally = entity != null ? entity.GetComponent<Entity>() : null;
            if (ally == null || ally == _owner || ally.entityType != _owner.entityType)
            {
                continue;
            }
            if (data.tag != null && !ally.HasTag(data.tag))
            {
                continue;
            }

            BuffManager buffManager = ally.GetComponent<BuffManager>();
            if (buffManager != null)
            {
                buffManager.AddHandler(data.buffHandlerFactory, ally.gameObject, ally.gameObject);
            }
        }
    }
}
