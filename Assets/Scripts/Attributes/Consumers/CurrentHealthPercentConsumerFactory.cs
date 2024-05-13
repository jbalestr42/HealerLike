using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/CurrentHealthPercentConsumer")]
public class CurrentHealthPercentConsumerFactory : ConsumerFactory<CurrentHealthPercentConsumer, CurrentHealthPercentConsumerData> { }

[Serializable]
public class CurrentHealthPercentConsumerData : ConsumerBaseData
{
    public float multiplier;
    public bool inverse;
}

public class CurrentHealthPercentConsumer : AConsumer<CurrentHealthPercentConsumerData>
{
    public override float GetValue()
    {
        float baseHealth = 0f;
        
        if (data.inverse)
        {
            baseHealth = target.GetComponent<Entity>().health.Max - target.GetComponent<Entity>().health.Value;
        }
        else
        {
            baseHealth = target.GetComponent<Entity>().health.Value;
        }
        return -baseHealth * data.multiplier;
    }
}