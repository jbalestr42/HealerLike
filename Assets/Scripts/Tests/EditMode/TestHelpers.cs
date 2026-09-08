using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Shared scaffolding for EditMode tests that need to construct MonoBehaviours
/// (AttributeManager, Entity, Character, ...) without their full production Init() chain.
/// EditMode tests don't run the normal player loop, so Awake/Start never fire automatically,
/// while Editor-only messages like Reset() DO fire synchronously on AddComponent - these
/// helpers work around both.
/// </summary>
/// <summary>
/// A no-op AttributeModifier that just returns a fixed value - shared across tests that need a
/// simple, controllable modifier without pulling in a real gameplay modifier's own logic.
/// </summary>
public class FakeModifier : AttributeModifier
{
    readonly float _value;
    public FakeModifier(float value) { _value = value; }
    public override float ApplyModifier() => _value;
}

public static class TestHelpers
{
    public static void WithLoggingDisabled(Action action)
    {
        bool wasLogEnabled = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = false;
        try
        {
            action();
        }
        finally
        {
            Debug.unityLogger.logEnabled = wasLogEnabled;
        }
    }

    public static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(target, null);
    }

    public static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(target, value);
    }

    /// <summary>
    /// Creates a GameObject with a working, empty AttributeManager (Awake forced).
    /// </summary>
    public static AttributeManager CreateAttributeManager(GameObject go)
    {
        AttributeManager attributeManager = go.AddComponent<AttributeManager>();
        InvokePrivate(attributeManager, "Awake");
        return attributeManager;
    }

    /// <summary>
    /// Creates a GameObject with a working AttributeManager (Awake forced) and one attribute.
    /// </summary>
    public static AttributeManager CreateAttributeManager(GameObject go, AttributeType type, float value)
    {
        AttributeManager attributeManager = CreateAttributeManager(go);
        attributeManager.Add(type, new Attribute(value));
        return attributeManager;
    }

    /// <summary>
    /// Creates a GameObject with a working ResourceAttribute (Init() called, so Max/Value are set)
    /// backed by its own AttributeManager. Adding Entity triggers Entity.Reset() (NREs without a
    /// full Init()), so this is wrapped in WithLoggingDisabled.
    /// </summary>
    public static ResourceAttribute CreateResourceAttribute(GameObject go, AttributeType maxType, float maxValue)
    {
        ResourceAttribute resourceAttribute = null;
        WithLoggingDisabled(() =>
        {
            CreateAttributeManager(go, maxType, maxValue);
            resourceAttribute = go.AddComponent<ResourceAttribute>();
            resourceAttribute.Init(maxType);
        });
        return resourceAttribute;
    }
}
