using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class ABuffHandlerFactory : SerializedScriptableObject
{
    [HideInInlineEditors]
    public string uniqueID = Guid.NewGuid().ToString();
    public abstract ABuffHandler GetBuffHandler();
    public abstract ABuffFactory GetBuffFactory();
    public abstract GameObject GetBuffEffect();
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
}

public abstract class ABuffHandler
{
    public abstract bool IsDone();
    public abstract void Start(GameObject source, GameObject target);
    public abstract void Update(float deltaTime);
    public abstract void Stop(GameObject source, GameObject target);
    public abstract void Refresh(GameObject source, GameObject target);
}

public class BuffHandlerBaseData
{
    [CreateDataButton]
    public ABuffFactory buffFactory;

    [AssetsOnly]
    public GameObject buffEffect; // TODO IBuffEffect ? to manage start and stop visual effect
}

public abstract class ABuffHandler<DataType> : ABuffHandler where DataType : BuffHandlerBaseData
{
    public DataType data;
}