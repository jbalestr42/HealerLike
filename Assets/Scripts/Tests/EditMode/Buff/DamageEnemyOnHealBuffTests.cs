using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The enemies come from the EntityManager singleton in the game
public class TestDamageEnemyOnHealBuff : DamageEnemyOnHealBuff
{
    public List<GameObject> enemies = new List<GameObject>();

    protected override List<GameObject> GetEnemies() => enemies;
}

// Thorns of Life: each heal received by the holder hurts one of its enemies for a part of it
public class DamageEnemyOnHealBuffTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    Entity _near;
    Entity _far;
    TestDamageEnemyOnHealBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(50f, 100f, "Owner");
        _near = _units.Create(100f, 100f, "Near");
        _near.transform.position = new Vector3(2f, 0f, 0f);
        _far = _units.Create(60f, 100f, "Far");
        _far.transform.position = new Vector3(5f, 0f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    void AddBuff(TargetBehaviourType targetType)
    {
        _buff = new TestDamageEnemyOnHealBuff { data = new DamageEnemyOnHealBuffData { ratio = 0.25f, targetType = targetType } };
        _buff.enemies = new List<GameObject> { _far.gameObject, _near.gameObject };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    void Heal(float amount)
    {
        Entity.NotifyHealed(null, _owner.gameObject, new ConsumerResult(amount, false));
    }

    [Test]
    public void Heal_Received_DamagesTheNearestEnemyForTheRatio()
    {
        AddBuff(TargetBehaviourType.Nearest);

        Heal(20f);
        TestUnits.Process(_near.health);

        Assert.AreEqual(95f, _near.health.Value, 0.0001f);
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_far));
    }

    [Test]
    public void Heal_Received_TheEnemyFollowsTheTargetType()
    {
        AddBuff(TargetBehaviourType.LowestHealth);

        Heal(20f);
        TestUnits.Process(_far.health);

        Assert.AreEqual(55f, _far.health.Value, 0.0001f);
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_near));
    }

    [Test]
    public void Heal_Received_TheDamageComesFromTheHolder()
    {
        AddBuff(TargetBehaviourType.Nearest);

        Heal(20f);

        Assert.AreSame(_owner.gameObject, TestUnits.GetPendingModifiers(_near)[0].source);
    }

    [Test]
    public void Heal_Received_TheDamageIsReducedByArmorAndPreventedByInvincibility()
    {
        AddBuff(TargetBehaviourType.Nearest);

        Heal(20f);

        AConsumer damageConsumer = TestUnits.GetPendingModifiers(_near)[0].consumers[0];
        Assert.IsFalse(damageConsumer.ignoreDamageReduction);
        Assert.IsFalse(damageConsumer.ignoreConsumerPrevention);
    }

    [Test]
    public void Heal_Received_SkipsADeadEnemy()
    {
        AddBuff(TargetBehaviourType.Nearest);
        _near.health.SetValue(0f);

        Heal(20f);
        TestUnits.Process(_far.health);

        Assert.AreEqual(55f, _far.health.Value, 0.0001f);
    }

    [Test]
    public void Heal_WithoutEnemy_DoesNothing()
    {
        AddBuff(TargetBehaviourType.Nearest);
        _buff.enemies.Clear();

        Assert.DoesNotThrow(() => Heal(20f));
    }

    [Test]
    public void Damage_Received_HurtsNobody()
    {
        AddBuff(TargetBehaviourType.Nearest);

        Heal(-20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_near));
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_far));
    }

    [Test]
    public void Heal_AfterRemove_HurtsNobody()
    {
        AddBuff(TargetBehaviourType.Nearest);
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Heal(20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_near));
    }
}

}
