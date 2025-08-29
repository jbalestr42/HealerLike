using System.Collections.Generic;
using UnityEngine;

public class ResourceConsumerResolver
{
    Attribute _percentArmor;
    Attribute _flatArmor;
    Attribute _hitArmor;
    Attribute _vulnerability;
    Attribute _criticalChanceResist;

    public void Init(AttributeManager attributeManager)
    {
        _percentArmor = attributeManager.GetOrAdd(AttributeType.PercentArmor);
        _flatArmor = attributeManager.GetOrAdd(AttributeType.FlatArmor);
        _hitArmor = attributeManager.GetOrAdd(AttributeType.HitArmor);
        _vulnerability = attributeManager.GetOrAdd(AttributeType.Vulnerability);
        _criticalChanceResist = attributeManager.GetOrAdd(AttributeType.CriticalChanceResist);
    }

    public (float value, bool isCritical) ComputeValue(ResourceAttribute resourceAttribute, ResourceModifier resourceModifier)
    {
        float value = 0f;
        foreach (AConsumer consumer in resourceModifier.consumers)
        {
            if (CanApplyConsumer(resourceAttribute, consumer))
            {
                value += ApplyConsumerModifiers(consumer);
            }
        }
        resourceModifier.consumers.Clear();

        bool isCritical = false;
        AttributeManager sourceAttributeManager = resourceModifier.source.GetComponent<AttributeManager>();
        if (sourceAttributeManager.Has(AttributeType.CriticalChance))
        {
            Attribute criticalChance = sourceAttributeManager.Get(AttributeType.CriticalChance);
            Attribute criticalMultiplier = sourceAttributeManager.Get(AttributeType.CriticalMultiplier);

            isCritical = Random.Range(0f, 100f) < (criticalChance.Value - _criticalChanceResist.Value);
            if (isCritical)
            {
                value *= criticalMultiplier.Value;
            }
        }

        value *= resourceModifier.multiplier;
        return (value, isCritical);
    }

    bool CanApplyConsumer(ResourceAttribute resourceAttribute, AConsumer consumer)
    {
        return !resourceAttribute.preventConsumers || consumer.ignoreConsumerPrevention;
    }

    float ApplyConsumerModifiers(AConsumer consumer)
    {
        float value = consumer.GetValue();
        if (!consumer.ignoreDamageReduction)
        {
            if (_hitArmor.Value > 0f)
            {
                _hitArmor.BaseValue -= 1f;
                return 0f;
            }
            return Mathf.Min(0f, value - _flatArmor.Value) * (1f - _percentArmor.Value) * (1f + _vulnerability.Value);
        }

        return value;
    }
}
