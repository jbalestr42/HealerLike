using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Balance
{

public class HealerBotBrainTests
{
    static BotUnit Unit(float health, float maxHealth = 100f, bool isTank = false)
    {
        return new BotUnit { health = health, maxHealth = maxHealth, isTank = isTank };
    }

    static HealerBotCondition Condition(HealerBotConditionType type, float value, int count = 1)
    {
        return new HealerBotCondition { type = type, value = value, count = count };
    }

    [Test]
    public void BotUnit_HealthPercentAndAlive()
    {
        Assert.AreEqual(0.25f, Unit(50f, 200f).healthPercent, 0.001f);
        Assert.IsTrue(Unit(1f).isAlive);
        Assert.IsFalse(Unit(0f).isAlive);
        Assert.AreEqual(0f, Unit(10f, 0f).healthPercent);
    }

    [Test]
    public void AlliesBelowHealth_CountsOnlyTheLivingAlliesBelow()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(20f), Unit(0f), Unit(50f), Unit(90f) };

        Assert.AreEqual(2, HealerBotBrain.CountAlliesBelow(allies, 0.6f));
        Assert.IsTrue(HealerBotBrain.IsMet(Condition(HealerBotConditionType.AlliesBelowHealth, 0.6f), allies, 1f));
        Assert.IsTrue(HealerBotBrain.IsMet(Condition(HealerBotConditionType.AlliesBelowHealth, 0.6f, 2), allies, 1f));
        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.AlliesBelowHealth, 0.6f, 3), allies, 1f));
        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.AlliesBelowHealth, 0.1f), allies, 1f));
    }

    [Test]
    public void TeamHealth_AverageOfTheLivingAllies()
    {
        // 20%, 60%, 100%, the dead one aside
        List<BotUnit> allies = new List<BotUnit> { Unit(20f), Unit(120f, 200f), Unit(0f), Unit(50f, 50f) };

        Assert.AreEqual(0.6f, HealerBotBrain.GetAverageHealth(allies), 0.001f);
        Assert.IsTrue(HealerBotBrain.IsMet(Condition(HealerBotConditionType.TeamHealthBelow, 0.7f), allies, 1f));
        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.TeamHealthBelow, 0.5f), allies, 1f));
        Assert.IsTrue(HealerBotBrain.IsMet(Condition(HealerBotConditionType.TeamHealthAbove, 0.5f), allies, 1f));
        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.TeamHealthAbove, 0.7f), allies, 1f));
    }

    [Test]
    public void TeamHealth_EveryAllyDead_NeitherBelowNorAbove()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(0f) };

        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.TeamHealthBelow, 0.7f), allies, 1f));
        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.TeamHealthAbove, 0f), allies, 1f));
    }

    [Test]
    public void HealthSpread_MostMinusLeastHealthyLivingAlly()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(30f), Unit(0f), Unit(90f), Unit(60f) };

        Assert.AreEqual(0.6f, HealerBotBrain.GetHealthSpread(allies), 0.001f);
        Assert.IsTrue(HealerBotBrain.IsMet(Condition(HealerBotConditionType.HealthSpreadAbove, 0.4f), allies, 1f));
        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.HealthSpreadAbove, 0.7f), allies, 1f));
    }

    [Test]
    public void HealthSpread_LessThanTwoLivingAllies_Zero()
    {
        Assert.AreEqual(0f, HealerBotBrain.GetHealthSpread(new List<BotUnit> { Unit(10f), Unit(0f) }));
    }

    [Test]
    public void ManaBelow_ComparesTheManaOfTheCharacter()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(100f) };

        Assert.IsTrue(HealerBotBrain.IsMet(Condition(HealerBotConditionType.ManaBelow, 0.3f), allies, 0.2f));
        Assert.IsFalse(HealerBotBrain.IsMet(Condition(HealerBotConditionType.ManaBelow, 0.3f), allies, 0.5f));
    }

    [Test]
    public void AreMet_EveryConditionNeeded_NoConditionAlwaysMet()
    {
        // Dark Pact: low mana and a healthy team
        List<HealerBotCondition> conditions = new List<HealerBotCondition>
        {
            Condition(HealerBotConditionType.ManaBelow, 0.3f),
            Condition(HealerBotConditionType.TeamHealthAbove, 0.7f),
        };
        List<BotUnit> healthy = new List<BotUnit> { Unit(90f), Unit(80f) };
        List<BotUnit> wounded = new List<BotUnit> { Unit(30f), Unit(80f) };

        Assert.IsTrue(HealerBotBrain.AreMet(conditions, healthy, 0.1f));
        Assert.IsFalse(HealerBotBrain.AreMet(conditions, wounded, 0.1f));
        Assert.IsFalse(HealerBotBrain.AreMet(conditions, healthy, 0.5f));
        Assert.IsTrue(HealerBotBrain.AreMet(new List<HealerBotCondition>(), wounded, 0f));
    }

    [Test]
    public void PickTarget_LowestHealthAlly_ByPercent_LivingAndNotExcluded()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(50f), Unit(0f), Unit(60f, 200f), Unit(40f) };

        Assert.AreEqual(2, HealerBotBrain.PickTarget(HealerBotTarget.LowestHealthAlly, allies, new List<BotUnit>()));
        Assert.AreEqual(3, HealerBotBrain.PickTarget(HealerBotTarget.LowestHealthAlly, allies, new List<BotUnit>(), i => i == 2));
    }

    [Test]
    public void PickTarget_Tank_TheTankWithTheMostMaxHealth()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(400f, 400f), Unit(100f, 150f, true), Unit(300f, 300f, true) };

        Assert.AreEqual(2, HealerBotBrain.PickTarget(HealerBotTarget.Tank, allies, new List<BotUnit>()));
    }

    [Test]
    public void PickTarget_TankAlreadyBuffed_NoTarget()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(400f, 400f), Unit(300f, 300f, true) };

        Assert.AreEqual(-1, HealerBotBrain.PickTarget(HealerBotTarget.Tank, allies, new List<BotUnit>(), i => i == 1));
    }

    [Test]
    public void PickTarget_TankWithoutATank_TheAllyWithTheMostMaxHealth()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(100f, 100f), Unit(200f, 200f), Unit(0f, 500f, true) };

        Assert.AreEqual(1, HealerBotBrain.PickTarget(HealerBotTarget.Tank, allies, new List<BotUnit>()));
    }

    [Test]
    public void PickTarget_Enemies_ByTheirHealth()
    {
        List<BotUnit> enemies = new List<BotUnit> { Unit(80f), Unit(0f), Unit(30f), Unit(500f, 600f) };

        Assert.AreEqual(2, HealerBotBrain.PickTarget(HealerBotTarget.LowestHealthEnemy, new List<BotUnit>(), enemies));
        Assert.AreEqual(3, HealerBotBrain.PickTarget(HealerBotTarget.HighestHealthEnemy, new List<BotUnit>(), enemies));
    }

    [Test]
    public void PickTarget_NoLivingUnitOrNoTarget_None()
    {
        List<BotUnit> dead = new List<BotUnit> { Unit(0f) };

        Assert.AreEqual(-1, HealerBotBrain.PickTarget(HealerBotTarget.LowestHealthAlly, dead, dead));
        Assert.AreEqual(-1, HealerBotBrain.PickTarget(HealerBotTarget.LowestHealthEnemy, dead, dead));
        Assert.AreEqual(-1, HealerBotBrain.PickTarget(HealerBotTarget.None, new List<BotUnit> { Unit(10f) }, dead));
        Assert.AreEqual(-1, HealerBotBrain.PickTarget(HealerBotTarget.FrontCell, new List<BotUnit> { Unit(10f) }, dead));
    }
}

}
