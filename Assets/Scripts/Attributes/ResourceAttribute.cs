using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResourceAttribute : MonoBehaviour
{
    public UnityEvent<ResourceAttribute> OnValueChanged = new UnityEvent<ResourceAttribute>();
    public UnityEvent<GameObject, ResourceAttribute, ResourceModifier, float> OnAllConsumerProcessed = new UnityEvent<GameObject, ResourceAttribute, ResourceModifier, float>();

    float _prevValue;
    float _value;
    public float Value { get { return _value; } }

    Attribute _max;
    public float Max { get { return _max.Value; } }

    public float percent => _value / _max.Value;

    int _preventConsumersCount = 0;
    public bool preventConsumers { get { return _preventConsumersCount > 0; } set { _preventConsumersCount += value ? 1 : -1; } }

    List<ResourceModifier> _resourceModifiers = new List<ResourceModifier>();
    List<AConsumerModifier> _consumerModifiers = new List<AConsumerModifier>();

    public void Init()
    {
        AttributeManager attributeManager = GetComponent<AttributeManager>();
        _max = attributeManager.Get(AttributeType.HealthMax);
        _value = _max.Value;
        _max.AddOnValueChangedListener(OnValueMaxChanged);
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
                    float value = 0f;
                    foreach (AConsumer consumer in resourceModifier.consumers)
                    {
                        if (CanApplyConsumer(consumer))
                        {
                            value += ApplyConsumerModifiers(consumer);
                        }
                    }
                    resourceModifier.consumers.Clear();

                    value *= resourceModifier.multiplier;
                    _value += value;
                    OnAllConsumerProcessed.Invoke(gameObject, this, resourceModifier, value);
                }
            }
            _resourceModifiers.Clear();
        }

        _value = Mathf.Clamp(_value, 0f, _max.Value);

        if (_prevValue != _value)
        {
            OnValueChanged.Invoke(this);
        }
        _prevValue = _value;
    }

    bool CanApplyConsumer(AConsumer consumer)
    {
        return !preventConsumers || consumer.ignoreConsumerPrevention;
    }

    float ApplyConsumerModifiers(AConsumer consumer)
    {
        float baseValue = consumer.GetValue();
        float value = baseValue;
        if (!consumer.ignoreConsumerModifier)
        {
            foreach (AConsumerModifier controller in _consumerModifiers)
            {
                value = controller.ApplyController(consumer, value);
            }
        }

        return value;
    }

    public void AddConsumerModifier(AConsumerModifier controller)
    {
        _consumerModifiers.Add(controller);
    }

    public void RemoveConsumerModifier(AConsumerModifier controller)
    {
        _consumerModifiers.Remove(controller);
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