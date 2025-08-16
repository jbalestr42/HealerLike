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
}

public class TimeModifier : AttributeModifier<TimeModifierData>, IStackableBuff
{
    float _start = 0f;
    float _duration = 1f;

    public TimeModifier()
    {
        _start = Time.time;
        _duration = buffHandler.hasDuration ? buffHandler.duration : 1f;
    }

    public override float ApplyModifier()
    {
        return data.value * (1f - GetRatio());
    }

    float GetRatio()
    {
        float ratio = (Time.time - _start) / _duration;
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
