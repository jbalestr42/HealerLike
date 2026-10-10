using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

public enum DurationType
{
    Instant,
    Duration,
    Infinite
}

[InlineEditor]
public abstract class ABuffHandlerFactory : ScriptableObject, ITaggable
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
    public abstract bool stackAcrossSources { get; }
    public abstract List<GameplayTag> tags { get; }

    #region ITaggable

    public bool HasTag(GameplayTag tag) => TagFilter.HasTag(tags, tag);
    public bool HasTag(string tagName) => TagFilter.HasTag(tags, tagName);

    #endregion
}

public class BuffHandlerFactory<BuffHandlerType, DataType> : ABuffHandlerFactory
                                            where BuffHandlerType : ABuffHandler<DataType>, new()
                                            where DataType : BuffHandlerBaseData
{
    [InlineProperty]
    public DataType data;

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
    public override bool stackAcrossSources => data.stackAcrossSources;
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
    // A period has been applied: its time is removed, what went beyond it counts for the next one
    public abstract void ConsumePeriod();
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
    [ShowIf(nameof(hasPeriod))]
    public float periodDuration;
    // A periodic buff lasting more than an instant: its period counts
    bool hasPeriod => durationType != DurationType.Instant && isPeriodic;
    // Stacks a single source can apply (every source together with stackAcrossSources), 0 for no limit: over
    // it, a new application only refreshes the duration
    [HideIf("durationType", DurationType.Instant)]
    [Min(0)]
    public int maxStacks;
    // Every source feeds the same stacks on a target instead of each source having its own (e.g. a poison
    // applied by several enemies), and the handler outlives its sources: it ends with its duration
    [HideIf("durationType", DurationType.Instant)]
    public bool stackAcrossSources;

    [CreateDataButton]
    public List<ABuffFactory> buffFactoryList;

    public GameObject buffEffect; // TODO IBuffEffect ? to manage start and stop visual effect

    // Displayed over the target while the handler is active (no icon = not displayed)
    [Preview(50)]
    public Sprite icon;

    public List<GameplayTag> tags = new List<GameplayTag>();
}

[Serializable]
public abstract class ABuffHandler<DataType> : ABuffHandler where DataType : BuffHandlerBaseData
{
    public DataType data;
}