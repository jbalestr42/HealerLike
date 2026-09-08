using NUnit.Framework;

namespace Attributes.Modifiers
{

public class FlatModifierTests
{
    static FlatModifier CreateModifier(float value, AttributeModifierType modifierType)
    {
        FlatModifier modifier = new FlatModifier { data = new FlatModifierData { value = value, modifierType = modifierType } };
        modifier.Init(null, null);
        return modifier;
    }

    [Test]
    public void ApplyModifier_AfterInit_ReturnsDataValue()
    {
        FlatModifier modifier = CreateModifier(5f, AttributeModifierType.Add);

        Assert.AreEqual(5f, modifier.ApplyModifier());
    }

    [Test]
    public void Stack_AddType_AccumulatesDataValue()
    {
        FlatModifier modifier = CreateModifier(5f, AttributeModifierType.Add);

        modifier.Stack(null, null);
        Assert.AreEqual(10f, modifier.ApplyModifier());

        modifier.Stack(null, null);
        Assert.AreEqual(15f, modifier.ApplyModifier());
    }

    [Test]
    public void Unstack_AddType_ReversesStack()
    {
        FlatModifier modifier = CreateModifier(5f, AttributeModifierType.Add);
        modifier.Stack(null, null);
        modifier.Stack(null, null); // 15

        modifier.Unstack(null, null);

        Assert.AreEqual(10f, modifier.ApplyModifier());
    }

    [Test]
    public void Stack_MultiplyType_AccumulatesLogarithmically()
    {
        FlatModifier modifier = CreateModifier(0.5f, AttributeModifierType.Multiply);

        modifier.Stack(null, null);

        // _lastStackedValue = 0.5 * 0.5 = 0.25; _stackedValue = 0.5 + 0.25 = 0.75
        Assert.AreEqual(0.75f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void Unstack_MultiplyType_ReversesStack()
    {
        FlatModifier modifier = CreateModifier(0.5f, AttributeModifierType.Multiply);
        modifier.Stack(null, null); // 0.75

        modifier.Unstack(null, null);

        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f); // back to the initial value
    }

    [Test]
    public void StackThenUnstack_MultipleCycles_AddType_ReturnsToOriginalValue()
    {
        FlatModifier modifier = CreateModifier(5f, AttributeModifierType.Add);

        modifier.Stack(null, null);
        modifier.Stack(null, null);
        modifier.Stack(null, null);
        modifier.Unstack(null, null);
        modifier.Unstack(null, null);
        modifier.Unstack(null, null);

        Assert.AreEqual(5f, modifier.ApplyModifier());
    }

    [Test]
    public void StackThenUnstack_MultipleCycles_MultiplyType_ReturnsToOriginalValue()
    {
        // _lastStackedValue compounds multiplicatively across stacks (0.5 -> 0.25 -> 0.125 -> ...),
        // so Unstack must divide it back out in the same order it was multiplied in to land exactly
        // back on the original value - this protects that symmetry across more than one cycle.
        FlatModifier modifier = CreateModifier(0.5f, AttributeModifierType.Multiply);

        modifier.Stack(null, null);
        modifier.Stack(null, null);
        modifier.Unstack(null, null);
        modifier.Unstack(null, null);

        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void Stack_OverrideType_DoesNotChangeAppliedValue()
    {
        // Stack() only has branches for Add and Multiply; Override is a no-op on purpose (an
        // override modifier fully replaces the attribute's value, stacking it wouldn't make sense).
        FlatModifier modifier = CreateModifier(5f, AttributeModifierType.Override);

        modifier.Stack(null, null);

        Assert.AreEqual(5f, modifier.ApplyModifier());
    }
}

}
