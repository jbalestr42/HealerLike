using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class CombatStatsTests
{
    static CombatStats.DamageEvent Damage(float time, float amount)
    {
        return new CombatStats.DamageEvent { time = time, amount = amount };
    }

    [Test]
    public void RecordMana_SpentAndGained_AreKeptApart()
    {
        CombatStats stats = new CombatStats();

        stats.RecordMana(-30f, 0f);
        stats.RecordMana(-10f, 0f);
        stats.RecordMana(15f, 5f);

        Assert.AreEqual(40f, stats.manaSpent, 0.001f);
        Assert.AreEqual(10f, stats.manaGained, 0.001f);
        Assert.AreEqual(5f, stats.manaOverflow, 0.001f);
    }

    [Test]
    public void RecordAllyHealth_HealsSplitBetweenCharacterAndUnits_OverhealApart()
    {
        CombatStats stats = new CombatStats();

        stats.RecordAllyHealth(0f, 50f, 20f, true);
        stats.RecordAllyHealth(0f, 10f, 0f, false);
        stats.RecordAllyHealth(0f, 8f, 3f, false);

        Assert.AreEqual(30f, stats.characterHeal, 0.001f);
        Assert.AreEqual(20f, stats.characterOverheal, 0.001f);
        Assert.AreEqual(15f, stats.otherHeal, 0.001f);
        Assert.AreEqual(3f, stats.otherOverheal, 0.001f);
        Assert.AreEqual(0f, stats.allyDamageTaken, 0.001f);
    }

    [Test]
    public void RecordAllyHealth_Damage_AddsToTheDamageTaken()
    {
        CombatStats stats = new CombatStats();

        stats.RecordAllyHealth(1f, -12f, 0f, false);
        stats.RecordAllyHealth(2f, -8f, 0f, true);

        Assert.AreEqual(20f, stats.allyDamageTaken, 0.001f);
        Assert.AreEqual(0f, stats.characterHeal, 0.001f);
    }

    [Test]
    public void RecordEnemyHealth_OnlyCountsDamage()
    {
        CombatStats stats = new CombatStats();

        stats.RecordEnemyHealth(-25f);
        stats.RecordEnemyHealth(10f);

        Assert.AreEqual(25f, stats.enemyDamageTaken, 0.001f);
    }

    [Test]
    public void AddAlly_SumsTheHealthOfEveryAlly()
    {
        CombatStats stats = new CombatStats();

        stats.AddAlly("Knight", 80f, 100f);
        stats.AddAlly("Archer", 50f, 50f);

        CollectionAssert.AreEqual(new[] { "Knight", "Archer" }, stats.allies);
        Assert.AreEqual(130f, stats.allyHealthStart, 0.001f);
        Assert.AreEqual(150f, stats.allyHealthMax, 0.001f);
    }

    [Test]
    public void Finish_SetsTheDurationTheResultAndTheEndValues()
    {
        CombatStats stats = new CombatStats();
        stats.Start(10f, 100f, 100f);

        stats.Finish(42.5f, true, 35f, 90f);

        Assert.IsTrue(stats.won);
        Assert.AreEqual(32.5f, stats.duration, 0.001f);
        Assert.AreEqual(35f, stats.manaEnd, 0.001f);
        Assert.AreEqual(90f, stats.allyHealthEnd, 0.001f);
    }

    [Test]
    public void Shares_AreRelativeToTheMax()
    {
        CombatStats stats = new CombatStats();
        stats.Start(0f, 200f, 200f);
        stats.AddAlly("Knight", 100f, 100f);
        stats.AddAlly("Archer", 60f, 100f);
        stats.RecordMana(-120f, 0f);

        stats.Finish(30f, true, 80f, 110f);

        Assert.AreEqual(0.6f, stats.manaSpentShare, 0.001f);
        Assert.AreEqual(0.4f, stats.manaEndShare, 0.001f);
        // 160 at the start, 110 at the end, out of 200
        Assert.AreEqual(0.25f, stats.allyHealthLostShare, 0.001f);
    }

    [Test]
    public void Finish_HealthNotFromHealsNorDamage_IsTheOtherChange()
    {
        CombatStats stats = new CombatStats();
        stats.Start(0f, 100f, 100f);
        stats.AddAlly("Knight", 100f, 200f);
        stats.RecordAllyHealth(1f, -50f, 0f, false);
        stats.RecordAllyHealth(2f, 20f, 0f, true);
        stats.RecordAllyHealth(3f, 10f, 0f, false);

        // 100 - 50 + 30 = 80 from damage and heals, 25 more set directly (e.g. Balance Life)
        stats.Finish(5f, true, 100f, 105f);

        Assert.AreEqual(25f, stats.allyHealthOtherChange, 0.001f);
    }

    [Test]
    public void Finish_OnlyDamageAndHeals_NoOtherChange()
    {
        CombatStats stats = new CombatStats();
        stats.Start(0f, 100f, 100f);
        stats.AddAlly("Knight", 100f, 100f);
        stats.RecordAllyHealth(1f, -50f, 0f, false);
        stats.RecordAllyHealth(2f, 30f, 10f, true);

        stats.Finish(5f, true, 100f, 70f);

        Assert.AreEqual(0f, stats.allyHealthOtherChange, 0.001f);
    }

    [Test]
    public void Shares_NoMax_AreZero()
    {
        CombatStats stats = new CombatStats();

        Assert.AreEqual(0f, stats.manaSpentShare);
        Assert.AreEqual(0f, stats.manaEndShare);
        Assert.AreEqual(0f, stats.allyHealthLostShare);
    }

    [Test]
    public void EnemyDps_IsTheDamageTakenByTheAlliesPerSecondOfTheFight()
    {
        CombatStats stats = new CombatStats { allyDamageTaken = 600f, duration = 30f, allyPeakDamage = 90f };

        Assert.AreEqual(20f, stats.enemyDps, 0.001f);
        Assert.AreEqual(90f / CombatStats.PeakWindow, stats.enemyPeakDps, 0.001f);
    }

    [Test]
    public void EnemyDps_NoDuration_IsZero()
    {
        Assert.AreEqual(0f, new CombatStats { allyDamageTaken = 600f }.enemyDps);
    }

    [Test]
    public void GetPeakDamage_NoDamage_IsZero()
    {
        Assert.AreEqual(0f, CombatStats.GetPeakDamage(new List<CombatStats.DamageEvent>(), 3f));
    }

    [Test]
    public void GetPeakDamage_KeepsTheWorstWindow()
    {
        List<CombatStats.DamageEvent> events = new List<CombatStats.DamageEvent>
        {
            Damage(0f, 10f),
            Damage(1f, 10f),
            // Burst: 15 + 20 + 25 within 2 seconds
            Damage(5f, 15f),
            Damage(6f, 20f),
            Damage(7f, 25f),
            Damage(12f, 30f),
        };

        Assert.AreEqual(60f, CombatStats.GetPeakDamage(events, 3f), 0.001f);
    }

    [Test]
    public void GetPeakDamage_DamageExactlyAWindowApart_IsNotInTheSameWindow()
    {
        List<CombatStats.DamageEvent> events = new List<CombatStats.DamageEvent>
        {
            Damage(0f, 10f),
            Damage(3f, 10f),
        };

        Assert.AreEqual(10f, CombatStats.GetPeakDamage(events, 3f), 0.001f);
    }

    [Test]
    public void Finish_PeakDamageFromTheRecordedDamage()
    {
        CombatStats stats = new CombatStats();
        stats.Start(0f, 100f, 100f);
        stats.RecordAllyHealth(1f, -10f, 0f, false);
        stats.RecordAllyHealth(2f, -30f, 0f, false);
        stats.RecordAllyHealth(10f, -5f, 0f, false);

        stats.Finish(12f, false, 0f, 0f);

        Assert.AreEqual(40f, stats.allyPeakDamage, 0.001f);
    }

    [Test]
    public void ToJson_KeepsTheContextAndTheStats()
    {
        CombatStats stats = new CombatStats { runId = "run", seed = 7, floor = 3, roomType = "Elite", wave = "Wave_Bulwark", character = "Cleric" };
        stats.AddAlly("Knight", 100f, 100f);
        stats.RecordAllyDeath("Knight");
        stats.RecordMana(-20f, 0f);

        stats.bot = "DruidBot";
        stats.casts.Add(new CombatStats.SkillCasts { skill = "Rejuvenation", count = 4 });

        CombatStats read = JsonUtility.FromJson<CombatStats>(JsonUtility.ToJson(stats));

        Assert.AreEqual("run", read.runId);
        Assert.AreEqual(7, read.seed);
        Assert.AreEqual(3, read.floor);
        Assert.AreEqual("Elite", read.roomType);
        Assert.AreEqual("Wave_Bulwark", read.wave);
        Assert.AreEqual("Cleric", read.character);
        CollectionAssert.AreEqual(new[] { "Knight" }, read.allies);
        CollectionAssert.AreEqual(new[] { "Knight" }, read.deadAllies);
        Assert.AreEqual(20f, read.manaSpent, 0.001f);
        Assert.AreEqual("DruidBot", read.bot);
        Assert.AreEqual(1, read.casts.Count);
        Assert.AreEqual("Rejuvenation", read.casts[0].skill);
        Assert.AreEqual(4, read.casts[0].count);
    }

    [Test]
    public void ToSummary_TimedOut_ShownAsSuch()
    {
        CombatStats stats = new CombatStats { roomType = "Elite", wave = "Wave_Bastion", timedOut = true };

        StringAssert.StartsWith("Timed out Elite 'Wave_Bastion'", stats.ToSummary());
    }

    [Test]
    public void ToSummary_ShowsTheResultAndTheMana()
    {
        CombatStats stats = new CombatStats { roomType = "Combat", wave = "Wave_Crypt", floor = 2 };
        stats.Start(0f, 100f, 100f);
        stats.RecordMana(-60f, 0f);
        stats.Finish(20f, true, 40f, 0f);

        string summary = stats.ToSummary();

        StringAssert.StartsWith("Won Combat 'Wave_Crypt' floor 2", summary);
        StringAssert.Contains("mana spent 60", summary);
        StringAssert.Contains("end 40", summary);
    }
}

}
