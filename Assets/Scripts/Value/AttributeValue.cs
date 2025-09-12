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
    public override float GetValue(GameObject source)
    {
        return source.GetComponent<AttributeManager>().Get(data.type).Value * data.multiplier;
    }
}
