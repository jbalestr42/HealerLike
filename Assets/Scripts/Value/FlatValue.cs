using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[Serializable]
public class FlatValueData
{
    [HideLabel]
    public float value;
}

[Serializable]
[MovedFrom(false, sourceAssembly: "Assembly-CSharp")]
public class FlatValue : AValue<FlatValueData>
{
    public override float GetValue(GameObject target)
    {
        return data.value;
    }
}
