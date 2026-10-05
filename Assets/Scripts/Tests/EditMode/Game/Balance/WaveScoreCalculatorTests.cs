using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Balance
{

public class WaveScoreCalculatorTests
{
    // A wave dealing damage to the dummies during duration seconds, with a peak over CombatStats.PeakWindow
    static CombatStats DpsFight(string wave, float damage, float duration, float peak)
    {
        return new CombatStats { wave = wave, allyDamageTaken = damage, duration = duration, allyPeakDamage = peak, timedOut = true };
    }

    static CombatStats RobustnessFight(string wave, float duration, bool timedOut = false)
    {
        return new CombatStats { wave = wave, duration = duration, won = !timedOut, timedOut = timedOut };
    }

    [Test]
    public void TeamDps_IsTheMeanDamagePerSecondDealtToTheDummies()
    {
        List<CombatStats> fights = new List<CombatStats>
        {
            new CombatStats { enemyDamageTaken = 1200f, duration = 30f },
            new CombatStats { enemyDamageTaken = 1260f, duration = 30f },
        };

        Assert.AreEqual(41f, WaveScoreCalculator.GetTeamDps(fights), 0.001f);
        Assert.AreEqual(0f, WaveScoreCalculator.GetTeamDps(new List<CombatStats>()));
    }

    [Test]
    public void Compute_AveragesTheSeeds_AndCombinesBothMeasures()
    {
        List<CombatStats> dps = new List<CombatStats>
        {
            DpsFight("Wave_A", 600f, 30f, 60f),
            DpsFight("Wave_A", 540f, 30f, 30f),
        };
        List<CombatStats> robustness = new List<CombatStats>
        {
            RobustnessFight("Wave_A", 14f),
            RobustnessFight("Wave_A", 16f),
        };

        WaveScore score = WaveScoreCalculator.Compute(dps, robustness, 40f)["Wave_A"];

        Assert.AreEqual(19f, score.dps, 0.001f);
        Assert.AreEqual(45f / CombatStats.PeakWindow, score.peakDps, 0.001f);
        Assert.AreEqual(15f, score.survivalTime, 0.001f);
        Assert.AreEqual(600f, score.effectiveHealth, 0.001f);
        Assert.AreEqual(285f, score.threat, 0.001f);
        Assert.IsFalse(score.timedOut);
        Assert.IsFalse(score.measured);
    }

    [Test]
    public void Compute_ASurvivalStoppedByTheTimeLimit_IsALowerBound()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight("Wave_A", 300f, 30f, 30f) };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight("Wave_A", 50f), RobustnessFight("Wave_A", 180f, true) };

        Assert.IsTrue(WaveScoreCalculator.Compute(dps, robustness, 40f)["Wave_A"].timedOut);
    }

    [Test]
    public void Compute_AWaveMeasuredOneWayOnly_HasNoScore()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight("Wave_A", 300f, 30f, 30f), DpsFight("Wave_B", 300f, 30f, 30f) };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight("Wave_A", 10f), RobustnessFight("Wave_C", 10f) };

        CollectionAssert.AreEquivalent(new[] { "Wave_A" }, WaveScoreCalculator.Compute(dps, robustness, 40f).Keys);
    }
}

}
