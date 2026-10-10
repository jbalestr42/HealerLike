using System;
using UnityEngine;
using Oisif.Inspector;

[Serializable]
public abstract class AValue
{
    public abstract float GetValue(GameObject target);
}

[Serializable]
public abstract class AValue<DataType> : AValue
{
    [InlineProperty]
    public DataType data;
}
