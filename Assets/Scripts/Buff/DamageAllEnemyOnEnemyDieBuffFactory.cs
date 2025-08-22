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

    void OnEntityDie(Entity target)
    {
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            if (entity != target.gameObject)
            {
                ResourceModifier resourceModifier = new ResourceModifier();
                resourceModifier.consumers.Add(data.damageToAllEntity.GetConsumer(target.gameObject, target.gameObject));
                resourceModifier.multiplier = _stacks;
                resourceModifier.source = target.gameObject;

                entity.GetComponent<Entity>().health.AddResourceModifier(resourceModifier);
            }
        }
    }

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
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