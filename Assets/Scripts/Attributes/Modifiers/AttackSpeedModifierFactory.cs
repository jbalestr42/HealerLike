using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/AttackSpeedModifier")]
public class AttackSpeedModifierFactory : BuffFactory<AttributeModifierBuff<AttackSpeedModifier, AttackSpeedModifierData>, AttackSpeedModifierData> { }

[Serializable]
public class AttackSpeedModifierData : BaseData
{
    // Speed added by each stack, 1 for +100% (twice as many attacks): meant for a Multiply modifier on a cooldown
    // (e.g. AttackRate)
    public float value;
}

// Speeds up a cooldown, the stacks adding up their speed: +100% then +200% divide the cooldown by 2 then 3
public class AttackSpeedModifier : AttributeModifier<AttackSpeedModifierData>, IStackableBuff
{
    int _stacks = 1;

    public override void Init(GameObject source, GameObject target)
    {
        _stacks = 1;
    }

    // The multiplier of the cooldown, minus 1 (the Multiply modifiers apply 1 + their value)
    public override float ApplyModifier()
    {
        return 1f / (1f + data.value * _stacks) - 1f;
    }

    #region IStackableBuff

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks = Mathf.Max(1, _stacks - 1);
    }

    #endregion
}
