using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/FlatModifier")]
public class FlatModifierFactory : BuffFactory<AttributeModifierBuff<FlatModifier, FlatModifierData>, FlatModifierData> { }

[Serializable]
public class FlatModifierData : BaseData
{
    public float value;
}

public class FlatModifier : AttributeModifier<FlatModifierData>, IStackableBuff
{
    float _stackedValue = 1f;
    float _lastStackedValue = 1f;

    public override void Init(GameObject source, GameObject target)
    {
        _stackedValue = data.value;
        _lastStackedValue = _stackedValue;
    }

    public override float ApplyModifier()
    {
        return _stackedValue;
    }

    public void Stack(GameObject source, GameObject target)
    {
        if (data.isRelative)
        {
            _lastStackedValue *= data.value;
            _stackedValue += Mathf.Sign(data.value) * Mathf.Abs(_lastStackedValue);
        }
        else
        {
            _stackedValue += data.value;
        }
    }

    public void Unstack(GameObject source, GameObject target)
    {
        if (data.isRelative)
        {
            _lastStackedValue /= data.value;
            _stackedValue -= Mathf.Sign(data.value) * Mathf.Abs(_lastStackedValue);
        }
        else
        {
            _stackedValue -= data.value;
        }
    }
}