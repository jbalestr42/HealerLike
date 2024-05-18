using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/SimpleConsumer")]
public class SimpleConsumerFactory : ConsumerFactory<SimpleConsumer, SimpleConsumerData> { }

[Serializable]
public class SimpleConsumerData : ConsumerBaseData
{
    public float value;
}

public class SimpleConsumer : AConsumer<SimpleConsumerData>
{
    public override float GetValue()
    {
        return -data.value;
    }
}