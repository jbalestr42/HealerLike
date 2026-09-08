using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Modifiers
{

public class TimeModifierTests
{
    [Test]
    public void Constructor_ThrowsBecauseBuffHandlerIsNeverAssignedInProduction()
    {
        // Same root cause as SlowModifier (see SlowModifierTests) - buffHandler is never assigned
        // before construction, so TimeModifier's constructor NREs on buffHandler.hasDuration.
        Assert.Throws<NullReferenceException>(() => new TimeModifier());
    }

    static TimeModifier CreateModifierBypassingConstructor(float value, float secondsElapsed, float duration)
    {
        TimeModifier modifier = (TimeModifier)FormatterServices.GetUninitializedObject(typeof(TimeModifier));
        modifier.data = new TimeModifierData { value = value };
        TestHelpers.SetPrivateField(modifier, "_start", Time.time - secondsElapsed);
        TestHelpers.SetPrivateField(modifier, "_duration", duration);
        return modifier;
    }

    [Test]
    public void ApplyModifier_NoTimeElapsed_ReturnsFullValue()
    {
        TimeModifier modifier = CreateModifierBypassingConstructor(value: 10f, secondsElapsed: 0f, duration: 10f);

        Assert.AreEqual(10f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_HalfwayThroughDuration_ReturnsHalfValue()
    {
        TimeModifier modifier = CreateModifierBypassingConstructor(value: 10f, secondsElapsed: 5f, duration: 10f);

        Assert.AreEqual(5f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_PastDuration_ReturnsZero()
    {
        TimeModifier modifier = CreateModifierBypassingConstructor(value: 10f, secondsElapsed: 100f, duration: 10f);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void Stack_RefreshesDecayBackToFullValue()
    {
        TimeModifier modifier = CreateModifierBypassingConstructor(value: 10f, secondsElapsed: 5f, duration: 10f);
        Assert.AreEqual(5f, modifier.ApplyModifier(), 0.0001f); // half-decayed before stacking

        modifier.Stack(null, null);

        Assert.AreEqual(10f, modifier.ApplyModifier(), 0.0001f); // decay reset
    }

    [Test]
    public void Unstack_DoesNotChangeAppliedValue()
    {
        // Unlike SlowModifier, TimeModifier.Unstack() is a documented no-op.
        TimeModifier modifier = CreateModifierBypassingConstructor(value: 10f, secondsElapsed: 5f, duration: 10f);

        modifier.Unstack(null, null);

        Assert.AreEqual(5f, modifier.ApplyModifier(), 0.0001f);
    }
}

}
