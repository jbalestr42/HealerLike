using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/PercentArmorDamageReductionBuff")]
public class PercentArmorDamageReductionBuffFactory : BuffFactory<ConsumerModifierBuff<PercentArmorDamageReductionBuff, PercentArmorDamageReductionBuffData>, PercentArmorDamageReductionBuffData> { }

[Serializable]
public class PercentArmorDamageReductionBuffData
{
    public AttributeType type;
}

public class PercentArmorDamageReductionBuff : AConsumerModifier<PercentArmorDamageReductionBuffData>
{
    Attribute attribute;

    public override void Init(GameObject source, GameObject target)
    {
        attribute = target.GetComponent<AttributeManager>().Get(data.type);
    }

    public override float ApplyController(AConsumer consumer, float incomingDamage)
    {
        return incomingDamage * (1f - attribute.Value);
    }
}