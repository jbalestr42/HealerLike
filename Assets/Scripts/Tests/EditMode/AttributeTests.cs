using NUnit.Framework;
using UnityEngine;

public class AttributeTests
{
    class FakeModifier : AttributeModifier
    {
        readonly float _value;
        public FakeModifier(float value) { _value = value; }
        public override float ApplyModifier() => _value;
    }

    [Test]
    public void Value_NoModifiers_EqualsBaseValue()
    {
        Attribute attribute = new Attribute(10f);

        Assert.AreEqual(10f, attribute.Value);
    }

    [Test]
    public void Value_WithAddModifier_AddsToBase()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update();

        Assert.AreEqual(15f, attribute.Value);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void Value_WithMultiplyModifier_IsAppliedAsPercentIncrease()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        // Multiply modifiers are interpreted as (1 + value), e.g. 0.5 => x1.5
        attribute.AddModifier(AttributeModifierType.Multiply, source, new FakeModifier(0.5f));
        attribute.Update();

        Assert.AreEqual(15f, attribute.Value);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void Value_AddAndMultiplyModifiers_AddIsAppliedBeforeMultiply()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(10f));
        attribute.AddModifier(AttributeModifierType.Multiply, source, new FakeModifier(1f)); // x2
        attribute.Update();

        Assert.AreEqual(40f, attribute.Value); // (10 + 10) * 2
        Object.DestroyImmediate(source);
    }

    [Test]
    public void Value_OverrideModifier_IgnoresBaseAndOtherModifiers()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(100f));
        attribute.AddModifier(AttributeModifierType.Override, source, new FakeModifier(3f));
        attribute.Update();

        Assert.AreEqual(3f, attribute.Value);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void Value_OverrideModifier_LastOneWinsWhenMultipleOverridesExist()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Override, source, new FakeModifier(1f));
        attribute.AddModifier(AttributeModifierType.Override, source, new FakeModifier(2f));
        attribute.Update();

        Assert.AreEqual(2f, attribute.Value);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void Value_ClampedToZero_NeverNegative()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(-100f));
        attribute.Update();

        Assert.AreEqual(0f, attribute.Value);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveModifiersBySource_RemovesAllTypesFromThatSource()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.RemoveModifiersBySource(source);
        attribute.Update();

        Assert.AreEqual(10f, attribute.Value);
        Object.DestroyImmediate(source);
    }
}
