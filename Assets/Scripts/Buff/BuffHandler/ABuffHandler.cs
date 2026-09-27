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
    public abstract Sprite icon { get; }
    public abstract DurationType durationType { get; }
    public abstract float duration { get; }
    public abstract bool hasDuration { get; }
    public abstract int maxStacks { get; }
    public abstract List<GameplayTag> tags { get; }
}

public class BuffHandlerFactory<BuffHandlerType, DataType> : ABuffHandlerFactory, IGameDataSource
                                            where BuffHandlerType : ABuffHandler<DataType>, new()
                                            where DataType : BuffHandlerBaseData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;
    public object sourceData { get { return data; } }

    public override ABuffHandler GetBuffHandler()
    {
        return new BuffHandlerType() { data = this.data };
    }

    public override List<ABuffFactory> buffFactoryList => data.buffFactoryList;
    public override GameObject buffEffect => data.buffEffect;
    public override Sprite icon => data.icon;
    public override DurationType durationType => data.durationType;
    public override float duration => data.duration;
    public override bool hasDuration => data.durationType != DurationType.Instant;
    public override int maxStacks => data.maxStacks;
    public override List<GameplayTag> tags => data.tags;
}

[Serializable]
public abstract class ABuffHandler
{
    public abstract void Start(GameObject source, GameObject target);
    public abstract void Update(float deltaTime);
    public abstract void Stop(GameObject source, GameObject target);
    public abstract void Refresh(GameObject source, GameObject target);
    public abstract void ResetPeriodDuration();
    public abstract DurationType durationType { get; }
    public abstract float duration { get; }
    // Time left before the handler stops, 0 when it has no limited duration
    public abstract float remainingDuration { get; }
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
    // Stacks a single source can apply, 0 for no limit: over it, a new application only refreshes the duration
    [HideIf("durationType", DurationType.Instant)]
    [MinValue(0)]
    public int maxStacks;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffFactory>, ABuffFactory>(buffFactoryList)")]
    public List<ABuffFactory> buffFactoryList;

    [AssetsOnly]
    public GameObject buffEffect; // TODO IBuffEffect ? to manage start and stop visual effect

    // Displayed over the target while the handler is active (no icon = not displayed)
    [PreviewField(50)]
    [AssetsOnly]
    public Sprite icon;

    public List<GameplayTag> tags = new List<GameplayTag>();
}

[Serializable]
public abstract class ABuffHandler<DataType> : ABuffHandler, IGameDataSource where DataType : BuffHandlerBaseData
{
    public DataType data;
    public object sourceData { get { return data; } }
}