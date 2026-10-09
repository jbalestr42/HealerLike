using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Consumers
{

// A part of the current (or missing) health of the entity, with another factor on the bosses
public class CurrentHealthValueTests
{
    readonly TestUnits _units = new TestUnits();
    GameplayTag _bossTag;

    [SetUp]
    public void SetUp()
    {
        _bossTag = ScriptableObject.CreateInstance<GameplayTag>();
        _bossTag.name = TagNames.Boss;
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_bossTag);
    }

    static CurrentHealthValue Create(float multiplier, float bossMultiplier = 1f, bool inverse = false)
    {
        return new CurrentHealthValue { data = new CurrentHealthValueData { multiplier = multiplier, bossMultiplier = bossMultiplier, inverse = inverse } };
    }

    // An entity with 400 health out of 1000
    Entity CreateEntity(bool isBoss)
    {
        Entity entity = _units.Create(400f, 1000f);
        if (isBoss)
        {
            entity.AddTag(_bossTag);
        }
        return entity;
    }

    [Test]
    public void BossMultiplier_DefaultsToOne()
    {
        Assert.AreEqual(1f, new CurrentHealthValueData().bossMultiplier);
    }

    [Test]
    public void GetValue_IsAPartOfTheCurrentHealth()
    {
        Assert.AreEqual(20f, Create(0.05f, 0.5f).GetValue(CreateEntity(false).gameObject), 0.0001f);
    }

    [Test]
    public void GetValue_OnABoss_IsMultipliedByTheBossMultiplier()
    {
        Assert.AreEqual(10f, Create(0.05f, 0.5f).GetValue(CreateEntity(true).gameObject), 0.0001f);
    }

    [Test]
    public void GetValue_OnABoss_WithTheDefaultBossMultiplier_IsUnchanged()
    {
        Assert.AreEqual(20f, Create(0.05f).GetValue(CreateEntity(true).gameObject), 0.0001f);
    }

    [Test]
    public void GetValue_Inverse_IsAPartOfTheMissingHealth_AlsoReducedOnABoss()
    {
        Assert.AreEqual(60f, Create(0.1f, 1f, inverse: true).GetValue(CreateEntity(false).gameObject), 0.0001f);
        Assert.AreEqual(30f, Create(0.1f, 0.5f, inverse: true).GetValue(CreateEntity(true).gameObject), 0.0001f);
    }
}

}
