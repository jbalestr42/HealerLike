using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/MaxHealthPercentConsumer")]
public class MaxHealthPercentConsumerFactory : ConsumerFactory<MaxHealthPercentConsumer, MaxHealthPercentConsumerData> { }

[Serializable]
public class MaxHealthPercentConsumerData : ConsumerBaseData
{
    public float multiplier;
}

public class MaxHealthPercentConsumer : AConsumer<MaxHealthPercentConsumerData>
{
    public override float GetValue()
    {
        return -target.GetComponent<Entity>().health.Max * data.multiplier;
    }
}