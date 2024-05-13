using System;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class ABuffFactory : SerializedScriptableObject
{
    [HideInInlineEditors]
    public string uniqueID = Guid.NewGuid().ToString();
    public abstract ABuff GetBuff();
}

public class BuffFactory<BuffType, DataType> : ABuffFactory where BuffType : ABuff<DataType>, new()
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override ABuff GetBuff()
    {
        return new BuffType() { data = this.data };
    }
}

public abstract class ABuff
{
    public abstract void Add(GameObject source, GameObject target);
    public abstract void Remove(GameObject source, GameObject target);
    public abstract bool isStackable { get; }
}

public abstract class ABuff<DataType> : ABuff
{
    public DataType data;
    public override bool isStackable => this is IStackableBuff;
}