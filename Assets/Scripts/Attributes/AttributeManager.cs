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

    public Attribute Get(AttributeType type)
    {
        if (!_attributes.ContainsKey(type))
        {
            Debug.LogError($"This AttributeType '{type}' doesn't exists in the AttributeManager.");
        }
        return _attributes[type];
    }

    public Attribute GetOrAdd(AttributeType type)
    {
        if (!_attributes.ContainsKey(type))
        {
            _attributes[type] = new Attribute();
            Debug.Log($"This AttributeType '{type}' doesn't exists in the AttributeManager, it's automatically added.");
        }
        return _attributes[type];
    }
}
