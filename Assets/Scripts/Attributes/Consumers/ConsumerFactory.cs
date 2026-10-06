using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/Consumer")]
public class ConsumerFactory : ConsumerFactory<Consumer, ConsumerData> { }

// Entity the value of a consumer is computed from
public enum ConsumerValueOwner
{
    // The one applying the consumer (e.g. its Heal Power, its max health)
    Source,
    // The one receiving it (e.g. a percent of the health of the enemy hit)
    Target,
}

[Serializable]
public class ConsumerData : ConsumerBaseData
{
    [SerializeReference]
    public AValue value;
    public ConsumerValueOwner valueOwner = ConsumerValueOwner.Source;
}

public class Consumer : AConsumer<ConsumerData>
{
    public override float GetValue()
    {
        GameObject owner = data.valueOwner == ConsumerValueOwner.Target ? target : source;
        // No target to read from (e.g. a consumer applied without one): no value
        if (owner == null)
        {
            return 0f;
        }
        return -data.value.GetValue(owner);
    }
}
