using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class AConsumerFactory : SerializedScriptableObject
{
    public abstract AConsumer GetConsumer(GameObject source, GameObject target);
}

public class ConsumerFactory<ConsumerType, DataType> : AConsumerFactory
                                            where ConsumerType : AConsumer<DataType>, new()
                                            where DataType : ConsumerBaseData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

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
    public abstract bool ignoreConsumerModifier { get; }
    public abstract bool ignoreConsumerPrevention { get; }
}

public class ConsumerBaseData
{
    public bool ignoreConsumerModifier;
    public bool ignoreConsumerPrevention;
}

public abstract class AConsumer<DataType> : AConsumer where DataType : ConsumerBaseData
{
    public DataType data;
    public override bool ignoreConsumerModifier => data.ignoreConsumerModifier;
    public override bool ignoreConsumerPrevention => data.ignoreConsumerPrevention;
}