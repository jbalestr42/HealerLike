using System;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class ABuffFactory : SerializedScriptableObject
{
    [HideInInlineEditors]
    public string uniqueID = Guid.NewGuid().ToString();
    public abstract ABuff GetBuff(ABuffHandler buffHandler);
}

public class BuffFactory<BuffType, DataType> : ABuffFactory, IGameDataSource where BuffType : ABuff<DataType>, new()
{
    [InlineProperty]
    [HideLabel]
    public DataType data;
    public object sourceData { get { return data; } }

    public override ABuff GetBuff(ABuffHandler buffHandler)
    {
        return new BuffType() { data = this.data, buffHandler = buffHandler };
    }
}

[Serializable]
public abstract class ABuff
{
    public ABuffHandler buffHandler { get; set; }

    public abstract void Instant(GameObject source, GameObject target);
    public abstract void Add(GameObject source, GameObject target);
    public abstract void Remove(GameObject source, GameObject target);
    public abstract bool isStackable { get; }
}

[Serializable]
public abstract class ABuff<DataType> : ABuff, IGameDataSource
{
    public DataType data;
    public object sourceData { get { return data; } }
    public override bool isStackable => this is IStackableBuff;
}