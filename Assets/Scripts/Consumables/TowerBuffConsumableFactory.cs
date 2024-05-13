using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumables/TowerBuffConsumable")]
public class TowerBuffConsumableFactory : ConsumableFactory<TowerBuffConsumable, TowerBuffConsumableData> {}

[Serializable]
public class TowerBuffConsumableData : BaseConsumableData
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

public class TowerBuffConsumable : AConsumable<TowerBuffConsumableData>
{
    public override void Use()
    {
        foreach (GameObject tower in EntityManager.instance.towers)
        {
            tower.GetComponent<IBuffable>().AddBuffHandler(data.buffHandlerFactory, tower, tower);
        }
    }
}