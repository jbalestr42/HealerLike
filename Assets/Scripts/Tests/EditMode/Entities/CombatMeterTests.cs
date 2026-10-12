using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The damage and heals an entity really dealt, per fight, over its last seconds and over the run
public class CombatMeterTests
{
    readonly TestUnits _units = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void FightDps_IsTheDamageOfTheFightOverItsDuration()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(10f);
        meter.AddDamage(12f, 50f);
        meter.AddDamage(18f, 50f);

        Assert.AreEqual(10f, meter.GetFightDps(20f), 0.0001f);
        Assert.AreEqual(100f, meter.fightDamage, 0.0001f);
    }

    [Test]
    public void FightHps_IsTheHealOfTheFightOverItsDuration()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddHeal(2f, 30f);

        Assert.AreEqual(3f, meter.GetFightHps(10f), 0.0001f);
        Assert.AreEqual(0f, meter.GetFightDps(10f), 0.0001f);
    }

    [Test]
    public void FightDps_EarlyInTheFight_IsComputedOverAtLeastOneSecond()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(0.1f, 20f);

        Assert.AreEqual(20f, meter.GetFightDps(0.2f), 0.0001f);
    }

    [Test]
    public void RecentDps_OnlyCountsTheLastFiveSeconds()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(1f, 100f);
        meter.AddDamage(16f, 40f);
        meter.AddDamage(19f, 10f);

        // 50 damage over the last 5s, the 100 of the start of the fight is too old
        Assert.AreEqual(10f, meter.GetRecentDps(20f), 0.0001f);
        Assert.AreEqual(7.5f, meter.GetFightDps(20f), 0.0001f);
    }

    [Test]
    public void RecentDps_FightShorterThanTheWindow_IsComputedSinceItsStart()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(1f, 20f);

        Assert.AreEqual(10f, meter.GetRecentDps(2f), 0.0001f);
    }

    [Test]
    public void RecentHps_OnlyCountsTheLastFiveSeconds()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddHeal(2f, 100f);
        meter.AddHeal(18f, 25f);

        Assert.AreEqual(5f, meter.GetRecentHps(20f), 0.0001f);
    }

    [Test]
    public void EndFight_StopsTheClockOfTheFight()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(5f, 100f);
        meter.EndFight(10f);

        Assert.IsFalse(meter.isInFight);
        Assert.AreEqual(10f, meter.GetFightDuration(50f), 0.0001f);
        Assert.AreEqual(10f, meter.GetFightDps(50f), 0.0001f);
    }

    [Test]
    public void DamageAndHeal_OutsideAFight_AreIgnored()
    {
        CombatMeter meter = new CombatMeter();
        meter.AddDamage(1f, 100f);
        meter.StartFight(2f);
        meter.EndFight(4f);
        meter.AddHeal(5f, 100f);

        Assert.AreEqual(0f, meter.runDamage);
        Assert.AreEqual(0f, meter.runHeal);
    }

    [Test]
    public void NewFight_ResetsTheFightButKeepsTheRun()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(5f, 100f);
        meter.AddHeal(5f, 40f);
        meter.EndFight(10f);

        meter.StartFight(100f);
        meter.AddDamage(105f, 200f);

        Assert.AreEqual(200f, meter.fightDamage, 0.0001f);
        Assert.AreEqual(0f, meter.fightHeal, 0.0001f);
        Assert.AreEqual(2, meter.fightCount);
        // 300 damage and 40 heal over the 10s of each fight, the time between fights doesn't count
        Assert.AreEqual(20f, meter.GetRunDuration(110f), 0.0001f);
        Assert.AreEqual(15f, meter.GetRunDps(110f), 0.0001f);
        Assert.AreEqual(2f, meter.GetRunHps(110f), 0.0001f);
    }

    [Test]
    public void StartFight_DuringAFight_ChangesNothing()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(5f, 100f);
        meter.StartFight(6f);

        Assert.AreEqual(100f, meter.fightDamage, 0.0001f);
        Assert.AreEqual(1, meter.fightCount);
        Assert.AreEqual(10f, meter.GetFightDuration(10f), 0.0001f);
    }

    [Test]
    public void NeverFought_HasNoFight()
    {
        CombatMeter meter = new CombatMeter();

        Assert.IsFalse(meter.hasFought);
        Assert.AreEqual(0, meter.fightCount);
        Assert.AreEqual(0f, meter.GetRunDuration(10f));
    }

    [Test]
    public void NotifyAttacker_CountsTheRawDamage_OverkillIncluded()
    {
        Entity attacker = _units.Create(100f, 100f, "Attacker");
        Entity target = _units.Create(10f, 100f, "Target");
        attacker.combatMeter.StartFight(Time.time);

        Entity.NotifyAttacker(attacker.gameObject, target.gameObject, -50f);

        Assert.AreEqual(50f, attacker.combatMeter.fightDamage, 0.0001f);
    }

    [Test]
    public void NotifyAttacker_DamageToItself_IsNotCounted()
    {
        Entity unit = _units.Create(100f, 100f, "Unit");
        unit.combatMeter.StartFight(Time.time);

        Entity.NotifyAttacker(unit.gameObject, unit.gameObject, -20f);

        Assert.AreEqual(0f, unit.combatMeter.fightDamage);
    }

    [Test]
    public void NotifyHealed_CountsTheHealForTheHealer_OverflowIncluded()
    {
        Entity healer = _units.Create(100f, 100f, "Healer");
        Entity target = _units.Create(95f, 100f, "Target");
        healer.combatMeter.StartFight(Time.time);

        Entity.NotifyHealed(healer.gameObject, target.gameObject, new ConsumerResult(30f, false, 25f));

        Assert.AreEqual(30f, healer.combatMeter.fightHeal, 0.0001f);
        Assert.AreEqual(0f, target.combatMeter.fightHeal);
    }
}

}
