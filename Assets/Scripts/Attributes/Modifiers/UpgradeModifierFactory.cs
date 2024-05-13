using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/UpgradeModifier")]
public class UpgradeModifierFactory : BuffFactory<AttributeModifierBuff<UpgradeModifier, UpgradeModifierData>, UpgradeModifierData> { }

[Serializable]
public class UpgradeModifierData : BaseData
{
    public float value;
}

public class UpgradeModifier : AttributeModifier<UpgradeModifierData>, IStackableBuff
{
    public float stacks = 1f;

    public override float ApplyModifier()
    {
        return data.value * stacks;
    }

    public void Stack(GameObject source, GameObject target)
    {
        stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        stacks--;
    }
}