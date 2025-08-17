using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum DurationType
{
    Instant,
    Duration,
    Infinite
}

[InlineEditor]
public abstract class ABuffHandlerFactory : SerializedScriptableObject
{
    [HideInInlineEditors]
    public string uniqueID = Guid.NewGuid().ToString();
    public abstract ABuffHandler GetBuffHandler();
    public abstract ABuffFactory GetBuffFactory();
    public abstract GameObject GetBuffEffect();
    public abstract DurationType durationType { get; }
    public abstract float duration { get; }
    public abstract bool hasDuration { get; }
}

public class BuffHandlerFactory<BuffHandlerType, DataType> : ABuffHandlerFactory
                                            where BuffHandlerType : ABuffHandler<DataType>, new()
                                            where DataType : BuffHandlerBaseData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override ABuffHandler GetBuffHandler()
    {
        return new BuffHandlerType() { data = this.data };
    }

    public override ABuffFactory GetBuffFactory()
    {
        return data.buffFactory;
    }

    public override GameObject GetBuffEffect()
    {
        return data.buffEffect;
    }
    public override DurationType durationType => data.durationType;
    public override float duration => data.duration;
    public override bool hasDuration => data.durationType != DurationType.Instant;
}

public abstract class ABuffHandler
{
    public abstract void Start(GameObject source, GameObject target);
    public abstract void Update(float deltaTime);
    public abstract void Stop(GameObject source, GameObject target);
    public abstract void Refresh(GameObject source, GameObject target);
    public abstract void ResetDuration();
    public abstract void ResetPeriodDuration();
    public abstract DurationType durationType { get; }
    public abstract float duration { get; }
    public abstract bool hasDuration { get; }
    public abstract bool isDone { get; }
    public abstract bool isPeriodDone { get; }
}

public class BuffHandlerBaseData
{
    public DurationType durationType;

    //TODO hide if durationType is instant
    [HideIf("durationType", DurationType.Instant)]
    public float duration;
    //TODO hide if durationType is instant
    [HideIf("durationType", DurationType.Instant)]
    public bool isPeriodic;
    //TODO hide if isPeriodic is false
    [ShowIf("@this.durationType != DurationType.Instant && isPeriodic")]
    public float periodDuration;

    [CreateDataButton]
    public ABuffFactory buffFactory;

    [AssetsOnly]
    public GameObject buffEffect; // TODO IBuffEffect ? to manage start and stop visual effect
}

public abstract class ABuffHandler<DataType> : ABuffHandler where DataType : BuffHandlerBaseData
{
    public DataType data;
}