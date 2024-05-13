using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/HPBasedModifier")]
public class HPBasedModifierFactory : BuffFactory<AttributeModifierBuff<HPBasedModifier, HPBasedModifierData>, HPBasedModifierData> { }

[Serializable]
public class HPBasedModifierData : BaseData
{
    public float factor;
    public float threshold;
}

public class HPBasedModifier : AttributeModifier<HPBasedModifierData>
{
    ResourceAttribute _health;

    public override void Init(GameObject source, GameObject target)
    {
        _health = target.GetComponent<Entity>().health;
    }

    public override float ApplyModifier()
    {
        return 1f + ((1f - Mathf.Clamp01(_health.percent / data.threshold)) * data.factor);
    }
}
