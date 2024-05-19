using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/DamageAllEnemyOnEnemyDieBuff")]
public class DamageAllEnemyOnEnemyDieBuffFactory : BuffFactory<DamageAllEnemyOnEnemyDieBuff, DamageAllEnemyOnEnemyDieBuffData> { }

[Serializable]
public class DamageAllEnemyOnEnemyDieBuffData
{
    [CreateDataButton]
    public AConsumerFactory damageToAllEnemy;
    public Entity.EntityType entityType;
}

public class DamageAllEnemyOnEnemyDieBuff : ABuff<DamageAllEnemyOnEnemyDieBuffData>, IStackableBuff
{
    int _stacks = 1;

    void OnEntityDie(Entity target)
    {
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            if (entity != target.gameObject)
            {
                ResourceModifier resourceModifier = new ResourceModifier();
                resourceModifier.consumers.Add(data.damageToAllEnemy.GetConsumer(target.gameObject, target.gameObject));
                resourceModifier.multiplier = _stacks;
                resourceModifier.source = target.gameObject;

                entity.GetComponent<Entity>().health.AddResourceModifier(resourceModifier);
            }
        }
    }

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