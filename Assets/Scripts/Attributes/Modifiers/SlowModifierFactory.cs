using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/SlowModifier")]
public class SlowModifierFactory : BuffFactory<AttributeModifierBuff<SlowModifier, SlowModifierData>, SlowModifierData> { }

[Serializable]
public class SlowModifierData : BaseData
{
    public float value;
}

public class SlowModifier : AttributeModifier<SlowModifierData>, IStackableBuff
{
    float _start = 0f;
    float _stackFactor = 1f;
    float _stacks = 1f;
    float _duration = 1f;
    // An infinite handler has no duration to fade over: the modifier keeps its full value
    bool _isPermanent = false;

    public override void Init(GameObject source, GameObject target)
    {
        _start = Time.time;
        _duration = buffHandler.hasDuration ? buffHandler.duration : 1f;
        _isPermanent = buffHandler.durationType == DurationType.Infinite;
    }

    public override float ApplyModifier()
    {
        return data.value * _stackFactor * (1f - GetRatio());
    }

    float GetRatio()
    {
        if (_isPermanent)
        {
            return 0f;
        }
        return Mathf.Clamp01((Time.time - _start) / _duration);
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
        _stackFactor = GetStackFactor(_stacks);
        _start = Time.time;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
        _stackFactor = GetStackFactor(_stacks);
    }

    // 1 for a single stack, then grows with diminishing returns (~1.35 at 2 stacks, ~1.55 at 3)
    static float GetStackFactor(float stacks)
    {
        return Mathf.Log(stacks) / 2f + 1f;
    }
}
