using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class InvincibilityBuffTests
{
    GameObject _source;
    GameObject _target;
    ResourceAttribute _health;
    InvincibleValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Guardian");
        TestHelpers.CreateAttributeManager(_source);
        _target = new GameObject("Target");
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            entity = _target.AddComponent<Entity>();
        });
        TestHelpers.InvokePrivate(_target.GetComponent<AttributeManager>(), "Awake");
        GameObject healthGo = new GameObject("Health");
        healthGo.transform.SetParent(_target.transform);
        _health = TestHelpers.CreateResourceAttribute(healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(entity, "_health", _health);

        _validator = new InvincibleValidator { data = new InvincibleValidatorData() };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
    }

    static InvincibilityBuff CreateBuff()
    {
        return new InvincibilityBuff { data = new InvincibilityBuffData() };
    }

    void Hit(float damage)
    {
        ResourceModifier modifier = new ResourceModifier { source = _source };
        modifier.consumers.Add(new FlatDamageConsumer(damage));
        _health.AddResourceModifier(modifier);
        TestHelpers.InvokePrivate(_health, "Update");
    }

    class FlatDamageConsumer : AConsumer
    {
        readonly float _damage;
        public FlatDamageConsumer(float damage) => _damage = damage;
        public override float GetValue() => -_damage;
        public override bool ignoreDamageReduction => false;
        public override bool ignoreConsumerPrevention => false;
    }

    [Test]
    public void Add_BlocksTheDamage()
    {
        CreateBuff().Add(_source, _target);

        Hit(10f);

        Assert.AreEqual(100f, _health.Value);
    }

    [Test]
    public void Remove_TakesDamageAgain()
    {
        InvincibilityBuff buff = CreateBuff();
        buff.Add(_source, _target);

        buff.Remove(_source, _target);
        Hit(10f);

        Assert.AreEqual(90f, _health.Value);
    }

    [Test]
    public void Overlapping_TheFirstRemoved_StaysInvincibleUntilTheLastOne()
    {
        InvincibilityBuff first = CreateBuff();
        InvincibilityBuff second = CreateBuff();
        first.Add(_source, _target);
        second.Add(_source, _target);

        first.Remove(_source, _target);
        Hit(10f);
        Assert.AreEqual(100f, _health.Value);

        second.Remove(_source, _target);
        Hit(10f);
        Assert.AreEqual(90f, _health.Value);
    }

    [Test]
    public void InvincibleValidator_ByDefault_OnlyAcceptsTargetsNotInvincible()
    {
        Assert.IsTrue(_validator.IsValid(_source, _target));

        CreateBuff().Add(_source, _target);

        Assert.IsFalse(_validator.IsValid(_source, _target));
    }

    [Test]
    public void InvincibleValidator_Inverse_OnlyAcceptsInvincibleTargets()
    {
        _validator.data.inverse = true;
        Assert.IsFalse(_validator.IsValid(_source, _target));

        CreateBuff().Add(_source, _target);

        Assert.IsTrue(_validator.IsValid(_source, _target));
    }
}

}
