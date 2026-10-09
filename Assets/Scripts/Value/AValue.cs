using System;
using UnityEngine;

[Serializable]
public abstract class AValue
{
    public abstract float GetValue(GameObject target);
}

[Serializable]
public abstract class AValue<DataType> : AValue
{
    public DataType data;
}
