using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Attributes
{

// Every living unit ends up at the same health percent, the average of their percents (relative) or the total
// health over the total max health (absolute), shared by the Balance Life spell and the Arbiter's Balance
public class HealthBalanceTests
{
    readonly List<GameObject> _units = new List<GameObject>();
    readonly TestUnits _entities = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject unit in _units)
        {
            Object.DestroyImmediate(unit);
        }
        _units.Clear();
        _entities.DestroyAll();
    }

    ResourceAttribute CreateHealth(float value, float max)
    {
        GameObject unit = new GameObject("Unit");
        _units.Add(unit);
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(unit, ResourceType.Health, AttributeType.HealthMax, max);
        health.SetValue(value);
        return health;
    }

    [Test]
    public void Balance_SameMaxHealth_GivesEveryUnitTheAverage()
    {
        ResourceAttribute full = CreateHealth(100f, 100f);
        ResourceAttribute wounded = CreateHealth(20f, 100f);

        HealthBalance.Balance(new List<ResourceAttribute> { full, wounded }, BalanceLifeMode.Relative);

        Assert.AreEqual(60f, full.Value, 0.0001f);
        Assert.AreEqual(60f, wounded.Value, 0.0001f);
    }

    // Relative: the percents are averaged, whatever the max health of each unit
    [Test]
    public void Balance_DifferentMaxHealth_GivesEveryUnitTheAverageOfTheirPercents()
    {
        ResourceAttribute tank = CreateHealth(20f, 200f);
        ResourceAttribute small = CreateHealth(80f, 100f);

        HealthBalance.Balance(new List<ResourceAttribute> { tank, small }, BalanceLifeMode.Relative);

        // 10% and 80%: 45% each
        Assert.AreEqual(90f, tank.Value, 0.0001f);
        Assert.AreEqual(45f, small.Value, 0.0001f);
    }

    [Test]
    public void Balance_ThreeUnits_AveragesTheThreePercents()
    {
        ResourceAttribute a = CreateHealth(100f, 100f);
        ResourceAttribute b = CreateHealth(50f, 200f);
        ResourceAttribute c = CreateHealth(20f, 400f);

        HealthBalance.Balance(new List<ResourceAttribute> { a, b, c }, BalanceLifeMode.Relative);

        // 100%, 25% and 5%: 43.33% each
        Assert.AreEqual(43.3333f, a.Value, 0.001f);
        Assert.AreEqual(86.6667f, b.Value, 0.001f);
        Assert.AreEqual(173.3333f, c.Value, 0.001f);
    }

    [Test]
    public void Absolute_DifferentMaxHealth_GivesEveryUnitTheSamePercentAndKeepsTheTotal()
    {
        ResourceAttribute tank = CreateHealth(20f, 200f);
        ResourceAttribute small = CreateHealth(80f, 100f);

        HealthBalance.Balance(new List<ResourceAttribute> { tank, small }, BalanceLifeMode.Absolute);

        // 100 health out of 300: 33.33% each
        Assert.AreEqual(66.6667f, tank.Value, 0.001f);
        Assert.AreEqual(33.3333f, small.Value, 0.001f);
        Assert.AreEqual(100f, tank.Value + small.Value, 0.001f);
    }

    [Test]
    public void Absolute_DeadUnit_IsNotRevivedNorCounted()
    {
        ResourceAttribute dead = CreateHealth(0f, 100f);
        ResourceAttribute alive = CreateHealth(50f, 100f);

        HealthBalance.Balance(new List<ResourceAttribute> { dead, alive }, BalanceLifeMode.Absolute);

        Assert.AreEqual(0f, dead.Value, 0.0001f);
        Assert.AreEqual(50f, alive.Value, 0.0001f);
    }

    [Test]
    public void Balance_IgnoresArmorAndInvincibility()
    {
        ResourceAttribute invincible = CreateHealth(100f, 100f);
        invincible.preventConsumers = true;
        invincible.GetComponent<AttributeManager>().Get(AttributeType.PercentArmor).BaseValue = 0.5f;
        ResourceAttribute wounded = CreateHealth(20f, 100f);

        HealthBalance.Balance(new List<ResourceAttribute> { invincible, wounded }, BalanceLifeMode.Relative);

        Assert.AreEqual(60f, invincible.Value, 0.0001f);
        Assert.AreEqual(60f, wounded.Value, 0.0001f);
    }

    [Test]
    public void Balance_DeadUnit_IsNotRevivedNorCounted()
    {
        ResourceAttribute dead = CreateHealth(0f, 100f);
        ResourceAttribute alive = CreateHealth(50f, 100f);

        HealthBalance.Balance(new List<ResourceAttribute> { dead, alive }, BalanceLifeMode.Relative);

        Assert.AreEqual(0f, dead.Value, 0.0001f);
        Assert.AreEqual(50f, alive.Value, 0.0001f);
    }

    [Test]
    public void Balance_WithoutUnit_DoesNothing()
    {
        Assert.DoesNotThrow(() => HealthBalance.Balance(new List<ResourceAttribute>(), BalanceLifeMode.Relative));
        Assert.DoesNotThrow(() => HealthBalance.Balance(new List<ResourceAttribute>(), BalanceLifeMode.Absolute));
    }

    [Test]
    public void BalanceUnits_EveryUnitAtTheAverageShareOfItsMaxHealth()
    {
        Entity tank = _entities.Create(200f, 200f, "Tank");
        Entity dying = _entities.Create(10f, 100f, "Dying");
        Entity wounded = _entities.Create(30f, 60f, "Wounded");

        // (100% + 10% + 50%) / 3 = 53.33%
        HealthBalance.Balance(new List<GameObject> { tank.gameObject, dying.gameObject, wounded.gameObject }, BalanceLifeMode.Relative);

        Assert.AreEqual(200f * 1.6f / 3f, tank.health.Value, 0.001f);
        Assert.AreEqual(100f * 1.6f / 3f, dying.health.Value, 0.001f);
        Assert.AreEqual(60f * 1.6f / 3f, wounded.health.Value, 0.001f);
    }

    // The average is a share: the tank loses more health than the small unit gains
    [Test]
    public void BalanceUnits_TheAverageIsOfTheShares_NotOfTheHealth()
    {
        Entity tank = _entities.Create(300f, 300f, "Tank");
        Entity small = _entities.Create(0.1f * 50f, 50f, "Small");

        HealthBalance.Balance(new List<GameObject> { tank.gameObject, small.gameObject }, BalanceLifeMode.Relative);

        Assert.AreEqual(0.55f * 300f, tank.health.Value, 0.001f);
        Assert.AreEqual(0.55f * 50f, small.health.Value, 0.001f);
    }

    [Test]
    public void BalanceUnits_TheDeadStayDead_AndAreNotCounted()
    {
        Entity full = _entities.Create(100f, 100f, "Full");
        Entity half = _entities.Create(50f, 100f, "Half");
        Entity dead = _entities.Create(0f, 100f, "Dead");

        HealthBalance.Balance(new List<GameObject> { full.gameObject, half.gameObject, dead.gameObject }, BalanceLifeMode.Relative);

        Assert.AreEqual(75f, full.health.Value, 0.001f);
        Assert.AreEqual(75f, half.health.Value, 0.001f);
        Assert.AreEqual(0f, dead.health.Value);
    }

    [Test]
    public void BalanceUnits_ASingleUnit_KeepsItsHealth()
    {
        Entity unit = _entities.Create(40f, 100f);

        HealthBalance.Balance(new List<GameObject> { unit.gameObject }, BalanceLifeMode.Relative);

        Assert.AreEqual(40f, unit.health.Value, 0.001f);
    }

    [Test]
    public void BalanceUnits_NoUnitOrNull_DoesNothing()
    {
        Assert.DoesNotThrow(() =>
        {
            HealthBalance.Balance(new List<GameObject>(), BalanceLifeMode.Relative);
            HealthBalance.Balance(new List<GameObject> { null }, BalanceLifeMode.Absolute);
        });
    }

    // Set directly: armor, shields and the heal modifiers don't change it
    [Test]
    public void BalanceUnits_IgnoresTheHealingReceived()
    {
        Entity full = _entities.Create(100f, 100f, "Full");
        Entity empty = _entities.Create(20f, 100f, "Wounded");
        Attribute healingReceived = empty.health.GetComponent<AttributeManager>().GetOrAdd(AttributeType.HealingReceived);
        healingReceived.BaseValue = 0.5f;
        healingReceived.Update();

        HealthBalance.Balance(new List<GameObject> { full.gameObject, empty.gameObject }, BalanceLifeMode.Relative);

        Assert.AreEqual(60f, empty.health.Value, 0.001f);
    }
}

}
