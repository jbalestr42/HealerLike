using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class FlatValueData
{
    [HideLabel]
    public float value;
}

[Serializable]
public class FlatValue : AValue<FlatValueData>
{
    public override float GetValue(GameObject source)
    {
        return data.value;
    }
}
