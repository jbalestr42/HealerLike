using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumables/EnemyBuffConsumable")]
public class EnemyBuffConsumableFactory : ConsumableFactory<EnemyBuffConsumable, EnemyBuffConsumableData> {}

[Serializable]
public class EnemyBuffConsumableData : BaseConsumableData
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

public class EnemyBuffConsumable : AConsumable<EnemyBuffConsumableData>
{
    public override void Use()
    {
        foreach (GameObject enemy in EntityManager.instance.enemies)
        {
            enemy.GetComponent<BuffManager>().AddHandler(data.buffHandlerFactory, enemy, enemy);
        }
    }
}