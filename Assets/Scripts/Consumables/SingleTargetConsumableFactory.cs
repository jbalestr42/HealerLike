using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumables/SingleTargetConsumable")]
public class SingleTargetConsumableFactory : ConsumableFactory<SingleTargetConsumable, SingleTargetConsumableData> {}

[Serializable]
public class SingleTargetConsumableData : BaseConsumableData
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    public LayerMask layerMask;
}

public class SingleTargetConsumable : AConsumable<SingleTargetConsumableData>
{
    public override void Use()
    {
        InteractionManager.instance.SetInteraction(new BuffTargetInteraction(data.buffHandlerFactory, data.layerMask));
    }
}