using NUnit.Framework;
using UnityEngine;

namespace Attributes.Modifiers
{

public class HPBasedModifierTests
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

    HPBasedModifier CreateModifierAtPercent(float percent, float factor, float threshold)
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(health, "_value", 100f * percent);
        TestHelpers.SetPrivateField(_entity, "_health", health);

        HPBasedModifier modifier = new HPBasedModifier { data = new HPBasedModifierData { factor = factor, threshold = threshold } };
        modifier.Init(null, _targetGo);
        return modifier;
    }

    [Test]
    public void ApplyModifier_AtFullHealth_ReturnsNoBonus()
    {
        HPBasedModifier modifier = CreateModifierAtPercent(percent: 1f, factor: 1f, threshold: 0.5f);

        Assert.AreEqual(1f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_AtZeroHealth_ReturnsFullFactorBonus()
    {
        HPBasedModifier modifier = CreateModifierAtPercent(percent: 0f, factor: 1f, threshold: 0.5f);

        Assert.AreEqual(2f, modifier.ApplyModifier(), 0.0001f); // 1 + (1 * factor)
    }

    [Test]
    public void ApplyModifier_HalfwayBetweenThresholdAndZero_ReturnsHalfFactorBonus()
    {
        HPBasedModifier modifier = CreateModifierAtPercent(percent: 0.25f, factor: 1f, threshold: 0.5f);

        Assert.AreEqual(1.5f, modifier.ApplyModifier(), 0.0001f); // 1 + (0.5 * factor)
    }

    [Test]
    public void ApplyModifier_AtThreshold_ReturnsNoBonus()
    {
        HPBasedModifier modifier = CreateModifierAtPercent(percent: 0.5f, factor: 1f, threshold: 0.5f);

        Assert.AreEqual(1f, modifier.ApplyModifier(), 0.0001f);
    }

    [Test]
    public void ApplyModifier_AboveThreshold_ClampsToNoBonus()
    {
        // percent/threshold > 1 is clamped via Clamp01, so being well above threshold never gives
        // a negative bonus.
        HPBasedModifier modifier = CreateModifierAtPercent(percent: 1f, factor: 2f, threshold: 0.1f);

        Assert.AreEqual(1f, modifier.ApplyModifier(), 0.0001f);
    }
}

}
