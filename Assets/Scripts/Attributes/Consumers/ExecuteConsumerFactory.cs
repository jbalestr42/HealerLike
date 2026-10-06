using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Consumer/ExecuteConsumer")]
public class ExecuteConsumerFactory : ConsumerFactory<ExecuteConsumer, ExecuteConsumerData> { }

[Serializable]
public class ExecuteConsumerData : ConsumerBaseData
{
    // Health percent (0-1) the target must be strictly below to be executed
    public float healthThreshold = 0.1f;
}

// Kills the target outright when its health is low enough, does nothing otherwise
public class ExecuteConsumer : AConsumer<ExecuteConsumerData>
{
    public override float GetValue()
    {
        Entity entity = target != null ? target.GetComponent<Entity>() : null;
        if (entity == null || entity.health == null || entity.health.percent >= data.healthThreshold)
        {
            return 0f;
        }

        // Its whole max health: still lethal when the hit is reduced (e.g. by a bounce)
        return -entity.health.Max;
    }
}
