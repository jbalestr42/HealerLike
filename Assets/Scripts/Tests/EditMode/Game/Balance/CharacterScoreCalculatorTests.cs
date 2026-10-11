using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Balance
{

public class CharacterScoreCalculatorTests
{
    // The starting units dealing damage to the dummies during duration seconds
    static CombatStats DpsFight(string character, float damage, float duration)
    {
        return new CombatStats { character = character, enemyDamageTaken = damage, duration = duration, timedOut = true };
    }

    // The starting units against the balance team until they all die, or until the time limit
    static CombatStats RobustnessFight(string character, float duration, float heal = 0f, float mana = 0f, bool timedOut = false)
    {
        return new CombatStats { character = character, duration = duration, characterHeal = heal, manaSpent = mana, timedOut = timedOut };
    }

    [Test]
    public void Compute_AveragesTheSeeds_AndCombinesTheFourMeasures()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight("Cleric", 300f, 30f), DpsFight("Cleric", 360f, 30f) };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight("Cleric", 9f), RobustnessFight("Cleric", 11f) };
        List<CombatStats> spellDps = new List<CombatStats> { DpsFight("Cleric", 420f, 30f), DpsFight("Cleric", 480f, 30f) };
        List<CombatStats> spellRobustness = new List<CombatStats> { RobustnessFight("Cleric", 20f, 100f, 40f), RobustnessFight("Cleric", 40f, 240f, 120f) };

        CharacterScore score = CharacterScoreCalculator.Compute(dps, robustness, spellDps, spellRobustness, 40f)["Cleric"];

        Assert.AreEqual(11f, score.dps, 0.001f);
        Assert.AreEqual(10f, score.survivalTime, 0.001f);
        Assert.AreEqual(400f, score.effectiveHealth, 0.001f);
        // The damage dealt before dying: 11 dps for 10s
        Assert.AreEqual(110f, score.threat, 0.001f);
        Assert.AreEqual(15f, score.spellDps, 0.001f);
        Assert.AreEqual(30f, score.spellSurvivalTime, 0.001f);
        Assert.AreEqual(1200f, score.spellEffectiveHealth, 0.001f);
        // With the skills: 15 dps for 30s
        Assert.AreEqual(450f, score.spellThreat, 0.001f);
        // Per fight, then averaged: (100/20 + 240/40) / 2 and (40/20 + 120/40) / 2
        Assert.AreEqual(5.5f, score.healingPerSecond, 0.001f);
        Assert.AreEqual(2.5f, score.manaPerSecond, 0.001f);
        Assert.IsFalse(score.timedOut);
        Assert.IsTrue(string.IsNullOrEmpty(score.fingerprint));
    }

    [Test]
    public void Compute_EachCharacterFromItsOwnFights()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight("Cleric", 300f, 30f), DpsFight("Druid", 600f, 30f) };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight("Cleric", 10f), RobustnessFight("Druid", 20f) };
        List<CombatStats> spellDps = new List<CombatStats> { DpsFight("Cleric", 450f, 30f), DpsFight("Druid", 900f, 30f) };
        List<CombatStats> spellRobustness = new List<CombatStats> { RobustnessFight("Cleric", 15f), RobustnessFight("Druid", 25f) };

        Dictionary<string, CharacterScore> scores = CharacterScoreCalculator.Compute(dps, robustness, spellDps, spellRobustness, 40f);

        Assert.AreEqual(10f, scores["Cleric"].dps, 0.001f);
        Assert.AreEqual(20f, scores["Druid"].dps, 0.001f);
        Assert.AreEqual(10f, scores["Cleric"].survivalTime, 0.001f);
        Assert.AreEqual(15f, scores["Cleric"].spellDps, 0.001f);
        Assert.AreEqual(30f, scores["Druid"].spellDps, 0.001f);
        Assert.AreEqual(25f, scores["Druid"].spellSurvivalTime, 0.001f);
    }

    [Test]
    public void Compute_AFightReachingTheTimeLimit_TheScoreIsALowerBound()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight("Cleric", 300f, 30f) };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight("Cleric", 10f) };
        List<CombatStats> spellDps = new List<CombatStats> { DpsFight("Cleric", 300f, 30f) };
        List<CombatStats> spellRobustness = new List<CombatStats> { RobustnessFight("Cleric", 180f, timedOut: true) };

        Assert.IsTrue(CharacterScoreCalculator.Compute(dps, robustness, spellDps, spellRobustness, 40f)["Cleric"].timedOut);
    }

    [Test]
    public void Compute_ACharacterMissingAMeasure_NotScored()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight("Cleric", 300f, 30f), DpsFight("Druid", 300f, 30f), DpsFight("Warlock", 300f, 30f) };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight("Cleric", 10f), RobustnessFight("Druid", 10f), RobustnessFight("Warlock", 10f) };
        // The Druid misses its damage with the skills, the Warlock its survival with the skills
        List<CombatStats> spellDps = new List<CombatStats> { DpsFight("Cleric", 300f, 30f), DpsFight("Warlock", 300f, 30f) };
        List<CombatStats> spellRobustness = new List<CombatStats> { RobustnessFight("Cleric", 15f), RobustnessFight("Druid", 15f) };

        Dictionary<string, CharacterScore> scores = CharacterScoreCalculator.Compute(dps, robustness, spellDps, spellRobustness, 40f);

        Assert.IsTrue(scores.ContainsKey("Cleric"));
        Assert.IsFalse(scores.ContainsKey("Druid"));
        Assert.IsFalse(scores.ContainsKey("Warlock"));
    }
}

}
