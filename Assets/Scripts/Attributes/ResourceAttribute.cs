using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResourceAttribute : MonoBehaviour
{
    public UnityEvent<ResourceAttribute> OnValueChanged = new UnityEvent<ResourceAttribute>();
    public UnityEvent<GameObject, ResourceModifier, float, bool> OnAllConsumerProcessed = new UnityEvent<GameObject, ResourceModifier, float, bool>();

    float _prevValue;
    float _value;
    public float Value { get { return _value; } }

    Attribute _max;
    public float Max { get { return _max.Value; } }

    public float percent => _value / _max.Value;

    int _preventConsumersCount = 0;

    // TODO: replace by tag
    public bool preventConsumers { get { return _preventConsumersCount > 0; } set { _preventConsumersCount += value ? 1 : -1; } }

    List<ResourceModifier> _resourceModifiers = new List<ResourceModifier>();

    // TODO: move in SO
    ResourceConsumerResolver _resourceConsumerResolver = new ResourceConsumerResolver();

    public void Init(AttributeType maxResourceType)
    {
        AttributeManager attributeManager = GetComponent<AttributeManager>();
        _max = attributeManager.Get(maxResourceType);
        _value = _max.Value;
        _max.AddOnValueChangedListener(OnValueMaxChanged);
        _resourceConsumerResolver.Init(attributeManager);
        Update();
    }

    void Update()
    {
        if (_resourceModifiers.Count > 0)
        {
            foreach (ResourceModifier resourceModifier in _resourceModifiers)
            {
                if (resourceModifier.consumers.Count > 0)
                {
                    (float value, bool isCritical) = _resourceConsumerResolver.ComputeValue(this, resourceModifier);
                    _value += value;
                    OnAllConsumerProcessed.Invoke(gameObject, resourceModifier, value, isCritical);
                }
            }
            _resourceModifiers.Clear();
        }
        _value = Mathf.Clamp(_value, 0f, _max.Value);

        if (_prevValue != _value)
        {
            OnValueChanged.Invoke(this);
            _prevValue = _value;
        }
    }

    public void AddResourceModifier(ResourceModifier resourceModifier)
    {
        _resourceModifiers.Add(resourceModifier);
    }

    void OnValueMaxChanged(Attribute max)
    {
        _value = max.Value;
        OnValueChanged.Invoke(this);
    }
}