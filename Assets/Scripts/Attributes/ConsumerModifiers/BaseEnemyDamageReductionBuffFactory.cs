using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/BaseEnemyDamageReductionBuff")]
public class BaseEnemyDamageReductionBuffFactory : BuffFactory<ConsumerModifierBuff<BaseEnemyDamageReductionBuff, BaseEnemyDamageReductionBuffData>, BaseEnemyDamageReductionBuffData> { }

[Serializable]
public class BaseEnemyDamageReductionBuffData
{
    public AttributeType percentArmorType;
    public AttributeType flatArmorType;
    public AttributeType vulnerabilityType;
}

public class BaseEnemyDamageReductionBuff : AConsumerModifier<BaseEnemyDamageReductionBuffData>
{
    Attribute percentArmor;
    Attribute flatArmor;
    Attribute vulnerability;

    public override void Init(GameObject source, GameObject target)
    {
        percentArmor = target.GetComponent<AttributeManager>().GetOrAdd(data.percentArmorType);
        flatArmor = target.GetComponent<AttributeManager>().GetOrAdd(data.flatArmorType);
        vulnerability = target.GetComponent<AttributeManager>().GetOrAdd(data.vulnerabilityType);
    }

    public override float ApplyController(AConsumer consumer, float incomingDamage)
    {
        return Mathf.Min(0f, incomingDamage + flatArmor.Value) * (1f - percentArmor.Value) * (1f + vulnerability.Value);
    }
}