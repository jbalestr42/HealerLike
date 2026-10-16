using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ReviveAllyOnDeathBuff")]
public class ReviveAllyOnDeathBuffFactory : BuffFactory<ReviveAllyOnDeathBuff, ReviveAllyOnDeathBuffData> { }

[Serializable]
public class ReviveAllyOnDeathBuffData
{
    // Given to the holder when an ally falls, while it raises it: the ally rises when it ends with the holder still
    // alive, its duration being the time to raise (e.g. 6s of enrage). Also what shows the raising (its icon)
    [CreateDataButton]
    public ABuffHandlerFactory raisingBuffHandlerFactory;
    // Part (0-1) of the max health the ally rises with
    [Range(0f, 1f)]
    public float healthRatio = 0.5f;
}

// Each ally of the same kind as the holder that falls (e.g. its twin) rises again where it died once the holder is
// done raising it, unless the holder dies first. Without limit: a raised ally can fall and rise again
public class ReviveAllyOnDeathBuff : ABuff<ReviveAllyOnDeathBuffData>
{
    struct Fallen
    {
        public EntityData data;
        public Vector3 position;
    }

    Entity _owner;
    BuffManager _buffManager;
    readonly List<Fallen> _fallen = new List<Fallen>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _buffManager = target.GetComponent<BuffManager>();
        _buffManager.OnBuffHandlerStopped.AddListener(OnBuffHandlerStopped);
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityKilled);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_buffManager != null)
        {
            _buffManager.OnBuffHandlerStopped.RemoveListener(OnBuffHandlerStopped);
        }
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntityKilled.RemoveListener(OnEntityKilled);
        }
        _fallen.Clear();
    }

    void OnEntityKilled(Entity dead)
    {
        // Safety net if the owner is gone without Remove() being called: stop listening
        if (_owner == null)
        {
            EntityManager.instance.OnEntityKilled.RemoveListener(OnEntityKilled);
            return;
        }

        if (!CanRaise(dead) || data.raisingBuffHandlerFactory == null)
        {
            return;
        }

        _fallen.Add(new Fallen { data = dead.data, position = dead.transform.position });
        _buffManager.AddHandler(data.raisingBuffHandlerFactory, _owner.gameObject, _owner.gameObject);
    }

    bool CanRaise(Entity dead)
    {
        return dead != null && dead != _owner && dead.entityType == _owner.entityType && dead.data != null
            && dead.data == _owner.data;
    }

    // The raising is over: the holder is alive, a dying one is never notified (see BuffManager.OnDestroy)
    void OnBuffHandlerStopped(BuffManager.BuffHandlerData stopped)
    {
        if (stopped.buffHandlerFactory != data.raisingBuffHandlerFactory || _owner == null || _owner.health.Value <= 0f)
        {
            return;
        }

        foreach (Fallen fallen in _fallen)
        {
            Entity raised = Spawn(fallen.data, fallen.position);
            if (raised != null)
            {
                raised.health.SetValue(raised.health.Max * data.healthRatio);
            }
        }
        _fallen.Clear();
    }

    // Joins the running battle as a summon, on the nearest free cell of its side
    protected virtual Entity Spawn(EntityData entityData, Vector3 position)
    {
        GameObject raised = EntityManager.instance.SpawnEntity(entityData, position, _owner.entityType);
        if (raised == null)
        {
            return null;
        }
        Entity entity = raised.GetComponent<Entity>();
        // Entities are disabled at spawn until the battle starts, this one joins the running battle
        EntityManager.instance.AddSummon(entity);
        entity.Enable(true);
        return entity;
    }
}
