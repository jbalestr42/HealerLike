using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/BuffHandler/BuffHandler")]
public class BuffHandlerFactory : BuffHandlerFactory<BuffHandler, BuffHandlerData> {}

[Serializable]
public class BuffHandlerData : BuffHandlerBaseData
{
}

public class BuffHandler : ABuffHandler<BuffHandlerData>
{
    public float durationTimer;
    public float periodDurationTimer;

    public override void Refresh(GameObject source, GameObject target)
    {
        Debug.Log("[BuffHandler] Refresh " + data.buffFactory.name);
        durationTimer = 0f;
        periodDurationTimer = 0f;
    }

    public override void Start(GameObject source, GameObject target)
    {
        Debug.Log("[BuffHandler] Start " + data.buffFactory.name);
        durationTimer = 0f;
        periodDurationTimer = 0f;
    }

    public override void Update(float deltaTime)
    {
        if (hasDuration)
        {
            durationTimer += deltaTime;
            if (data.durationType == DurationType.Duration && durationTimer > data.duration)
            {
                durationTimer = data.duration;
            }
            
            if (data.isPeriodic)
            {
                periodDurationTimer += deltaTime;
                if (periodDurationTimer > data.periodDuration)
                {
                    periodDurationTimer = data.periodDuration;
                }
            }
        }
    }

    public override void Stop(GameObject source, GameObject target)
    {
        Debug.Log("[BuffHandler] Stop " + data.buffFactory.name);
    }

    public override void ResetDuration() => durationTimer = 0f;
    public override void ResetPeriodDuration() => periodDurationTimer = 0f;
    public override DurationType durationType => data.durationType;
    public override float duration => data.duration;
    public override bool hasDuration => data.durationType == DurationType.Duration || data.durationType == DurationType.Infinite;
    public override bool isDone =>
        data.durationType switch
        {
            DurationType.Instant => true,
            DurationType.Duration => durationTimer >= data.duration,
            DurationType.Infinite => false,
            _ => false
        };
    public override bool isPeriodDone =>
        data.durationType switch
        {
            DurationType.Instant => true,
            DurationType.Duration or DurationType.Infinite => periodDurationTimer >= data.periodDuration,
            _ => false
        };
}