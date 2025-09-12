using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class AValue : SerializedScriptableObject
{
    public abstract float GetValue(GameObject source);
}

public abstract class AValue<DataType> : AValue
{
    [InlineProperty]
    [HideLabel]
    public DataType data;
}
