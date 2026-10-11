using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[Serializable]
public class CurrentHealthValueData
{
    public float multiplier;
    public bool inverse;
    // Factor on top of multiplier when the entity is a boss (tagged Boss), whose large health would make the
    // value huge
    public float bossMultiplier = 1f;
}

[Serializable]
[MovedFrom(false, sourceAssembly: "Assembly-CSharp")]
public class CurrentHealthValue : AValue<CurrentHealthValueData>
{
    public override float GetValue(GameObject target)
    {
        // The health of the entity given: the source or the target, from ConsumerData.valueOwner
        Entity entity = target.GetComponent<Entity>();
        float baseHealth = 0f;

        if (data.inverse)
        {
            baseHealth = entity.health.Max - entity.health.Value;
        }
        else
        {
            baseHealth = entity.health.Value;
        }

        float multiplier = data.multiplier;
        if (entity.HasTag(TagNames.Boss))
        {
            multiplier *= data.bossMultiplier;
        }
        return baseHealth * multiplier;
    }
}
