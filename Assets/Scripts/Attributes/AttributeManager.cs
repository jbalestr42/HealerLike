using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributeManager : MonoBehaviour
{
    Dictionary<AttributeType, Attribute> _attributes;

	void Awake()
    {
        _attributes = new Dictionary<AttributeType, Attribute>();
    }
	
	void Update()
    {
        foreach (var attribute in _attributes)
        {
            attribute.Value.Update();
        }
	}

    public Attribute Add(AttributeType type, Attribute attribute)
    {
        if (_attributes.ContainsKey(type))
        {
            Debug.LogError($"This AttributeType '{type}' already exists in the AttributeManager.");
        }
        _attributes.Add(type, attribute);
        return attribute;
    }

    public bool Has(AttributeType type)
    {
        return _attributes.ContainsKey(type);
    }

    public Attribute Get(AttributeType type)
    {
        if (!_attributes.ContainsKey(type))
        {
            Debug.LogError($"This AttributeType '{type}' doesn't exists in the AttributeManager.");
        }
        return _attributes[type];
    }

    // Value an attribute starts from when it's added without one: the multipliers leave the value
    // untouched (heals received) or increase it by half (critical hits), the other attributes start at 0
    public static float GetDefaultValue(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.HealingReceived:
                return 1f;
            case AttributeType.CriticalMultiplier:
                return 1.5f;
            default:
                return 0f;
        }
    }

    public Attribute GetOrAdd(AttributeType type)
    {
        return GetOrAdd(type, GetDefaultValue(type));
    }

    public Attribute GetOrAdd(AttributeType type, float defaultValue)
    {
        if (!_attributes.ContainsKey(type))
        {
            _attributes[type] = new Attribute(defaultValue);
            Debug.Log($"This AttributeType '{type}' doesn't exists in the AttributeManager, it's automatically added.");
        }
        return _attributes[type];
    }
}
