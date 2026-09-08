using NUnit.Framework;
using UnityEngine;

namespace Attributes
{

public class AttributeTests
{
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

    [Test]
    public void OnValueChanged_ValueActuallyChanges_ListenerIsInvoked()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        int callCount = 0;
        attribute.AddOnValueChangedListener(_ => callCount++);

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update();

        Assert.AreEqual(1, callCount);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void OnValueChanged_ListenerReceivesTheSameAttributeInstance()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        Attribute received = null;
        attribute.AddOnValueChangedListener(a => received = a);

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update();

        Assert.AreSame(attribute, received);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void OnValueChanged_ValueUnchanged_ListenerIsNotInvoked()
    {
        Attribute attribute = new Attribute(10f);
        int callCount = 0;
        attribute.AddOnValueChangedListener(_ => callCount++);

        // No modifiers added, Value stays at 10 across Update calls.
        attribute.Update();
        attribute.Update();

        Assert.AreEqual(0, callCount);
    }

    [Test]
    public void OnValueChanged_MultipleChanges_InvokedOncePerActualChange()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        int callCount = 0;
        attribute.AddOnValueChangedListener(_ => callCount++);

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update(); // 10 -> 15
        attribute.Update(); // 15 -> 15, no change
        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update(); // 15 -> 20

        Assert.AreEqual(20f, attribute.Value);
        Assert.AreEqual(2, callCount);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void OnValueChanged_MultipleListeners_AllAreInvoked()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        int firstCallCount = 0;
        int secondCallCount = 0;
        attribute.AddOnValueChangedListener(_ => firstCallCount++);
        attribute.AddOnValueChangedListener(_ => secondCallCount++);

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update();

        Assert.AreEqual(1, firstCallCount);
        Assert.AreEqual(1, secondCallCount);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveOnValueChangedListener_StopsReceivingFutureNotifications()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        int callCount = 0;
        Attribute.OnValueChanged listener = _ => callCount++;
        attribute.AddOnValueChangedListener(listener);

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update(); // triggers the listener once

        attribute.RemoveOnValueChangedListener(listener);

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update(); // should no longer trigger the listener

        Assert.AreEqual(1, callCount);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveOnValueChangedListener_OnlyRemovesTheGivenListener()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        int removedCallCount = 0;
        int remainingCallCount = 0;
        Attribute.OnValueChanged removedListener = _ => removedCallCount++;
        attribute.AddOnValueChangedListener(removedListener);
        attribute.AddOnValueChangedListener(_ => remainingCallCount++);

        attribute.RemoveOnValueChangedListener(removedListener);

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.Update();

        Assert.AreEqual(0, removedCallCount);
        Assert.AreEqual(1, remainingCallCount);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void GetModifiers_NoModifiersAdded_ReturnsEmptyList()
    {
        Attribute attribute = new Attribute(10f);

        Assert.IsEmpty(attribute.GetModifiers(AttributeModifierType.Add));
    }

    [Test]
    public void GetModifiers_ReturnsOnlyModifiersOfThatType()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        AttributeModifier addModifier = new FakeModifier(5f);
        AttributeModifier multiplyModifier = new FakeModifier(0.5f);

        attribute.AddModifier(AttributeModifierType.Add, source, addModifier);
        attribute.AddModifier(AttributeModifierType.Multiply, source, multiplyModifier);

        var addModifiers = attribute.GetModifiers(AttributeModifierType.Add);
        Assert.AreEqual(1, addModifiers.Count);
        Assert.AreSame(addModifier, addModifiers[0].modifier);
        Assert.AreSame(source, addModifiers[0].source);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void GetModifiers_MultipleModifiersSameType_ReturnsAllOfThem()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(3f));

        Assert.AreEqual(2, attribute.GetModifiers(AttributeModifierType.Add).Count);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveModifier_WithType_RemovesOnlyThatModifierInstance()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        AttributeModifier toRemove = new FakeModifier(5f);
        AttributeModifier toKeep = new FakeModifier(3f);
        attribute.AddModifier(AttributeModifierType.Add, source, toRemove);
        attribute.AddModifier(AttributeModifierType.Add, source, toKeep);

        attribute.RemoveModifier(AttributeModifierType.Add, toRemove);
        attribute.Update();

        Assert.AreEqual(13f, attribute.Value); // only toKeep (+3) remains
        Assert.AreEqual(1, attribute.GetModifiers(AttributeModifierType.Add).Count);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveModifier_WithType_DoesNotAffectOtherTypes()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        AttributeModifier modifier = new FakeModifier(5f);
        attribute.AddModifier(AttributeModifierType.Add, source, modifier);

        // Removing from Multiply should not remove the same instance registered under Add.
        attribute.RemoveModifier(AttributeModifierType.Multiply, modifier);
        attribute.Update();

        Assert.AreEqual(15f, attribute.Value); // modifier is still active under Add
        Assert.AreEqual(1, attribute.GetModifiers(AttributeModifierType.Add).Count);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveModifier_WithoutType_RemovesFromWhicheverTypeItWasAddedTo()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        AttributeModifier modifier = new FakeModifier(0.5f);
        attribute.AddModifier(AttributeModifierType.Multiply, source, modifier);

        attribute.RemoveModifier(modifier);
        attribute.Update();

        Assert.AreEqual(10f, attribute.Value);
        Assert.IsEmpty(attribute.GetModifiers(AttributeModifierType.Multiply));
        Object.DestroyImmediate(source);
    }

    [Test]
    public void BaseValue_ChangedAfterConstruction_IsReflectedOnNextUpdate()
    {
        Attribute attribute = new Attribute(10f);

        attribute.BaseValue = 20f;
        attribute.Update();

        Assert.AreEqual(20f, attribute.Value);
    }

    [Test]
    public void Value_MultipleMultiplyModifiers_StackAdditivelyBeforeMultiplying()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        // multiplicative = (1 + 0.5) * (1 + 0.5) is NOT how it works: it's 1 * (1+0.5) * (1+0.5) applied
        // sequentially, i.e. multiplicative *= 1 + value for each modifier.
        attribute.AddModifier(AttributeModifierType.Multiply, source, new FakeModifier(0.5f));
        attribute.AddModifier(AttributeModifierType.Multiply, source, new FakeModifier(0.5f));
        attribute.Update();

        Assert.AreEqual(22.5f, attribute.Value); // 10 * 1.5 * 1.5
        Object.DestroyImmediate(source);
    }

    [Test]
    public void Value_OverrideModifier_IgnoresMultiplyModifiers()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();

        attribute.AddModifier(AttributeModifierType.Multiply, source, new FakeModifier(2f));
        attribute.AddModifier(AttributeModifierType.Override, source, new FakeModifier(5f));
        attribute.Update();

        Assert.AreEqual(5f, attribute.Value);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveModifiersBySource_WithType_OnlyRemovesFromThatType()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.AddModifier(AttributeModifierType.Multiply, source, new FakeModifier(1f)); // x2

        attribute.RemoveModifiersBySource(AttributeModifierType.Add, source);
        attribute.Update();

        Assert.AreEqual(20f, attribute.Value); // (10 + 0) * 2, Multiply modifier untouched
        Assert.IsEmpty(attribute.GetModifiers(AttributeModifierType.Add));
        Assert.AreEqual(1, attribute.GetModifiers(AttributeModifierType.Multiply).Count);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveModifiersBySource_WithoutType_RemovesFromEveryType()
    {
        Attribute attribute = new Attribute(10f);
        GameObject source = new GameObject();
        attribute.AddModifier(AttributeModifierType.Add, source, new FakeModifier(5f));
        attribute.AddModifier(AttributeModifierType.Multiply, source, new FakeModifier(1f)); // x2
        attribute.AddModifier(AttributeModifierType.Override, source, new FakeModifier(3f));

        attribute.RemoveModifiersBySource(source);
        attribute.Update();

        Assert.AreEqual(10f, attribute.Value); // back to base, every type cleared
        Assert.IsEmpty(attribute.GetModifiers(AttributeModifierType.Add));
        Assert.IsEmpty(attribute.GetModifiers(AttributeModifierType.Multiply));
        Assert.IsEmpty(attribute.GetModifiers(AttributeModifierType.Override));
        Object.DestroyImmediate(source);
    }

    [Test]
    public void RemoveModifiersBySource_OnlyAffectsTheGivenSource()
    {
        Attribute attribute = new Attribute(10f);
        GameObject sourceToRemove = new GameObject();
        GameObject otherSource = new GameObject();
        attribute.AddModifier(AttributeModifierType.Add, sourceToRemove, new FakeModifier(5f));
        attribute.AddModifier(AttributeModifierType.Add, otherSource, new FakeModifier(3f));

        attribute.RemoveModifiersBySource(sourceToRemove);
        attribute.Update();

        Assert.AreEqual(13f, attribute.Value); // only otherSource's modifier remains
        Assert.AreEqual(1, attribute.GetModifiers(AttributeModifierType.Add).Count);
        Object.DestroyImmediate(sourceToRemove);
        Object.DestroyImmediate(otherSource);
    }
}

}
