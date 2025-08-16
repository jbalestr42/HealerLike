using UnityEngine;

public class ResourceConsumerResolver
{
    Attribute _percentArmor;
    Attribute _flatArmor;
    Attribute _hitArmor;
    Attribute _vulnerability;

    public void Init(AttributeManager attributeManager)
    {
        _percentArmor = attributeManager.GetOrAdd(AttributeType.PercentArmor);
        _flatArmor = attributeManager.GetOrAdd(AttributeType.FlatArmor);
        _hitArmor = attributeManager.GetOrAdd(AttributeType.HitArmor);
        _vulnerability = attributeManager.GetOrAdd(AttributeType.Vulnerability);
    }

    public float ComputeValue(ResourceAttribute resourceAttribute, ResourceModifier resourceModifier)
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

        value *= resourceModifier.multiplier;
        return value;
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
