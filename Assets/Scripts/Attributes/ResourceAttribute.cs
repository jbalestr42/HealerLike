using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResourceAttribute : MonoBehaviour
{
    public UnityEvent<ResourceAttribute> OnValueChanged = new UnityEvent<ResourceAttribute>();
    public UnityEvent<GameObject, ResourceModifier, ConsumerResult> OnAllConsumerProcessed = new UnityEvent<GameObject, ResourceModifier, ConsumerResult>();

    float _prevValue;
    float _value;
    public float Value { get { return _value; } }

    Attribute _max;
    public float Max { get { return _max.Value; } }
    // Max value the current value was last adjusted to, to apply only the difference when the max changes
    float _lastMax;

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
        _lastMax = _max.Value;
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
                    float overflow = GetOverflow(_value, value, _max.Value);
                    _value += value;
                    OnAllConsumerProcessed.Invoke(gameObject, resourceModifier, new ConsumerResult(value, isCritical, overflow));
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

    // Part of a heal going above the max, from a value that may already be above it (the value is only
    // clamped once every modifier of the frame is processed)
    public static float GetOverflow(float before, float value, float max)
    {
        if (value <= 0f)
        {
            return 0f;
        }
        return Mathf.Max(0f, before + value - Mathf.Max(before, max));
    }

    public void Refill()
    {
        _value = _max.Value;
    }

    // Sets the value directly, without any consumer: armor, invincibility and heal modifiers don't apply
    public void SetValue(float value)
    {
        _value = Mathf.Clamp(value, 0f, _max.Value);
    }

    public void AddResourceModifier(ResourceModifier resourceModifier)
    {
        _resourceModifiers.Add(resourceModifier);
    }

    // A growing max adds the same amount to the value (40/100 becomes 90/150), a shrinking max keeps
    // the value and only caps it (90/150 becomes 90/100, 140/150 becomes 100/100)
    void OnValueMaxChanged(Attribute max)
    {
        float delta = max.Value - _lastMax;
        _lastMax = max.Value;
        if (delta > 0f)
        {
            _value += delta;
        }
        _value = Mathf.Clamp(_value, 0f, max.Value);

        // Always notified, even when only the max changed: the views show it (90 / 150 becomes 90 / 100)
        _prevValue = _value;
        OnValueChanged.Invoke(this);
    }
}