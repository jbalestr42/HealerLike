using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/AttributeConsumer")]
public class AttributeConsumerFactory: ConsumerFactory<AttributeConsumer, AttributeConsumerData> { }

[Serializable]
public class AttributeConsumerData : ConsumerBaseData
{
    public AttributeType type;
}

public class AttributeConsumer : AConsumer<AttributeConsumerData>
{
    public override float GetValue()
    {
        return -source.GetComponent<AttributeManager>().Get(data.type).Value;
    }
}