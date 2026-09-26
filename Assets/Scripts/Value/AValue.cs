using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public abstract class AValue
{
    public abstract float GetValue(GameObject target);
}

[Serializable]
public abstract class AValue<DataType> : AValue, IGameDataSource
{
    [InlineProperty]
    [HideLabel]
    public DataType data;
    public object sourceData { get { return data; } }
}
