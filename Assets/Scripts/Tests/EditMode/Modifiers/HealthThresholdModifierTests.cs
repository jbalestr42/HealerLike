using NUnit.Framework;
using UnityEngine;

namespace Attributes.Modifiers
{

public class HealthThresholdModifierTests
{
    GameObject _targetGo;
    GameObject _healthGo;
    Entity _entity;

    [SetUp]
    public void SetUp()
    {
        _targetGo = new GameObject();
        // Adding Entity triggers Entity.Reset() (an editor-only message), which NREs without a
        // full Entity.Init() - not needed here, we only use it as a holder for .health.
        TestHelpers.WithLoggingDisabled(() => _entity = _targetGo.AddComponent<Entity>());

        _healthGo = new GameObject();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_targetGo);
        Object.DestroyImmediate(_healthGo);
    }

    HealthThresholdModifier CreateModifierAtPercent(float percent, float value, float threshold, bool isBelow = false)
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(health, "_value", 100f * percent);
        TestHelpers.SetPrivateField(_entity, "_health", health);

        HealthThresholdModifier modifier = new HealthThresholdModifier { data = new HealthThresholdModifierData { value = value, threshold = threshold, isBelow = isBelow } };
        modifier.Init(null, _targetGo);
        return modifier;
    }

    [Test]
    public void ApplyModifier_AtFullHealth_ReturnsTheWholeValue()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 1f, value: 0.5f, threshold: 0.7f);

        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_JustAboveThreshold_ReturnsTheWholeValue()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 0.71f, value: 0.5f, threshold: 0.7f);

        Assert.AreEqual(0.5f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_AtThreshold_ReturnsNoBonus()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 0.7f, value: 0.5f, threshold: 0.7f);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_BelowThreshold_ReturnsNoBonus()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 0.3f, value: 0.5f, threshold: 0.7f);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_FollowsTheCurrentHealth()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 1f, value: 0.5f, threshold: 0.7f);
        TestHelpers.SetPrivateField(_entity.health, "_value", 50f);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void AsMultiplyModifier_AboveThreshold_IncreasesTheAttributeByTheValue()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 1f, value: 0.5f, threshold: 0.7f);
        Attribute damage = new Attribute(10f);
        damage.AddModifier(AttributeModifierType.Multiply, _targetGo, modifier);

        damage.Update();

        Assert.AreEqual(15f, damage.Value, 0.0001f);
    }

    [Test]
    public void AsMultiplyModifier_BelowThreshold_LeavesTheAttributeUnchanged()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 0.5f, value: 0.5f, threshold: 0.7f);
        Attribute damage = new Attribute(10f);
        damage.AddModifier(AttributeModifierType.Multiply, _targetGo, modifier);

        damage.Update();

        Assert.AreEqual(10f, damage.Value, 0.0001f);
    }

    [Test]
    public void IsBelow_BelowThreshold_ReturnsTheWholeValue()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 0.3f, value: -0.3f, threshold: 0.5f, isBelow: true);

        Assert.AreEqual(-0.3f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void IsBelow_AtThreshold_ReturnsNoBonus()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 0.5f, value: -0.3f, threshold: 0.5f, isBelow: true);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void IsBelow_AboveThreshold_ReturnsNoBonus()
    {
        HealthThresholdModifier modifier = CreateModifierAtPercent(percent: 1f, value: -0.3f, threshold: 0.5f, isBelow: true);

        Assert.AreEqual(0f, modifier.ApplyModifier(), 0.0001f);
    }
}

}
