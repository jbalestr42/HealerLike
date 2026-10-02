using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/DamageAllEntityOnEntityDieBuff")]
public class DamageAllEntityOnEntityDieBuffFactory : BuffFactory<DamageAllEntityOnEntityDieBuff, DamageAllEntityOnEntityDieBuffData> { }

public enum DeathTrigger
{
    // Any entity dying, from any side
    AnyEntity,
    // Only the entity carrying the buff
    Owner,
}

[Serializable]
public class DamageAllEntityOnEntityDieBuffData
{
    public DeathTrigger trigger = DeathTrigger.AnyEntity;
    [CreateDataButton]
    public AConsumerFactory damageToAllEntity;
    public Entity.EntityType entityType;
    // Damages the opponents of the entity carrying the buff instead of entityType, whatever its side
    public bool targetOwnerOpponents;
}

public class DamageAllEntityOnEntityDieBuff : ABuff<DamageAllEntityOnEntityDieBuffData>, IStackableBuff
{
    int _stacks = 1;
    // Entity carrying the buff, source of the damage
    GameObject _owner;

    void OnEntityDie(Entity dead)
    {
        // Safety net if the owner is gone without Remove() being called: stop listening
        if (_owner == null)
        {
            EntityManager.instance.OnEntityKilled.RemoveListener(OnEntityDie);
            return;
        }

        if (data.trigger == DeathTrigger.Owner && dead.gameObject != _owner)
        {
            return;
        }

        Entity.EntityType targetType = data.targetOwnerOpponents ? _owner.GetComponent<Entity>().GetTargetType() : data.entityType;
        foreach (GameObject entity in EntityManager.instance.GetEntities(targetType))
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
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntityKilled.RemoveListener(OnEntityDie);
        }
    }

    #region IStackableBuff

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }

    #endregion
}