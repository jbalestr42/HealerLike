using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/Consumer")]
public class ConsumerFactory : ConsumerFactory<Consumer, ConsumerData> { }

[Serializable]
public class ConsumerData : ConsumerBaseData
{
    [SerializeReference]
    public AValue value;
}

public class Consumer : AConsumer<ConsumerData>
{
    public override float GetValue()
    {
        return -data.value.GetValue(source);
    }
}