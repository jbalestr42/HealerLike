using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffOnAllyDeathBuff")]
public class ApplyBuffOnAllyDeathBuffFactory : BuffFactory<ApplyBuffOnAllyDeathBuff, ApplyBuffOnAllyDeathBuffData> { }

[Serializable]
public class ApplyBuffOnAllyDeathBuffData
{
    // Given to the holder by itself each time one of its allies dies: its duration and max stacks say how the
    // deaths add up
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

// Each death of an ally of the holder (same side, not itself) gives it a buff (e.g. the Vengeful Sniper)
public class ApplyBuffOnAllyDeathBuff : ABuff<ApplyBuffOnAllyDeathBuffData>
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

        if (dead == null || dead == _owner || dead.entityType != _owner.entityType)
        {
            return;
        }

        BuffManager buffManager = _owner.GetComponent<BuffManager>();
        if (buffManager == null || data.buffHandlerFactory == null)
        {
            return;
        }

        buffManager.AddHandler(data.buffHandlerFactory, _owner.gameObject, _owner.gameObject);
    }
}
