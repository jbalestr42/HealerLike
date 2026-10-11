using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Balance
{

public class ItemScoreCalculatorTests
{
    float _noEffectShare;

    // Whatever the threshold set in the Scores window
    [SetUp]
    public void SetUp()
    {
        _noEffectShare = ItemScore.noEffectShare;
        ItemScore.noEffectShare = ItemScore.DefaultNoEffectShare;
    }

    [TearDown]
    public void TearDown()
    {
        ItemScore.noEffectShare = _noEffectShare;
    }

    [Test]
    public void HasEffect_FromTheThreshold_EitherWay()
    {
        ItemScore.noEffectShare = 0.03f;

        Assert.IsFalse(ItemScore.HasEffect(0.029f));
        Assert.IsFalse(ItemScore.HasEffect(-0.029f));
        Assert.IsTrue(ItemScore.HasEffect(0.03f));
        Assert.IsTrue(ItemScore.HasEffect(-0.05f));

        ItemScore.noEffectShare = 0.01f;

        Assert.IsTrue(ItemScore.HasEffect(0.029f));
    }

    [Test]
    public void HasEffect_ByDefault_UnderTheNoiseOfTheFights_ThreePercent()
    {
        Assert.AreEqual(0.03f, ItemScore.DefaultNoEffectShare, 0.0001f);
    }

    // The balance team dealing damage to the dummies during 30s, one of its units holding the item (none without)
    static CombatStats DpsFight(int seed, float damage, string item = "", string holder = "")
    {
        return new CombatStats { seed = seed, enemyDamageTaken = damage, duration = 30f, timedOut = true, item = item, itemHolder = holder };
    }

    // The dummies against the balance team until they all die, one of them holding the item (none without)
    static CombatStats RobustnessFight(int seed, float duration, string item = "", bool timedOut = false)
    {
        return new CombatStats { seed = seed, duration = duration, item = item, itemHolder = item != "" ? "Balance Dummy" : "", timedOut = timedOut };
    }

    [Test]
    public void Compute_TheGainsOfAnItem_ComparedWithTheFightsWithoutItem()
    {
        // Without item: 40 dps, the dummies survive 10s, so 400 effective health
        List<CombatStats> dps = new List<CombatStats> { DpsFight(1, 1200f), DpsFight(1, 1500f, "WhetstoneItem", "Sniper") };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f), RobustnessFight(1, 12f, "WhetstoneItem") };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore baseline)["WhetstoneItem"];

        Assert.AreEqual(40f, baseline.dps, 0.001f);
        Assert.AreEqual(10f, baseline.survivalTime, 0.001f);
        Assert.AreEqual(400f, baseline.effectiveHealth, 0.001f);
        Assert.AreEqual(50f, score.dps, 0.001f);
        Assert.AreEqual(10f, score.dpsGain, 0.001f);
        Assert.AreEqual(0.25f, score.dpsGainShare, 0.001f);
        Assert.AreEqual(12f, score.survivalTime, 0.001f);
        // Measured with the damage of the balance team without item: 12s x 40 dps
        Assert.AreEqual(480f, score.effectiveHealth, 0.001f);
        Assert.AreEqual(80f, score.effectiveHealthGain, 0.001f);
        Assert.AreEqual(0.2f, score.effectiveHealthGainShare, 0.001f);
        Assert.IsTrue(score.hasDpsEffect);
        Assert.IsTrue(score.hasRobustnessEffect);
        // As the threat of a wave: 40 dps x 10s without item, 50 dps x 12s with it
        Assert.AreEqual(400f, baseline.power, 0.001f);
        Assert.AreEqual(600f, score.power, 0.001f);
        Assert.AreEqual(200f, score.powerGain, 0.001f);
        // Both gains combined: 1.25 x 1.2 - 1
        Assert.AreEqual(0.5f, score.powerGainShare, 0.001f);
        Assert.IsTrue(score.hasPowerEffect);
        Assert.IsFalse(score.timedOut);
        Assert.IsTrue(string.IsNullOrEmpty(score.fingerprint));
    }

    [Test]
    public void Compute_EachFightComparedWithTheFightWithoutItemOfItsSeed()
    {
        // Seed 1 is a lucky fight (60 dps), seed 2 an unlucky one (20 dps): the item adds 3 dps to each
        List<CombatStats> dps = new List<CombatStats>
        {
            DpsFight(1, 1800f), DpsFight(2, 600f),
            DpsFight(1, 1890f, "WhetstoneItem", "Sniper"), DpsFight(2, 690f, "WhetstoneItem", "Gunner"),
        };
        List<CombatStats> robustness = new List<CombatStats>
        {
            RobustnessFight(1, 8f), RobustnessFight(2, 12f),
            RobustnessFight(1, 9f, "WhetstoneItem"), RobustnessFight(2, 13f, "WhetstoneItem"),
        };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore baseline)["WhetstoneItem"];

        Assert.AreEqual(40f, baseline.dps, 0.001f);
        Assert.AreEqual(3f, score.dpsGain, 0.001f);
        // 1s more on both seeds, at the 40 dps of the balance team without item
        Assert.AreEqual(40f, score.effectiveHealthGain, 0.001f);
        Assert.AreEqual(0.1f, score.effectiveHealthGainShare, 0.001f);
    }

    [Test]
    public void Compute_TheBestHolder_TheUnitAddingTheMostDamage()
    {
        List<CombatStats> dps = new List<CombatStats>
        {
            DpsFight(1, 1200f), DpsFight(2, 1200f), DpsFight(3, 1200f),
            DpsFight(1, 1230f, "BounceItem", "Sniper"), DpsFight(2, 1500f, "BounceItem", "Gunner"), DpsFight(3, 1260f, "BounceItem", "Mortar"),
        };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f), RobustnessFight(1, 10f, "BounceItem") };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore _)["BounceItem"];

        Assert.AreEqual("Gunner", score.bestHolder);
        Assert.AreEqual(10f, score.bestHolderDpsGain, 0.001f);
        // The mean of the three holders: (1 + 10 + 2) / 3
        Assert.AreEqual(13f / 3f, score.dpsGain, 0.001f);
    }

    [Test]
    public void Compute_AnItemOfTheCharacter_NoHolder()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight(1, 1200f), DpsFight(1, 1350f, "WarDrumsItem") };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f), RobustnessFight(1, 10f, "WarDrumsItem") };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore _)["WarDrumsItem"];

        Assert.AreEqual("", score.bestHolder);
        Assert.AreEqual(5f, score.dpsGain, 0.001f);
    }

    [Test]
    public void Compute_AnItemChangingNothing_HasNoEffect()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight(1, 1200f), DpsFight(1, 1200f, "ManaCrystalItem") };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f), RobustnessFight(1, 10f, "ManaCrystalItem") };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore _)["ManaCrystalItem"];

        Assert.AreEqual(0f, score.dpsGain, 0.001f);
        Assert.IsFalse(score.hasDpsEffect);
        Assert.IsFalse(score.hasRobustnessEffect);
        Assert.AreEqual(0f, score.powerGain, 0.001f);
        Assert.IsFalse(score.hasPowerEffect);
    }

    [Test]
    public void Compute_AnItemOnlyMakingTheDummiesSurvive_GainsPowerToo()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight(1, 1200f), DpsFight(1, 1200f, "IronPlatingItem", "Sniper") };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f), RobustnessFight(1, 11f, "IronPlatingItem") };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore _)["IronPlatingItem"];

        Assert.IsFalse(score.hasDpsEffect);
        // 40 dps x 11s against 40 dps x 10s
        Assert.AreEqual(40f, score.powerGain, 0.001f);
        Assert.AreEqual(0.1f, score.powerGainShare, 0.001f);
        Assert.IsTrue(score.hasPowerEffect);
    }

    [Test]
    public void Compute_ALossIsAnEffectToo()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight(1, 1200f), DpsFight(1, 1080f, "LoneWolfItem", "Sniper") };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f), RobustnessFight(1, 10f, "LoneWolfItem") };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore _)["LoneWolfItem"];

        Assert.AreEqual(-4f, score.dpsGain, 0.001f);
        Assert.AreEqual(-0.1f, score.dpsGainShare, 0.001f);
        Assert.IsTrue(score.hasDpsEffect);
    }

    [Test]
    public void Compute_TheDummiesAliveAtTheTimeLimit_TheScoreIsALowerBound()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight(1, 1200f), DpsFight(1, 1200f, "PhylacteryItem", "Sniper") };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f), RobustnessFight(1, 180f, "PhylacteryItem", true) };

        ItemScore score = ItemScoreCalculator.Compute(dps, robustness, out ItemScore baseline)["PhylacteryItem"];

        Assert.IsTrue(score.timedOut);
        Assert.IsFalse(baseline.timedOut);
    }

    [Test]
    public void Compute_AnItemMeasuredOneWayOnly_NotScored()
    {
        List<CombatStats> dps = new List<CombatStats> { DpsFight(1, 1200f), DpsFight(1, 1500f, "WhetstoneItem", "Sniper") };
        List<CombatStats> robustness = new List<CombatStats> { RobustnessFight(1, 10f) };

        Dictionary<string, ItemScore> scores = ItemScoreCalculator.Compute(dps, robustness, out ItemScore _);

        Assert.IsEmpty(scores);
    }
}

}
