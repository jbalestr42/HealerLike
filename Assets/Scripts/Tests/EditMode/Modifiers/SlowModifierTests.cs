using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Modifiers
{

// A slow fading over the duration of its buff, from its full value when the buff is given or refreshed to nothing
public class SlowModifierTests
{
    static BuffHandler CreateHandler(DurationType durationType, float duration, float elapsed = 0f)
    {
        return new BuffHandler { data = new BuffHandlerData { durationType = durationType, duration = duration }, durationTimer = elapsed };
    }

    static SlowModifier CreateModifier(float value, BuffHandler handler)
    {
        SlowModifier modifier = new SlowModifier { data = new SlowModifierData { value = value }, buffHandler = handler };
        modifier.Init(null, null);
        return modifier;
    }

    [Test]
    public void ApplyModifier_NoTimeElapsed_ReturnsFullValue()
    {
        Assert.AreEqual(0.5f, CreateModifier(0.5f, CreateHandler(DurationType.Duration, 10f)).ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_HalfwayThroughTheBuff_ReturnsHalfValue()
    {
        Assert.AreEqual(0.25f, CreateModifier(0.5f, CreateHandler(DurationType.Duration, 10f, 5f)).ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_AtTheEndOfTheBuff_ReturnsZero()
    {
        Assert.AreEqual(0f, CreateModifier(0.5f, CreateHandler(DurationType.Duration, 10f, 10f)).ApplyModifier(), 0.0001f);
    }

    [Test]
    public void InfiniteBuff_NeverFades()
    {
        Assert.AreEqual(0.5f, CreateModifier(0.5f, CreateHandler(DurationType.Infinite, 0f, 1000f)).ApplyModifier(), 0.0001f);
    }

    // E.g. Chilled hit again while chilled, without a new stack (max 1)
    [Test]
    public void BuffRefreshed_BackToTheFullValue()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, 10f, 5f);
        SlowModifier modifier = CreateModifier(0.5f, handler);

        handler.Refresh(null, null);

        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f);
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

    [Test]
    public void Stack_IncreasesTheValueWithDiminishingReturns()
    {
        SlowModifier modifier = CreateModifier(1f, CreateHandler(DurationType.Duration, 10f));

        modifier.Stack(null, null);
        Assert.AreEqual(1.3466f, modifier.ApplyModifier(), 0.0001f, "2 stacks: ln(2)/2 + 1");

        modifier.Stack(null, null);
        Assert.AreEqual(1.5493f, modifier.ApplyModifier(), 0.0001f, "3 stacks: ln(3)/2 + 1");
    }

    [Test]
    public void Stack_StillFadesWithTheBuff()
    {
        SlowModifier modifier = CreateModifier(1f, CreateHandler(DurationType.Duration, 10f, 5f));

        modifier.Stack(null, null);

        Assert.AreEqual(1.3466f / 2f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void Unstack_BackToOneStack_IsAsStrongAsAFreshSlow()
    {
        SlowModifier modifier = CreateModifier(1f, CreateHandler(DurationType.Duration, 10f));
        modifier.Stack(null, null);

        modifier.Unstack(null, null);

        Assert.AreEqual(1f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void StackThenUnstack_MultipleCycles_ReturnsToTheSingleStackValue()
    {
        SlowModifier modifier = CreateModifier(1f, CreateHandler(DurationType.Duration, 10f));

        modifier.Stack(null, null);
        modifier.Stack(null, null);
        modifier.Unstack(null, null);
        modifier.Unstack(null, null);

        Assert.AreEqual(1f, modifier.ApplyModifier(), 0.0001f);
    }
}

}
