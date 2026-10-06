using System;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class AConsumerFactory : SerializedScriptableObject
{
    public abstract AConsumer GetConsumer(GameObject source, GameObject target);
}

public class ConsumerFactory<ConsumerType, DataType> : AConsumerFactory, IGameDataSource
                                            where ConsumerType : AConsumer<DataType>, new()
                                            where DataType : ConsumerBaseData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;
    public object sourceData { get { return data; } }

    public override AConsumer GetConsumer(GameObject source, GameObject target)
    {
        return new ConsumerType() { data = this.data, source = source, target = target };
    }
}

public abstract class AConsumer
{
    public GameObject source;
    public GameObject target;

    public abstract float GetValue();
    public abstract bool ignoreDamageReduction { get; }
    public abstract bool ignoreConsumerPrevention { get; }
    // False for a value already final, e.g. damage passed on from another unit
    public virtual bool canBeCritical => true;
}

[Serializable]
public class ConsumerBaseData
{
    public bool ignoreDamageReduction;
    public bool ignoreConsumerPrevention;
    // False for a value that must stay as set, e.g. the mana and heal of a rest room
    public bool canBeCritical = true;
}

public abstract class AConsumer<DataType> : AConsumer, IGameDataSource where DataType : ConsumerBaseData
{
    public DataType data;
    public object sourceData { get { return data; } }
    public override bool ignoreDamageReduction => data.ignoreDamageReduction;
    public override bool ignoreConsumerPrevention => data.ignoreConsumerPrevention;
    public override bool canBeCritical => data.canBeCritical;
}