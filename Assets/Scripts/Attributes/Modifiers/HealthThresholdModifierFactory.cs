using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/HealthThresholdModifier")]
public class HealthThresholdModifierFactory : BuffFactory<AttributeModifierBuff<HealthThresholdModifier, HealthThresholdModifierData>, HealthThresholdModifierData> { }

[Serializable]
public class HealthThresholdModifierData : BaseData
{
    public float value;
    // Health percent (0-1) the target must stay strictly above for the modifier to apply
    public float threshold;
}

public class HealthThresholdModifier : AttributeModifier<HealthThresholdModifierData>
{
    ResourceAttribute _health;

    public override void Init(GameObject source, GameObject target)
    {
        _health = target.GetComponent<Entity>().health;
    }

    public override float ApplyModifier()
    {
        // All or nothing: the whole value above the threshold, no bonus at or below it
        return _health.percent > data.threshold ? data.value : 0f;
    }
}
