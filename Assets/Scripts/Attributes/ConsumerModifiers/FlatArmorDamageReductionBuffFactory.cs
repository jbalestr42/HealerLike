using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/FlatArmorDamageReductionBuff")]
public class FlatArmorDamageReductionBuffFactory : BuffFactory<ConsumerModifierBuff<FlatArmorDamageReductionBuff, FlatArmorDamageReductionBuffData>, FlatArmorDamageReductionBuffData> { }

[Serializable]
public class FlatArmorDamageReductionBuffData
{
    public AttributeType type;
}

public class FlatArmorDamageReductionBuff : AConsumerModifier<FlatArmorDamageReductionBuffData>
{
    Attribute attribute;

    public override void Init(GameObject source, GameObject target)
    {
        attribute = target.GetComponent<AttributeManager>().Get(data.type);
    }

    public override float ApplyController(AConsumer consumer, float incomingDamage)
    {
        return Mathf.Min(0f, incomingDamage + attribute.Value);
    }
}