using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[Serializable]
public class CurrentHealthValueData
{
    public float multiplier;
    public bool inverse;
}

[Serializable]
[MovedFrom(false, sourceAssembly: "Assembly-CSharp")]
public class CurrentHealthValue : AValue<CurrentHealthValueData>
{
    public override float GetValue(GameObject target)
    {
        // The health of the entity given: the source or the target, from ConsumerData.valueOwner
        float baseHealth = 0f;
        
        if (data.inverse)
        {
            baseHealth = target.GetComponent<Entity>().health.Max - target.GetComponent<Entity>().health.Value;
        }
        else
        {
            baseHealth = target.GetComponent<Entity>().health.Value;
        }
        return baseHealth * data.multiplier;
    }
}
