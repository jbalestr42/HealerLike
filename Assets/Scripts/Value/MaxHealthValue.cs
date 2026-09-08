using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[Serializable]
public class MaxHealthValueData
{
    public float multiplier;
    public bool inverse;
}

[Serializable]
[MovedFrom(false, sourceAssembly: "Assembly-CSharp")]
public class MaxHealthValue : AValue<MaxHealthValueData>
{
    public override float GetValue(GameObject target)
    {
        return target.GetComponent<Entity>().health.Max * data.multiplier;
    }
}
