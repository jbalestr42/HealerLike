using System;
using System.Collections.Generic;
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
    public abstract List<ABuffFactory> buffFactoryList { get; }
    public abstract GameObject buffEffect { get; }
    public abstract DurationType durationType { get; }
    public abstract float duration { get; }
    public abstract bool hasDuration { get; }
    public abstract List<GameplayTag> tags { get; }
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

    public override List<ABuffFactory> buffFactoryList => data.buffFactoryList;
    public override GameObject buffEffect => data.buffEffect;
    public override DurationType durationType => data.durationType;
    public override float duration => data.duration;
    public override bool hasDuration => data.durationType != DurationType.Instant;
    public override List<GameplayTag> tags => data.tags;
}

public abstract class ABuffHandler
{
    public abstract void Start(GameObject source, GameObject target);
    public abstract void Update(float deltaTime);
    public abstract void Stop(GameObject source, GameObject target);
    public abstract void Refresh(GameObject source, GameObject target);
    public abstract void ResetPeriodDuration();
    public abstract DurationType durationType { get; }
    public abstract float duration { get; }
    public abstract bool hasDuration { get; }
    public abstract bool isPeriodic { get; }
    public abstract bool isDone { get; }
    public abstract bool isPeriodDone { get; }
}

public class BuffHandlerBaseData
{
    public DurationType durationType;

    [ShowIf("durationType", DurationType.Duration)]
    public float duration;
    [HideIf("durationType", DurationType.Instant)]
    public bool isPeriodic;
    [ShowIf("@this.durationType != DurationType.Instant && isPeriodic")]
    public float periodDuration;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffFactory>, ABuffFactory>(buffFactoryList)")]
    public List<ABuffFactory> buffFactoryList;

    [AssetsOnly]
    public GameObject buffEffect; // TODO IBuffEffect ? to manage start and stop visual effect

    public List<GameplayTag> tags = new List<GameplayTag>();
}

public abstract class ABuffHandler<DataType> : ABuffHandler where DataType : BuffHandlerBaseData
{
    public DataType data;
}