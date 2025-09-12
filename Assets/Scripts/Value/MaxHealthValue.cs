using System;
using UnityEngine;

[Serializable]
public class MaxHealthValueData
{
    public float multiplier;
    public bool inverse;
}

[Serializable]
public class MaxHealthValue : AValue<MaxHealthValueData>
{
    public override float GetValue(GameObject target)
    {
        return target.GetComponent<Entity>().health.Max * data.multiplier;
    }
}
