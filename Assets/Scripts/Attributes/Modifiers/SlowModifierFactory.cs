using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/SlowModifier")]
public class SlowModifierFactory : BuffFactory<AttributeModifierBuff<SlowModifier, SlowModifierData>, SlowModifierData> { }

[Serializable]
public class SlowModifierData : BaseData
{
    public float value;
}

// Fades over the duration of its buff, from its full value when the buff is given or refreshed (e.g. Chilled hit again
// while chilled) to nothing when the buff ends
public class SlowModifier : AttributeModifier<SlowModifierData>, IStackableBuff
{
    float _stackFactor = 1f;
    float _stacks = 1f;

    public override void Init(GameObject source, GameObject target)
    {
    }

    public override float ApplyModifier()
    {
        return data.value * _stackFactor * (1f - GetRatio());
    }

    // The time spent of the buff, which restarts on each refresh; an infinite buff has no duration to fade over
    float GetRatio()
    {
        if (buffHandler.durationType != DurationType.Duration || buffHandler.duration <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(1f - buffHandler.remainingDuration / buffHandler.duration);
    }

    // 1 for a single stack, then grows with diminishing returns (~1.35 at 2 stacks, ~1.55 at 3)
    static float GetStackFactor(float stacks)
    {
        return Mathf.Log(stacks) / 2f + 1f;
    }

    #region IStackableBuff

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
        _stackFactor = GetStackFactor(_stacks);
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
        _stackFactor = GetStackFactor(_stacks);
    }

    #endregion
}
