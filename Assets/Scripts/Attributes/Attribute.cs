using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttributeModifierType
{
    Add,
    Multiply,
    Override
}

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

    public class SourceModifier
    {
        public GameObject source;
        public AttributeModifier modifier;
    }

    Dictionary<AttributeModifierType, List<SourceModifier>> _modifiers = new Dictionary<AttributeModifierType, List<SourceModifier>>();

    public Attribute()
    {
        _prevValue = 0f;
        _modifiers[AttributeModifierType.Add] = new List<SourceModifier>();
        _modifiers[AttributeModifierType.Multiply] = new List<SourceModifier>();
        _modifiers[AttributeModifierType.Override] = new List<SourceModifier>();
    }

    public Attribute(float value)
        :this()
    {
        _baseValue = value;
        Update();
    }

    public virtual float ComputeValue(Attribute attribute)
    {
        // If there is multiple override modifiers, only get the last one
        if (_modifiers[AttributeModifierType.Override].Count > 0)
        {
            return _modifiers[AttributeModifierType.Override][^1].modifier.ApplyModifier();
        }

        float multiplicative = 1f;
        foreach (var sourceModifier in _modifiers[AttributeModifierType.Multiply])
        {
            multiplicative *= 1f + sourceModifier.modifier.ApplyModifier();
        }

        float additive = 0f;
        foreach (var sourceModifier in _modifiers[AttributeModifierType.Add])
        {
            additive += sourceModifier.modifier.ApplyModifier();
        }

        return (attribute.BaseValue + additive) * multiplicative;
    }

    public void Update()
    {
        _prevValue = _value;
        _value = ComputeValue(this);
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

    public void AddModifier(AttributeModifierType type, GameObject source, AttributeModifier modifier)
    {
        _modifiers[type].Add(new SourceModifier { source = source, modifier = modifier });
    }

    public List<SourceModifier> GetModifiers(AttributeModifierType type)
    {
        return _modifiers[type];
    }

    public void RemoveModifiersBySource(AttributeModifierType type, GameObject source)
    {
        _modifiers[type].RemoveAll(x => x.source == source);
    }

    public void RemoveModifier(AttributeModifierType type, AttributeModifier modifier)
    {
        _modifiers[type].RemoveAll(x => x.modifier == modifier);
    }

    public void RemoveModifiersBySource(GameObject source)
    {
        RemoveModifiersBySource(AttributeModifierType.Add, source);
        RemoveModifiersBySource(AttributeModifierType.Multiply, source);
        RemoveModifiersBySource(AttributeModifierType.Override, source);
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        RemoveModifier(AttributeModifierType.Add, modifier);
        RemoveModifier(AttributeModifierType.Multiply, modifier);
        RemoveModifier(AttributeModifierType.Override, modifier);
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