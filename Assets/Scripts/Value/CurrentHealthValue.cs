using System;
using UnityEngine;

[Serializable]
public class CurrentHealthValueData
{
    public float multiplier;
    public bool inverse;
}

[Serializable]
public class CurrentHealthValue : AValue<CurrentHealthValueData>
{
    public override float GetValue(GameObject target)
    {
        // Here target refere to owner, how do we do if we want the health of the real target?
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
