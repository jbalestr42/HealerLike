using UnityEngine;

// Turns the consumers sent to a resource into the value it changes by: damage reduced by armor and vulnerability, then
// critical or not, then multiplied
public class ResourceConsumerResolver : IResourceConsumerResolver
{
    Attribute _percentArmor;
    Attribute _flatArmor;
    Attribute _hitArmor;
    Attribute _vulnerability;
    Attribute _criticalChanceResist;
    // The attribute of the owner multiplying the gains (e.g. HealingReceived for the heals of the health), none: the
    // gains stay whole
    readonly AttributeType? _gainMultiplierType;
    Attribute _gainMultiplier;

    public ResourceConsumerResolver(AttributeType? gainMultiplierType = null)
    {
        _gainMultiplierType = gainMultiplierType;
    }

    // The attributes of the owner reducing the damage and resisting the criticals
    protected virtual void InitReductionsAndCriticals(AttributeManager attributeManager)
    {
        _percentArmor = attributeManager.GetOrAdd(AttributeType.PercentArmor);
        _flatArmor = attributeManager.GetOrAdd(AttributeType.FlatArmor);
        _hitArmor = attributeManager.GetOrAdd(AttributeType.HitArmor);
        _vulnerability = attributeManager.GetOrAdd(AttributeType.Vulnerability);
        _criticalChanceResist = attributeManager.GetOrAdd(AttributeType.CriticalChanceResist);
    }

    // The value multiplied when the source rolls a critical, true then
    protected virtual bool TryApplyCritical(ResourceModifier resourceModifier, ref float value)
    {
        bool isCritical = false;
        // The source may be gone since it sent the value (e.g. killed in between): no critical then
        AttributeManager sourceAttributeManager = resourceModifier.source != null ? resourceModifier.source.GetComponent<AttributeManager>() : null;
        bool canBeCritical = sourceAttributeManager != null;
        bool hasCriticalChance = canBeCritical && sourceAttributeManager.Has(AttributeType.CriticalChance);
        // Heals (positive) add their own critical chance to the regular one
        bool hasHealCriticalChance = canBeCritical && value > 0f && sourceAttributeManager.Has(AttributeType.HealCriticalChance);
        if (hasCriticalChance || hasHealCriticalChance)
        {
            float criticalChance = 0f;
            if (hasCriticalChance)
            {
                criticalChance += sourceAttributeManager.Get(AttributeType.CriticalChance).Value;
            }
            if (hasHealCriticalChance)
            {
                criticalChance += sourceAttributeManager.Get(AttributeType.HealCriticalChance).Value;
            }
            // A critical chance from an item alone uses the default multiplier
            Attribute criticalMultiplier = sourceAttributeManager.GetOrAdd(AttributeType.CriticalMultiplier);

            isCritical = Random.Range(0f, 100f) < (criticalChance - _criticalChanceResist.Value);
            if (isCritical)
            {
                value *= criticalMultiplier.Value;
            }
        }
        return isCritical;
    }

    bool CanApplyConsumer(ResourceAttribute resourceAttribute, AConsumer consumer)
    {
        return !resourceAttribute.preventConsumers || consumer.ignoreConsumerPrevention;
    }

    float ApplyConsumerModifiers(AConsumer consumer)
    {
        float value = consumer.GetValue();
        // Only damage (negative) is reduced, never a heal
        if (!consumer.ignoreDamageReduction && value < 0f)
        {
            return ReduceDamage(value);
        }

        return value;
    }

    // The damage (negative) left once the armor and the vulnerability of the owner are applied
    protected virtual float ReduceDamage(float value)
    {
        if (_hitArmor.Value > 0f)
        {
            _hitArmor.BaseValue -= 1f;
            return 0f;
        }
        // Damage is negative: flat armor blocks up to its value per hit, never turning it into a heal
        return Mathf.Min(0f, value + _flatArmor.Value) * (1f - _percentArmor.Value) * (1f + _vulnerability.Value);
    }

    #region IResourceConsumerResolver

    public void Init(AttributeManager attributeManager)
    {
        // A multiplier: the gains are untouched by default, unlike the other attributes starting at 0
        _gainMultiplier = _gainMultiplierType.HasValue ? attributeManager.GetOrAdd(_gainMultiplierType.Value) : null;
        InitReductionsAndCriticals(attributeManager);
    }

    public (float value, bool isCritical) ComputeValue(ResourceAttribute resourceAttribute, ResourceModifier resourceModifier)
    {
        // A single consumer already final (e.g. damage passed on from another unit) makes the whole value so
        bool canBeCritical = resourceModifier.consumers.TrueForAll(consumer => consumer.canBeCritical);
        float value = 0f;
        foreach (AConsumer consumer in resourceModifier.consumers)
        {
            if (CanApplyConsumer(resourceAttribute, consumer))
            {
                value += ApplyConsumerModifiers(consumer);
            }
        }
        resourceModifier.consumers.Clear();

        bool isCritical = canBeCritical && TryApplyCritical(resourceModifier, ref value);
        value *= resourceModifier.multiplier;
        // Gains are positive
        if (value > 0f && _gainMultiplier != null)
        {
            value *= _gainMultiplier.Value;
        }
        return (value, isCritical);
    }

    #endregion
}
