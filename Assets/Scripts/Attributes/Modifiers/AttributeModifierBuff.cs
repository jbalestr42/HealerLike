using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BaseData
{
    public AttributeType type;
    public bool isRelative;
}

public class AttributeModifierBuff<ModifierType, DataType> : ABuff<DataType>, IStackableBuff
                                        where ModifierType : AttributeModifier<DataType>, new()
                                        where DataType : BaseData
{
    ModifierType _modifier;

    public override void Add(GameObject source, GameObject target)
    {
        _modifier = new ModifierType() { data = data };
        _modifier.Init(source, target);
        if (data.isRelative)
        {
            target.GetComponent<AttributeManager>().GetOrAdd(data.type).AddRelativeModifier(source, _modifier);
        }
        else
        {
            target.GetComponent<AttributeManager>().GetOrAdd(data.type).AddAbsoluteModifier(source, _modifier);
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (data.isRelative)
        {
            target.GetComponent<AttributeManager>().Get(data.type).RemoveRelativeModifier(source, _modifier);
        }
        else
        {
            target.GetComponent<AttributeManager>().Get(data.type).RemoveAbsoluteModifier(source, _modifier);
        }
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