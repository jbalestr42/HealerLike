using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumables/EnemyConsumerConsumable")]
public class EnemyConsumerConsumableFactory : ConsumableFactory<EnemyConsumerConsumable, EnemyConsumerConsumableData> {}

[Serializable]
public class EnemyConsumerConsumableData : BaseConsumableData
{
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
}

public class EnemyConsumerConsumable : AConsumable<EnemyConsumerConsumableData>
{
    public override void Use()
    {
        foreach (GameObject enemy in EntityManager.instance.enemies)
        {
            ResourceModifier resourceModifier = new ResourceModifier();
            resourceModifier.consumers.Add(data.consumerFactory.GetConsumer(enemy, enemy));
            resourceModifier.multiplier = 1f;
            resourceModifier.source = enemy;

            enemy.GetComponent<Enemy>().health.AddResourceModifier(resourceModifier);
        }
    }
}