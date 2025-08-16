using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BaseData
{
    public AttributeType type;
    public AttributeModifierType modifierType;
}

public class AttributeModifierBuff<ModifierType, DataType> : ABuff<DataType>, IStackableBuff
                                        where ModifierType : AttributeModifier<DataType>, new()
                                        where DataType : BaseData
{
    ModifierType _modifier;

    void ComputeInstantValue(Attribute attribute, float value)
    {
        switch (data.modifierType)
        {
            case AttributeModifierType.Add:
                attribute.BaseValue += value;
                break;
            case AttributeModifierType.Multiply:
                attribute.BaseValue *= value;
                break;
            case AttributeModifierType.Override:
                attribute.BaseValue = value;
                break;
        }
    }

    public override void Instant(GameObject source, GameObject target)
    {
        _modifier = new ModifierType() { data = data };
        _modifier.Init(source, target);
        ComputeInstantValue(target.GetComponent<AttributeManager>().GetOrAdd(data.type), _modifier.ApplyModifier());
    }

    public override void Add(GameObject source, GameObject target)
    {
        _modifier = new ModifierType() { data = data };
        _modifier.Init(source, target);
        target.GetComponent<AttributeManager>().GetOrAdd(data.type).AddModifier(data.modifierType, source, _modifier);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        target.GetComponent<AttributeManager>().Get(data.type).RemoveModifier(_modifier);
    }

    public override bool isStackable => _modifier is IStackableBuff;

    public void Stack(GameObject source, GameObject target)
    {
        IStackableBuff stackableModifier = _modifier as IStackableBuff;
        stackableModifier.Stack(source, target);
    }

    public void Unstack(GameObject source, GameObject target)
    {
        IStackableBuff stackableModifier = _modifier as IStackableBuff; 
        stackableModifier.Unstack(source, target);
    }
}