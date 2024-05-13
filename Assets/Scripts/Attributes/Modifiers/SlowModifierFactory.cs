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
    public float duration;
}

public class SlowModifier : AttributeModifier<SlowModifierData>, IStackableBuff
{
    float _start = 0f;
    float _stackFactor = 1f;
    float _stacks = 1f;

    public SlowModifier()
    {
        _start = Time.time;
    }

    public override float ApplyModifier()
    {
        return data.value * _stackFactor * (1f - GetRatio());
    }

    float GetRatio()
    {
        return Mathf.Clamp01((Time.time - _start) / data.duration);
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
        _stackFactor = Mathf.Log(_stacks + 1) / 2f + 1f;
        _start = Time.time;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
        _stackFactor = Mathf.Log(_stacks + 1) / 2f + 1f;
    }
}
