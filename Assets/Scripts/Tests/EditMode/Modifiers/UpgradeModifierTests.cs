using NUnit.Framework;

namespace Attributes.Modifiers
{

public class UpgradeModifierTests
{
    static UpgradeModifier CreateModifier(float value)
    {
        return new UpgradeModifier { data = new UpgradeModifierData { value = value } };
    }

    [Test]
    public void ApplyModifier_DefaultStackCount_ReturnsDataValue()
    {
        UpgradeModifier modifier = CreateModifier(3f);

        Assert.AreEqual(3f, modifier.ApplyModifier());
    }

    [Test]
    public void Stack_IncrementsAppliedValue()
    {
        UpgradeModifier modifier = CreateModifier(3f);

        modifier.Stack(null, null);

        Assert.AreEqual(6f, modifier.ApplyModifier()); // 3 * 2 stacks
    }

    [Test]
    public void Stack_Multiple_ScalesLinearlyWithStackCount()
    {
        UpgradeModifier modifier = CreateModifier(3f);

        modifier.Stack(null, null);
        modifier.Stack(null, null);
        modifier.Stack(null, null);

        Assert.AreEqual(12f, modifier.ApplyModifier()); // 3 * 4 stacks
    }

    [Test]
    public void Unstack_DecrementsAppliedValue()
    {
        UpgradeModifier modifier = CreateModifier(3f);
        modifier.Stack(null, null);
        modifier.Stack(null, null); // 3 stacks

        modifier.Unstack(null, null);

        Assert.AreEqual(6f, modifier.ApplyModifier()); // back to 2 stacks
    }
}

}
