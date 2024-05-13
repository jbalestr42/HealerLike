using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Attribute
{
    public delegate void OnValueChanged(Attribute attribute);
    event OnValueChanged _onValueChanged;

    [SerializeField] float _baseValue;
    public float BaseValue { get { return _baseValue; } set { _baseValue = value; } }

    float _prevValue;
    float _value;
    public float Value { get { return _value; } }

    Dictionary<GameObject, List<AttributeModifier>> _relativeModifiers = new Dictionary<GameObject, List<AttributeModifier>>();
    Dictionary<GameObject, List<AttributeModifier>> _absoluteModifiers = new Dictionary<GameObject, List<AttributeModifier>>();

    public Attribute()
    {
        _prevValue = 0f;
    }

    public Attribute(float value)
        :this()
    {
        _baseValue = value;
        Update();
    }

    public virtual void Update()
    {
        float relativeBonus = 1f;
        foreach (var kvp in _relativeModifiers)
        {
            List<AttributeModifier> modifiers = kvp.Value;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                relativeBonus *= 1f + modifiers[i].ApplyModifier();
            }
        }

        float absoluteBonus = 0f;
        foreach (var kvp in _absoluteModifiers)
        {
            List<AttributeModifier> modifiers = kvp.Value;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                absoluteBonus += modifiers[i].ApplyModifier();
            }
        }

        _prevValue = _value;
        _value = (_baseValue + absoluteBonus) * relativeBonus;
        _value = Mathf.Max(_value, 0f);

        if (_onValueChanged != null && _prevValue != _value)
        {
            _onValueChanged(this);
        }
    }

    public Attribute Clone()
    {
        return new Attribute(_baseValue);
    }

    public void AddRelativeModifier(GameObject source, AttributeModifier modifier)
    {
        if (!_relativeModifiers.ContainsKey(source))
        {
            _relativeModifiers[source] = new List<AttributeModifier>();
        }
        _relativeModifiers[source].Add(modifier);
    }

    public List<AttributeModifier> GetRelativeModifier(GameObject source)
    {
        return _relativeModifiers[source];
    }

    public void RemoveRelativeModifierFromSource(GameObject source)
    {
        _relativeModifiers[source].Clear();
    }

    public void RemoveRelativeModifier(GameObject source, AttributeModifier modifier)
    {
        _relativeModifiers[source].Remove(modifier);
    }

    public void AddAbsoluteModifier(GameObject source, AttributeModifier modifier)
    {
        if (!_absoluteModifiers.ContainsKey(source))
        {
            _absoluteModifiers[source] = new List<AttributeModifier>();
        }
        _absoluteModifiers[source].Add(modifier);
    }

    public List<AttributeModifier> GetAbsoluteModifier(GameObject source)
    {
        return _absoluteModifiers[source];
    }

    public void RemoveAbsoluteModifierFromSource(GameObject source)
    {
        _absoluteModifiers[source].Clear();
    }

    public void RemoveAbsoluteModifier(GameObject source, AttributeModifier modifier)
    {
        _absoluteModifiers[source].Remove(modifier);
    }

    public void AddOnValueChangedListener(OnValueChanged onValueChanged)
    {
        _onValueChanged += onValueChanged;
    }

    public void RemoveOnValueChangedListener(OnValueChanged onValueChanged)
    {
        _onValueChanged -= onValueChanged;
    }
}