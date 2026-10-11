using NUnit.Framework;

namespace Attributes.Modifiers
{

public class AttackSpeedModifierTests
{
    static AttackSpeedModifier CreateModifier(float value)
    {
        AttackSpeedModifier modifier = new AttackSpeedModifier { data = new AttackSpeedModifierData { value = value, modifierType = AttributeModifierType.Multiply } };
        modifier.Init(null, null);
        return modifier;
    }

    // The cooldown multiplier the Multiply modifier ends up applying
    static float CooldownMultiplier(AttackSpeedModifier modifier) => 1f + modifier.ApplyModifier();

    [Test]
    public void ApplyModifier_Plus100Percent_HalvesTheCooldown()
    {
        AttackSpeedModifier modifier = CreateModifier(1f);

        Assert.AreEqual(0.5f, CooldownMultiplier(modifier), 0.0001f);
    }

    [Test]
    public void ApplyModifier_Plus50Percent_GivesOneAndAHalfAttacks()
    {
        AttackSpeedModifier modifier = CreateModifier(0.5f);

        Assert.AreEqual(1f / 1.5f, CooldownMultiplier(modifier), 0.0001f);
    }

    [Test]
    public void Stack_AddsUpTheSpeed()
    {
        AttackSpeedModifier modifier = CreateModifier(1f);

        modifier.Stack(null, null);
        // +200%: three times as many attacks
        Assert.AreEqual(1f / 3f, CooldownMultiplier(modifier), 0.0001f);

        modifier.Stack(null, null);
        Assert.AreEqual(0.25f, CooldownMultiplier(modifier), 0.0001f);
    }

    [Test]
    public void Unstack_ReversesStack()
    {
        AttackSpeedModifier modifier = CreateModifier(1f);
        modifier.Stack(null, null);
        modifier.Stack(null, null);

        modifier.Unstack(null, null);
        Assert.AreEqual(1f / 3f, CooldownMultiplier(modifier), 0.0001f);

        modifier.Unstack(null, null);
        Assert.AreEqual(0.5f, CooldownMultiplier(modifier), 0.0001f);
    }

    [Test]
    public void Init_StartsFromASingleStack()
    {
        AttackSpeedModifier modifier = CreateModifier(1f);
        modifier.Stack(null, null);

        modifier.Init(null, null);

        Assert.AreEqual(0.5f, CooldownMultiplier(modifier), 0.0001f);
    }
}

}
