using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// Drain Life: damages an enemy and heals the most wounded ally by the damage it actually took
public class DrainLifeCharacterSkillTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _warlock;
    Entity _enemy;
    Entity _wounded;
    List<GameObject> _allies;

    [SetUp]
    public void SetUp()
    {
        _warlock = new GameObject("Warlock");
        TestHelpers.CreateAttributeManager(_warlock);
        _enemy = _units.Create(100f, 100f, "Enemy");
        _wounded = _units.Create(20f, 100f, "Wounded");
        _allies = new List<GameObject> { _units.Create(100f, 100f, "Full").gameObject, _wounded.gameObject };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_warlock);
    }

    ResourceModifier CreateDamage(float damage)
    {
        ResourceModifier modifier = new ResourceModifier { source = _warlock };
        modifier.consumers.Add(new RuntimeConsumer(-damage, ignoreDamageReduction: false, ignoreConsumerPrevention: false));
        return modifier;
    }

    [Test]
    public void Drain_DamagesTheEnemyAndHealsTheMostWoundedAllyByTheDamage()
    {
        DrainLifeCharacterSkill.Drain(_warlock, _enemy.health, CreateDamage(15f), _allies, 1f);
        TestUnits.Process(_enemy.health);
        TestUnits.Process(_wounded.health);

        Assert.AreEqual(85f, _enemy.health.Value, 0.0001f);
        Assert.AreEqual(35f, _wounded.health.Value, 0.0001f);
    }

    [Test]
    public void Drain_HealsTheDamageAfterTheEnemyArmor()
    {
        _enemy.health.GetComponent<AttributeManager>().Get(AttributeType.PercentArmor).BaseValue = 0.5f;
        _enemy.health.GetComponent<AttributeManager>().Get(AttributeType.PercentArmor).Update();

        DrainLifeCharacterSkill.Drain(_warlock, _enemy.health, CreateDamage(20f), _allies, 1f);
        TestUnits.Process(_enemy.health);
        TestUnits.Process(_wounded.health);

        Assert.AreEqual(90f, _enemy.health.Value, 0.0001f);
        Assert.AreEqual(30f, _wounded.health.Value, 0.0001f);
    }

    [Test]
    public void Drain_HealRatio_ScalesTheHeal()
    {
        DrainLifeCharacterSkill.Drain(_warlock, _enemy.health, CreateDamage(20f), _allies, 0.5f);
        TestUnits.Process(_enemy.health);
        TestUnits.Process(_wounded.health);

        Assert.AreEqual(30f, _wounded.health.Value, 0.0001f);
    }

    [Test]
    public void Drain_InvincibleEnemy_HealsNothing()
    {
        _enemy.health.preventConsumers = true;

        DrainLifeCharacterSkill.Drain(_warlock, _enemy.health, CreateDamage(20f), _allies, 1f);
        TestUnits.Process(_enemy.health);

        Assert.AreEqual(100f, _enemy.health.Value, 0.0001f);
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_wounded));
    }

    [Test]
    public void Drain_OtherDamageOnTheEnemy_HealsNothing()
    {
        DrainLifeCharacterSkill.Drain(_warlock, _enemy.health, CreateDamage(15f), _allies, 1f);
        TestUnits.Process(_enemy.health);
        TestUnits.Process(_wounded.health);

        // Only the drain heals, and only once
        _enemy.health.AddResourceModifier(CreateDamage(10f));
        TestUnits.Process(_enemy.health);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_wounded));
        Assert.AreEqual(35f, _wounded.health.Value, 0.0001f);
    }
}

}
