using NUnit.Framework;
using UnityEngine;

namespace Entities
{

public class HealthValidatorTests
{
    GameObject _source;
    GameObject _target;
    ResourceAttribute _health;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Guardian");
        _target = new GameObject("Target");
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            entity = _target.AddComponent<Entity>();
        });
        GameObject healthGo = new GameObject("Health");
        healthGo.transform.SetParent(_target.transform);
        _health = TestHelpers.CreateResourceAttribute(healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(entity, "_health", _health);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
    }

    void SetHealth(float value)
    {
        TestHelpers.CreateAttributeManager(_source);
        ResourceModifier modifier = new ResourceModifier { source = _source };
        modifier.consumers.Add(new FlatConsumer(value - _health.Value));
        _health.AddResourceModifier(modifier);
        TestHelpers.InvokePrivate(_health, "Update");
    }

    class FlatConsumer : AConsumer
    {
        readonly float _value;
        public FlatConsumer(float value) => _value = value;
        public override float GetValue() => _value;
        public override bool ignoreDamageReduction => true;
        public override bool ignoreConsumerPrevention => true;
    }

    static HealthValidator CreateValidator(float threshold)
    {
        return new HealthValidator { data = new HealthValidatorData { threshold = threshold } };
    }

    [Test]
    public void FullHealth_IsNotValidUnderSeventyPercent()
    {
        Assert.IsFalse(CreateValidator(0.7f).IsValid(_source, _target));
    }

    [Test]
    public void AboveTheThreshold_IsNotValid()
    {
        SetHealth(71f);

        Assert.IsFalse(CreateValidator(0.7f).IsValid(_source, _target));
    }

    [Test]
    public void UnderTheThreshold_IsValid()
    {
        SetHealth(69f);

        Assert.IsTrue(CreateValidator(0.7f).IsValid(_source, _target));
    }
}

}
