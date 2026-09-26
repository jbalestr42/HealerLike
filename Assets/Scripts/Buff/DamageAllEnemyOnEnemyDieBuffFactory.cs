using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/DamageAllEntityOnEntityDieBuff")]
public class DamageAllEntityOnEntityDieBuffFactory : BuffFactory<DamageAllEntityOnEntityDieBuff, DamageAllEntityOnEntityDieBuffData> { }

[Serializable]
public class DamageAllEntityOnEntityDieBuffData
{
    [CreateDataButton]
    public AConsumerFactory damageToAllEntity;
    public Entity.EntityType entityType;
}

public class DamageAllEntityOnEntityDieBuff : ABuff<DamageAllEntityOnEntityDieBuffData>, IStackableBuff
{
    int _stacks = 1;
    // Entity carrying the buff, source of the damage
    GameObject _owner;

    void OnEntityDie(Entity dead)
    {
        // A destroyed entity never gets Remove() called on its buffs: stop listening once it is gone
        if (_owner == null)
        {
            EntityManager.instance.OnEntityKilled.RemoveListener(OnEntityDie);
            return;
        }

        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            if (entity != dead.gameObject)
            {
                // The dead entity is destroyed at the end of the frame, it can't be the damage source
                entity.GetComponent<Entity>().health.AddResourceModifier(ResourceModifier.Create(data.damageToAllEntity, _owner, entity, _stacks));
            }
        }
    }

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target;
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityDie);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        EntityManager.instance.OnEntityKilled.RemoveListener(OnEntityDie);
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }
}