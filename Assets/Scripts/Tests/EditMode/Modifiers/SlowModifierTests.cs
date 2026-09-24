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
        // its original value here. The field initializer sets it
        // to 1f, but Stack()/Unstack() always recompute it from the CURRENT
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
