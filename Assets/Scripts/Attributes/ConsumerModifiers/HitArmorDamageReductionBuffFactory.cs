using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/HitArmorDamageReductionBuff")]
public class HitArmorDamageReductionBuffFactory : BuffFactory<ConsumerModifierBuff<HitArmorDamageReductionBuff, HitArmorDamageReductionBuffData>, HitArmorDamageReductionBuffData> { }

[Serializable]
public class HitArmorDamageReductionBuffData
{
    public AttributeType type;
}

public class HitArmorDamageReductionBuff : AConsumerModifier<HitArmorDamageReductionBuffData>
{
    int _hitCount = 0;

    public override void Init(GameObject source, GameObject target)
    {
        Attribute attribute = target.GetComponent<AttributeManager>().Get(data.type);
        _hitCount = (int)attribute.Value;
    }

    public override float ApplyController(AConsumer consumer, float incomingDamage)
    {
        // This is not working for now because in the ResourceAttirbute we apply all controllers each frame, we muste make a difference between ongoing effect and sinelge time effect (like Damage)
        if (_hitCount > 0)
        {
            _hitCount--;
            return 0f;
        }
        return incomingDamage;
    }
}