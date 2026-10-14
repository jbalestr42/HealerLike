using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Value of a modifier about to be applied, once reduced by the armor: the listeners may change it
public class PendingValue
{
    public ResourceModifier resourceModifier;
    public float value;
}

// What a resource is, an object may hold several (e.g. the mana and the Devotion of the character)
public enum ResourceType
{
    Health,
    Mana,
    // Charges built and spent by some skills of the character (e.g. the Cleric), back to 0 at each fight
    Devotion,
    // Taken before the health of a unit by the damage (see outerLayer), up to its max health, back to 0 at each fight
    Shield,
}

public class ResourceAttribute : MonoBehaviour
{
    public UnityEvent<ResourceAttribute> OnValueChanged = new UnityEvent<ResourceAttribute>();
    public UnityEvent<PendingValue> OnBeforeValueApplied = new UnityEvent<PendingValue>();
    public UnityEvent<GameObject, ResourceModifier, ConsumerResult> OnAllConsumerProcessed = new UnityEvent<GameObject, ResourceModifier, ConsumerResult>();

    float _prevValue;
    float _value;
    public float Value { get { return _value; } }

    ResourceType _type;
    public ResourceType type { get { return _type; } }

    Attribute _max;
    public float Max { get { return _max.Value; } }
    // Max value the current value was last adjusted to, to apply only the difference when the max changes
    float _lastMax;

    public float percent => _value / Max;

    int _preventConsumersCount = 0;

    // TODO: replace by tag
    public bool preventConsumers { get { return _preventConsumersCount > 0; } set { _preventConsumersCount += value ? 1 : -1; } }

    List<ResourceModifier> _resourceModifiers = new List<ResourceModifier>();
    // The modifiers being processed, swapped with _resourceModifiers so new ones can be added meanwhile
    List<ResourceModifier> _processedModifiers = new List<ResourceModifier>();
    // Reused for every modifier, so sending OnBeforeValueApplied allocates nothing
    readonly PendingValue _pendingValue = new PendingValue();

    // False: a growing max only raises the cap, the value is earned (e.g. a shield filled by the overheal)
    public bool fillWithMax { get; set; } = true;

    // Covers this resource: takes the damage before it, as much as it holds (e.g. the shield over the health). Itself
    // covered by its own outer layer, the damage going through the layers from the outermost one, none: nothing
    public ResourceAttribute outerLayer { get; set; }

    // TODO: move in SO
    ResourceConsumerResolver _resourceConsumerResolver = new ResourceConsumerResolver();

    // Full, its max read from the given attribute of the owner, its gains multiplied by the other one if any (e.g.
    // HealingReceived for the heals of the health)
    public void Init(ResourceType type, AttributeType maxType, AttributeType? gainMultiplierType = null)
    {
        _type = type;
        AttributeManager attributeManager = GetComponent<AttributeManager>();
        _max = attributeManager.Get(maxType);
        _value = _max.Value;
        _lastMax = _max.Value;
        _max.AddOnValueChangedListener(OnValueMaxChanged);
        _resourceConsumerResolver.Init(attributeManager, gainMultiplierType);
        Update();
    }

    // The resource of that type held by the owner, null when it has none
    public static ResourceAttribute Get(GameObject owner, ResourceType type)
    {
        if (owner == null)
        {
            return null;
        }

        foreach (ResourceAttribute resource in owner.GetComponents<ResourceAttribute>())
        {
            if (resource.type == type)
            {
                return resource;
            }
        }
        return null;
    }

    void Update()
    {
        if (_resourceModifiers.Count > 0)
        {
            // Modifiers added while these are processed (by a listener) wait for the next frame
            (_processedModifiers, _resourceModifiers) = (_resourceModifiers, _processedModifiers);
            foreach (ResourceModifier resourceModifier in _processedModifiers)
            {
                if (resourceModifier.consumers.Count > 0)
                {
                    // Read before the consumers are used up by the computation
                    bool isUnstoppable = IsUnstoppable(resourceModifier);
                    (float value, bool isCritical) = _resourceConsumerResolver.ComputeValue(this, resourceModifier);
                    _pendingValue.resourceModifier = resourceModifier;
                    _pendingValue.value = value;
                    OnBeforeValueApplied.Invoke(_pendingValue);
                    // After the listeners: the outer layers only take what is left (e.g. once a Soul Link shared the damage)
                    value = DamageThroughLayer(outerLayer, _pendingValue.value, isUnstoppable);
                    float absorbed = value - _pendingValue.value;
                    _pendingValue.resourceModifier = null;
                    float overflow = GetOverflow(_value, value, Max);
                    _value += value;
                    OnAllConsumerProcessed.Invoke(gameObject, resourceModifier, new ConsumerResult(value, isCritical, overflow, absorbed));
                }
            }
            _processedModifiers.Clear();
        }
        _value = Mathf.Clamp(_value, 0f, Max);

        if (_prevValue != _value)
        {
            OnValueChanged.Invoke(this);
            _prevValue = _value;
        }
    }

    // Damage left once the layer and the ones over it took what they hold, the outermost first, emptied right away
    // (several hits in a frame share them). Never a heal, nor a modifier that ignores the invincibility (e.g. a fuse
    // killing its holder)
    public static float DamageThroughLayer(ResourceAttribute layer, float value, bool isUnstoppable)
    {
        if (layer == null || value >= 0f || isUnstoppable)
        {
            return value;
        }

        value = DamageThroughLayer(layer.outerLayer, value, isUnstoppable);
        float taken = Mathf.Min(layer.Value, -value);
        if (taken > 0f)
        {
            layer.SetValue(layer.Value - taken);
        }
        return value + Mathf.Max(0f, taken);
    }

    // Every consumer ignores the invincibility (e.g. a fuse killing its holder)
    public static bool IsUnstoppable(ResourceModifier resourceModifier)
    {
        return resourceModifier.consumers.Count > 0 && resourceModifier.consumers.TrueForAll(consumer => consumer.ignoreConsumerPrevention);
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
        _value = Max;
    }

    // Sets the value directly, without any consumer: armor, invincibility and heal modifiers don't apply
    public void SetValue(float value)
    {
        _value = Mathf.Clamp(value, 0f, Max);
    }

    // Reads the value, then empties the resource through a consumer, never critical: the amount returned is the one
    // to use (e.g. a skill spending all the Devotion), the resource being emptied later doesn't change it
    public float SpendAll(GameObject source)
    {
        float spent = _value;
        if (spent > 0f)
        {
            ResourceModifier spending = new ResourceModifier { source = source };
            spending.consumers.Add(new RuntimeConsumer(-spent, canBeCritical: false));
            AddResourceModifier(spending);
        }
        return spent;
    }

    public void AddResourceModifier(ResourceModifier resourceModifier)
    {
        _resourceModifiers.Add(resourceModifier);
    }

    // A growing max adds the same amount to the value (40/100 becomes 90/150) unless it doesn't fill with it, a
    // shrinking max keeps the value and only caps it (90/150 becomes 90/100, 140/150 becomes 100/100)
    void OnValueMaxChanged(Attribute max)
    {
        float delta = max.Value - _lastMax;
        _lastMax = max.Value;
        if (delta > 0f && fillWithMax)
        {
            _value += delta;
        }
        _value = Mathf.Clamp(_value, 0f, max.Value);

        // Always notified, even when only the max changed: the views show it (90 / 150 becomes 90 / 100)
        _prevValue = _value;
        OnValueChanged.Invoke(this);
    }
}