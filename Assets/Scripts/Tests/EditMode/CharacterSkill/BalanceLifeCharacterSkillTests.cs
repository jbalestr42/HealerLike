using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// Balance Life: every living unit ends up at the same health percent, the average of their percents
// (relative, by default) or the total health over the total max health (absolute)
public class BalanceLifeCharacterSkillTests
{
    readonly List<GameObject> _units = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject unit in _units)
        {
            Object.DestroyImmediate(unit);
        }
        _units.Clear();
    }

    ResourceAttribute CreateHealth(float value, float max)
    {
        GameObject unit = new GameObject("Unit");
        _units.Add(unit);
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(unit, AttributeType.HealthMax, max);
        health.SetValue(value);
        return health;
    }

    [Test]
    public void Balance_SameMaxHealth_GivesEveryUnitTheAverage()
    {
        ResourceAttribute full = CreateHealth(100f, 100f);
        ResourceAttribute wounded = CreateHealth(20f, 100f);

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { full, wounded }, BalanceLifeMode.Relative);

        Assert.AreEqual(60f, full.Value, 0.0001f);
        Assert.AreEqual(60f, wounded.Value, 0.0001f);
    }

    // Relative: the percents are averaged, whatever the max health of each unit
    [Test]
    public void Balance_DifferentMaxHealth_GivesEveryUnitTheAverageOfTheirPercents()
    {
        ResourceAttribute tank = CreateHealth(20f, 200f);
        ResourceAttribute small = CreateHealth(80f, 100f);

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { tank, small }, BalanceLifeMode.Relative);

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

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { a, b, c }, BalanceLifeMode.Relative);

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

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { tank, small }, BalanceLifeMode.Absolute);

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

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { dead, alive }, BalanceLifeMode.Absolute);

        Assert.AreEqual(0f, dead.Value, 0.0001f);
        Assert.AreEqual(50f, alive.Value, 0.0001f);
    }

    [Test]
    public void Mode_IsRelativeByDefault()
    {
        Assert.AreEqual(BalanceLifeMode.Relative, new BalanceLifeCharacterSkillData().mode);
    }

    [Test]
    public void Balance_IgnoresArmorAndInvincibility()
    {
        ResourceAttribute invincible = CreateHealth(100f, 100f);
        invincible.preventConsumers = true;
        invincible.GetComponent<AttributeManager>().Get(AttributeType.PercentArmor).BaseValue = 0.5f;
        ResourceAttribute wounded = CreateHealth(20f, 100f);

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { invincible, wounded }, BalanceLifeMode.Relative);

        Assert.AreEqual(60f, invincible.Value, 0.0001f);
        Assert.AreEqual(60f, wounded.Value, 0.0001f);
    }

    [Test]
    public void Balance_DeadUnit_IsNotRevivedNorCounted()
    {
        ResourceAttribute dead = CreateHealth(0f, 100f);
        ResourceAttribute alive = CreateHealth(50f, 100f);

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { dead, alive }, BalanceLifeMode.Relative);

        Assert.AreEqual(0f, dead.Value, 0.0001f);
        Assert.AreEqual(50f, alive.Value, 0.0001f);
    }

    [Test]
    public void Balance_WithoutUnit_DoesNothing()
    {
        Assert.DoesNotThrow(() => BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute>(), BalanceLifeMode.Relative));
        Assert.DoesNotThrow(() => BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute>(), BalanceLifeMode.Absolute));
    }
}

}
