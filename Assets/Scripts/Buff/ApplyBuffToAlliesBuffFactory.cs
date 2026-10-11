using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffToAlliesBuff")]
public class ApplyBuffToAlliesBuffFactory : BuffFactory<ApplyBuffToAlliesBuff, ApplyBuffToAlliesBuffData> { }

[Serializable]
public class ApplyBuffToAlliesBuffData
{
    // Given by the holder to each of its allies while it has this buff
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

// Aura: while the holder has it, every ally of the holder (same side, not itself), already there or spawned later,
// has the buff, removed from all of them with it (e.g. the Blood Warden)
public class ApplyBuffToAlliesBuff : ABuff<ApplyBuffToAlliesBuffData>
{
    Entity _owner;
    readonly List<Entity> _allies = new List<Entity>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        foreach (GameObject entity in new List<GameObject>(EntityManager.instance.GetEntities(_owner.entityType)))
        {
            if (entity != null)
            {
                Give(entity.GetComponent<Entity>());
            }
        }
        EntityManager.instance.OnEntitySpawned.AddListener(Give);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntitySpawned.RemoveListener(Give);
        }

        foreach (Entity ally in _allies)
        {
            // A dead ally took the buff with it
            if (ally != null && _owner != null && data.buffHandlerFactory != null)
            {
                ally.GetComponent<BuffManager>().RemoveHandler(data.buffHandlerFactory, _owner.gameObject, ally.gameObject);
            }
        }
        _allies.Clear();
    }

    void Give(Entity ally)
    {
        // Safety net if the owner is gone without Remove() being called: stop listening
        if (_owner == null)
        {
            EntityManager.instance.OnEntitySpawned.RemoveListener(Give);
            return;
        }

        if (ally == null || ally == _owner || ally.entityType != _owner.entityType || _allies.Contains(ally) || data.buffHandlerFactory == null)
        {
            return;
        }

        BuffManager buffManager = ally.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            return;
        }

        buffManager.AddHandler(data.buffHandlerFactory, _owner.gameObject, ally.gameObject);
        _allies.Add(ally);
    }
}
