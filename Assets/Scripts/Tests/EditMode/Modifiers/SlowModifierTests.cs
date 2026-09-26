using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Modifiers
{

public class SlowModifierTests
{
    static ABuffHandler CreateHandler(DurationType durationType, float duration)
    {
        return new BuffHandler { data = new BuffHandlerData { durationType = durationType, duration = duration } };
    }

    static float GetDuration(SlowModifier modifier)
    {
        return (float)typeof(SlowModifier).GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(modifier);
    }

    [Test]
    public void Init_UsesTheHandlerDuration()
    {
        SlowModifier modifier = new SlowModifier { data = new SlowModifierData { value = 0.5f }, buffHandler = CreateHandler(DurationType.Duration, 10f) };

        modifier.Init(null, null);

        Assert.AreEqual(10f, GetDuration(modifier));
        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void Init_InfiniteHandler_NeverFades()
    {
        // An infinite handler keeps duration at 0 (the field is hidden in the Inspector)
        SlowModifier modifier = new SlowModifier { data = new SlowModifierData { value = 0.5f }, buffHandler = CreateHandler(DurationType.Infinite, 0f) };
        modifier.Init(null, null);

        TestHelpers.SetPrivateField(modifier, "_start", Time.time - 1000f);

        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void Init_HandlerWithoutDuration_DefaultsToOneSecond()
    {
        SlowModifier modifier = new SlowModifier { data = new SlowModifierData { value = 0.5f }, buffHandler = CreateHandler(DurationType.Instant, 10f) };

        modifier.Init(null, null);

        Assert.AreEqual(1f, GetDuration(modifier));
    }

    [Test]
    public void AttributeModifierBuff_Add_PassesItsHandlerToTheModifier()
    {
        // Regression: the modifier used to read a never-assigned buffHandler and NRE when applied
        GameObject target = new GameObject("Target");
        try
        {
            TestHelpers.CreateAttributeManager(target, AttributeType.Speed, 1f);
            var buff = new AttributeModifierBuff<SlowModifier, SlowModifierData>
            {
                data = new SlowModifierData { type = AttributeType.Speed, modifierType = AttributeModifierType.Multiply, value = -0.5f },
                buffHandler = CreateHandler(DurationType.Duration, 2f),
            };

            Assert.DoesNotThrow(() => buff.Add(target, target));
        }
        finally
        {
            Object.DestroyImmediate(target);
        }
    }

    static SlowModifier CreateModifierBypassingConstructor(float value, float secondsElapsed, float duration)
    {
        // Sets the private state directly so decay/stacking can be tested at a given elapsed time
        SlowModifier modifier = (SlowModifier)FormatterServices.GetUninitializedObject(typeof(SlowModifier));
        modifier.data = new SlowModifierData { value = value };
        TestHelpers.SetPrivateField(modifier, "_start", Time.time - secondsElapsed);
        TestHelpers.SetPrivateField(modifier, "_stackFactor", 1f);
        TestHelpers.SetPrivateField(modifier, "_stacks", 1f);
        TestHelpers.SetPrivateField(modifier, "_duration", duration);
        return modifier;
    }

    [Test]
    public void ApplyModifier_NoTimeElapsed_ReturnsFullValue()
    {
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 0.5f, secondsElapsed: 0f, duration: 10f);

        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_HalfwayThroughDuration_ReturnsHalfValue()
    {
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 0.5f, secondsElapsed: 5f, duration: 10f);

        Assert.AreEqual(0.25f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_PastDuration_ReturnsZero()
    {
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 0.5f, secondsElapsed: 100f, duration: 10f);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void Stack_IncreasesStackFactorAndResetsDecay()
    {
        // 5s into a 10s duration, value would normally have decayed to half.
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 1f, secondsElapsed: 5f, duration: 10f);

        modifier.Stack(null, null);

        Assert.AreEqual(1.3466f, modifier.ApplyModifier(), 0.0001f); // 2 stacks: ln(2)/2 + 1, decay reset
    }

    [Test]
    public void Stack_Twice_KeepsIncreasingWithDiminishingReturns()
    {
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 1f, secondsElapsed: 0f, duration: 10f);

        modifier.Stack(null, null);
        modifier.Stack(null, null);

        Assert.AreEqual(1.5493f, modifier.ApplyModifier(), 0.0001f); // 3 stacks: ln(3)/2 + 1
    }

    [Test]
    public void Unstack_BackToOneStack_IsAsStrongAsAFreshSlow()
    {
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 1f, secondsElapsed: 0f, duration: 10f);
        modifier.Stack(null, null);

        modifier.Unstack(null, null);

        Assert.AreEqual(1f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void StackThenUnstack_MultipleCycles_ReturnsToTheSingleStackValue()
    {
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 1f, secondsElapsed: 0f, duration: 10f);

        modifier.Stack(null, null);
        modifier.Stack(null, null);
        modifier.Unstack(null, null);
        modifier.Unstack(null, null);

        Assert.AreEqual(1f, modifier.ApplyModifier(), 0.0001f);
    }
}

}
