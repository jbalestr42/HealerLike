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

    static BotUnit Marked(float health, float strikeIn)
    {
        BotUnit unit = Unit(health);
        unit.isMarked = true;
        unit.strikeIn = strikeIn;
        return unit;
    }

    [Test]
    public void StrikeWithin_OnlyWhenTheStrikeOnALivingAllyIsClose()
    {
        HealerBotCondition withinOneSecond = Condition(HealerBotConditionType.StrikeWithin, 1f);

        Assert.IsTrue(HealerBotBrain.IsMet(withinOneSecond, new List<BotUnit> { Unit(100f), Marked(100f, 0.8f) }, 1f));
        Assert.IsTrue(HealerBotBrain.IsMet(withinOneSecond, new List<BotUnit> { Marked(100f, 1f) }, 1f));
        Assert.IsFalse(HealerBotBrain.IsMet(withinOneSecond, new List<BotUnit> { Marked(100f, 2.5f) }, 1f), "too early");
        Assert.IsFalse(HealerBotBrain.IsMet(withinOneSecond, new List<BotUnit> { Unit(100f), Unit(50f) }, 1f), "nobody marked");
        Assert.IsFalse(HealerBotBrain.IsMet(withinOneSecond, new List<BotUnit> { Marked(0f, 0.5f) }, 1f), "marked ally dead");
    }

    [Test]
    public void PickTarget_MarkedAlly_TheLivingMarkedAllyStruckFirst()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(10f), Marked(100f, 2f), Marked(0f, 0.1f), Marked(80f, 0.5f) };

        Assert.AreEqual(3, HealerBotBrain.PickTarget(HealerBotTarget.MarkedAlly, allies, new List<BotUnit>()));
        Assert.AreEqual(1, HealerBotBrain.PickTarget(HealerBotTarget.MarkedAlly, allies, new List<BotUnit>(), i => i == 3));
    }

    [Test]
    public void PickTarget_MarkedAlly_StruckTogether_TheOneTakingTheBiggestPart()
    {
        BotUnit whole = Marked(100f, 0.5f);
        whole.strikeShare = 1f;
        BotUnit half = Marked(100f, 0.5f);
        half.strikeShare = 0.5f;
        BotUnit quarter = Marked(100f, 0.5f);
        quarter.strikeShare = 0.25f;
        List<BotUnit> allies = new List<BotUnit> { quarter, whole, half };

        Assert.AreEqual(1, HealerBotBrain.PickTarget(HealerBotTarget.MarkedAlly, allies, new List<BotUnit>()));
        Assert.AreEqual(2, HealerBotBrain.PickTarget(HealerBotTarget.MarkedAlly, allies, new List<BotUnit>(), i => i == 1), "already protected");
    }

    [Test]
    public void PickTarget_MarkedAllyWithoutMark_NoTarget()
    {
        List<BotUnit> allies = new List<BotUnit> { Unit(10f), Unit(100f) };

        Assert.AreEqual(-1, HealerBotBrain.PickTarget(HealerBotTarget.MarkedAlly, allies, new List<BotUnit>()));
    }

    // Decisions taken over 10s with a 0.25s interval, the bot being updated every deltaTime
    static int CountDecisions(float deltaTime)
    {
        int decisions = 0;
        float next = 0f;
        for (float time = 0f; time < 10f - 0.0001f; time += deltaTime)
        {
            if (time >= next)
            {
                decisions++;
                next = HealerBotBrain.GetNextDecisionTime(next, time, 0.25f);
            }
        }
        return decisions;
    }

    [Test]
    public void Decisions_AsManyWithLongUpdatesAsWithShortOnes()
    {
        Assert.AreEqual(40, CountDecisions(0.03125f));
        Assert.AreEqual(40, CountDecisions(0.125f));
        Assert.AreEqual(40, CountDecisions(0.15625f));
    }

    [Test]
    public void NextDecision_FarBehind_DoesNotPileUpTheMissedOnes()
    {
        Assert.AreEqual(5f, HealerBotBrain.GetNextDecisionTime(1f, 5f, 0.25f));
        Assert.AreEqual(1.25f, HealerBotBrain.GetNextDecisionTime(1f, 1.1f, 0.25f));
    }
}

}
