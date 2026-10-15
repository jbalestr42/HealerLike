using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/WoundedTargetConsumer")]
public class WoundedTargetConsumerFactory : ConsumerFactory<WoundedTargetConsumer, WoundedTargetConsumerData> { }

[Serializable]
public class WoundedTargetConsumerData : ConsumerData
{
    // Health percent (0-1) the target must be strictly below for the value to be multiplied
    public float healthThreshold = 0.5f;
    public float multiplier = 2f;
}

// The value of a consumer, multiplied when the target is wounded enough (e.g. the Executioner hitting twice as hard
// a unit under half health)
public class WoundedTargetConsumer : AConsumer<WoundedTargetConsumerData>
{
    public override float GetValue()
    {
        float value = new Consumer { data = data, source = source, target = target }.GetValue();
        return IsWounded() ? value * data.multiplier : value;
    }

    bool IsWounded()
    {
        Entity entity = target != null ? target.GetComponent<Entity>() : null;
        return entity != null && entity.health != null && entity.health.percent < data.healthThreshold;
    }
}
