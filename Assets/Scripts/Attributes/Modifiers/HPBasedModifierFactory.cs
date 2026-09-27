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
        // Only the bonus: a Multiply modifier is applied as x(1 + value), so 0 leaves the attribute
        // unchanged above the threshold and it grows linearly up to x(1 + factor) at 0 health
        return (1f - Mathf.Clamp01(_health.percent / data.threshold)) * data.factor;
    }
}
