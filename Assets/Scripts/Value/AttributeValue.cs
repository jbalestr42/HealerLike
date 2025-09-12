using System;
using UnityEngine;

[Serializable]
public class AttributeValueData
{
    public AttributeType type;
    public float multiplier = 1f;
}

[Serializable]
public class AttributeValue : AValue<AttributeValueData>
{
    public override float GetValue(GameObject target)
    {
        return target.GetComponent<AttributeManager>().Get(data.type).Value * data.multiplier;
    }
}
