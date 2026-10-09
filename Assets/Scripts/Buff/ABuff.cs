using System;
using UnityEngine;
using Oisif.Inspector;

[InlineEditor]
public abstract class ABuffFactory : Sirenix.OdinInspector.SerializedScriptableObject
{
    [HideInInlineEditors]
    public string uniqueID = Guid.NewGuid().ToString();
    public abstract ABuff GetBuff(ABuffHandler buffHandler);
    // Data given to every buff created, to describe the buff without instantiating it
    public abstract object buffData { get; }
}

public class BuffFactory<BuffType, DataType> : ABuffFactory where BuffType : ABuff<DataType>, new()
{
    public DataType data;

    public override ABuff GetBuff(ABuffHandler buffHandler)
    {
        return new BuffType() { data = this.data, buffHandler = buffHandler };
    }

    public override object buffData => data;
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
public abstract class ABuff<DataType> : ABuff
{
    public DataType data;
    public override bool isStackable => this is IStackableBuff;
}