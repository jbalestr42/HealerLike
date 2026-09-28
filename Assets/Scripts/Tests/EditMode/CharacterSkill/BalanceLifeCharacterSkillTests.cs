using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// Balance Life: every living unit ends up at the same health percent, the total health being kept
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

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { full, wounded });

        Assert.AreEqual(60f, full.Value, 0.0001f);
        Assert.AreEqual(60f, wounded.Value, 0.0001f);
    }

    [Test]
    public void Balance_DifferentMaxHealth_GivesEveryUnitTheSamePercentAndKeepsTheTotal()
    {
        ResourceAttribute small = CreateHealth(90f, 100f);
        ResourceAttribute tank = CreateHealth(30f, 200f);

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { small, tank });

        // 120 health out of 300: 40% each
        Assert.AreEqual(40f, small.Value, 0.0001f);
        Assert.AreEqual(80f, tank.Value, 0.0001f);
        Assert.AreEqual(120f, small.Value + tank.Value, 0.0001f);
    }

    [Test]
    public void Balance_IgnoresArmorAndInvincibility()
    {
        ResourceAttribute invincible = CreateHealth(100f, 100f);
        invincible.preventConsumers = true;
        invincible.GetComponent<AttributeManager>().Get(AttributeType.PercentArmor).BaseValue = 0.5f;
        ResourceAttribute wounded = CreateHealth(20f, 100f);

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { invincible, wounded });

        Assert.AreEqual(60f, invincible.Value, 0.0001f);
        Assert.AreEqual(60f, wounded.Value, 0.0001f);
    }

    [Test]
    public void Balance_DeadUnit_IsNotRevivedNorCounted()
    {
        ResourceAttribute dead = CreateHealth(0f, 100f);
        ResourceAttribute alive = CreateHealth(50f, 100f);

        BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute> { dead, alive });

        Assert.AreEqual(0f, dead.Value, 0.0001f);
        Assert.AreEqual(50f, alive.Value, 0.0001f);
    }

    [Test]
    public void Balance_WithoutUnit_DoesNothing()
    {
        Assert.DoesNotThrow(() => BalanceLifeCharacterSkill.Balance(new List<ResourceAttribute>()));
    }
}

}
