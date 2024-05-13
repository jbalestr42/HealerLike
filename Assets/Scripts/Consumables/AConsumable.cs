using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AConsumableFactory : SerializedScriptableObject
{
    public abstract AConsumable GetConsumable();
    public abstract string title { get; }
}

public class ConsumableFactory<ConsumableType, DataType> : AConsumableFactory
                                            where ConsumableType : AConsumable<DataType>, new()
                                            where DataType : BaseConsumableData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override AConsumable GetConsumable()
    {
        return new ConsumableType() { data = this.data };
    }

    public override string title => data != null ? data.name : "None";
}

[Serializable]
public class BaseConsumableData
{
    [HorizontalGroup("Split", 75)]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public Sprite icon;

    [VerticalGroup("Split/Data")]
    [LabelWidth(100)]
    public string name;

    [VerticalGroup("Split/Data")]
    [LabelWidth(100)]
    public string description;
}

public abstract class AConsumable
{
    public abstract void Use();
    public abstract string title { get; }
    public abstract Sprite icon { get; }
}

public abstract class AConsumable<DataType> : AConsumable where DataType : BaseConsumableData
{
    public DataType data;
    public override string title => data.name;
    public override Sprite icon => data.icon;
}