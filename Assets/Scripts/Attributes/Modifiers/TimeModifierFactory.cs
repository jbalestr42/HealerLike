using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/TimeModifier")]
public class TimeModifierFactory : BuffFactory<AttributeModifierBuff<TimeModifier, TimeModifierData>, TimeModifierData> { }

[Serializable]
public class TimeModifierData : BaseData
{
    public float value;
    public float duration;
}

public class TimeModifier : AttributeModifier<TimeModifierData>, IStackableBuff
{
    float _start = 0f;

    public TimeModifier()
    {
        _start = Time.time;
    }

    public override float ApplyModifier()
    {
        return data.value * (1f - GetRatio());
    }

    float GetRatio()
    {
        float ratio = (Time.time - _start) / data.duration;
        ratio = Mathf.Clamp(ratio, 0f, 1f);
        return ratio;
    }

    public void Stack(GameObject source, GameObject target)
    {
        _start = Time.time;
    }

    public void Unstack(GameObject source, GameObject target)
    {
    }
}
