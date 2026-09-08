using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Modifiers
{

public class SlowModifierTests
{
    [Test]
    public void Constructor_ThrowsBecauseBuffHandlerIsNeverAssignedInProduction()
    {
        // AttributeModifier.buffHandler is a settable property, but nothing in the codebase ever
        // assigns it before a modifier is constructed (SlowModifierFactory/AttributeModifierBuff
        // just does `new ModifierType() { data = data }`). SlowModifier's constructor reads
        // buffHandler.hasDuration immediately, so any buff wired to SlowModifierFactory currently
        // crashes with NullReferenceException the moment it's applied in game. This test documents
        // that crash so a real fix (ABuff.Add/Instant passing the owning ABuffHandler through)
        // doesn't silently go untested. Flagged to the team separately - not fixed here.
        Assert.Throws<NullReferenceException>(() => new SlowModifier());
    }

    static SlowModifier CreateModifierBypassingConstructor(float value, float secondsElapsed, float duration)
    {
        // Bypasses the constructor (see the test above) purely so the rest of the class's logic
        // (ApplyModifier/Stack/Unstack) can be exercised and protected by tests.
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

        float expectedStackFactor = Mathf.Log(2f + 1f) / 2f + 1f; // _stacks went from 1 to 2
        Assert.AreEqual(expectedStackFactor, modifier.ApplyModifier(), 0.0001f); // decay reset to 0
    }

    [Test]
    public void Unstack_DecreasesStackFactor()
    {
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 1f, secondsElapsed: 0f, duration: 10f);
        modifier.Stack(null, null); // _stacks = 2

        modifier.Unstack(null, null); // _stacks = 1

        float expectedStackFactor = Mathf.Log(1f + 1f) / 2f + 1f;
        Assert.AreEqual(expectedStackFactor, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void StackThenUnstack_MultipleCycles_RecomputesFromCurrentStackCount()
    {
        // NOTE: unlike FlatModifier, a full Stack/Unstack round trip does NOT return _stackFactor to
        // its original value here. The constructor (bypassed above, see the crash test) would set it
        // to 1f by field initializer, but Stack()/Unstack() always recompute it from the CURRENT
        // _stacks count via Log(_stacks + 1) / 2 + 1 - which at _stacks == 1 evaluates to
        // Log(2)/2+1 (~1.35), not 1f. So 2 stacks followed by 2 unstacks lands on the formula's
        // value for 1 stack, not the field initializer's default. Documenting the actual behavior
        // here rather than assuming symmetry with FlatModifier's stacking.
        SlowModifier modifier = CreateModifierBypassingConstructor(value: 1f, secondsElapsed: 0f, duration: 10f);

        modifier.Stack(null, null); // _stacks = 2
        modifier.Stack(null, null); // _stacks = 3
        modifier.Unstack(null, null); // _stacks = 2
        modifier.Unstack(null, null); // _stacks = 1

        float expectedStackFactor = Mathf.Log(1f + 1f) / 2f + 1f;
        Assert.AreEqual(expectedStackFactor, modifier.ApplyModifier(), 0.0001f);
    }
}

}
