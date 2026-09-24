using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class UnitReadoutTests
{
    GameObject _owner;
    GameObject _target;
    Entity _entity;
    ResourceAttribute _health;
    UnitReadout _readout;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("EntityFixture");
        _target = new GameObject("Target");
        _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        TestHelpers.WithLoggingDisabled(() => _entity = _owner.AddComponent<Entity>());
        TestHelpers.SetPrivateField(_entity, "_health", _health);
        _readout = new UnitReadout();
        _readout.Init(_entity);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_target);
    }

    [Test]
    public void Read_LateAddedCooldownSkill_PollsReadinessTargetAndHealth()
    {
        TargetProvider provider = null;
        TestHelpers.WithLoggingDisabled(() => provider = _owner.AddComponent<TargetProvider>());
        _target.transform.position = Vector3.right * 3f;
        TestHelpers.SetPrivateField(provider, "_targets", new List<GameObject> { _target });
        ShootProjectileSkill skill = _owner.AddComponent<ShootProjectileSkill>();
        TestHelpers.SetPrivateField(skill, "_cooldownDuration", new Attribute(2));
        skill.isEnabled = true;
        TestHelpers.SetPrivateField(_health, "_value", 25f);
        FieldInfo cooldown = typeof(ACooldownSkill<ShootProjectileSkillData>).GetField("_cooldown",
            BindingFlags.NonPublic | BindingFlags.Instance);

        _readout.Read();

        Assert.AreEqual(1f, _readout.readiness);
        Assert.AreEqual(0.25f, _readout.healthFraction);
        Assert.AreEqual(Vector3.right * 3f, _readout.target);
        cooldown.SetValue(skill, 0.5f);
        _readout.Read();
        Assert.AreEqual(0.75f, _readout.readiness);
        cooldown.SetValue(skill, 2f);
        _readout.Read();
        Assert.AreEqual(0f, _readout.readiness);
    }

    [Test]
    public void Read_RemovedSkillAndHealedEntity_DropsTheSkillAndReadsFullHealth()
    {
        ShootProjectileSkill skill = _owner.AddComponent<ShootProjectileSkill>();
        TestHelpers.SetPrivateField(skill, "_cooldownDuration", new Attribute(2));
        skill.isEnabled = true;
        _readout.Read();
        Object.DestroyImmediate(skill);
        TestHelpers.SetPrivateField(_health, "_value", 100f);

        _readout.Read();

        Assert.AreEqual(0f, _readout.readiness);
        Assert.AreEqual(1f, _readout.healthFraction);
    }
}

}
