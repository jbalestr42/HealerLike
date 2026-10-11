using System.IO;
using NUnit.Framework;

namespace Game.Balance
{

public class ScoreFileTests
{
    string _root;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "ScoreFileTests-" + System.Guid.NewGuid());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Test]
    public void Write_ThenRead_GivesTheSameScores()
    {
        string folder = Path.Combine(_root, "20261005-100000");
        ScoreFile.Measure measure = new ScoreFile.Measure { date = "2026-10-05 10:00", teamDps = 50f };
        measure.waves.Add(new ScoreFile.WaveEntry { wave = "Wave_Crypt", score = new WaveScore { dps = 20f, survivalTime = 10f, threat = 200f, timedOut = true, fingerprint = "abc" } });

        ScoreFile.Write(folder, measure);
        ScoreFile.Measure read = ScoreFile.Read(folder);

        Assert.AreEqual(50f, read.teamDps);
        Assert.AreEqual(1, read.waves.Count);
        Assert.AreEqual("Wave_Crypt", read.waves[0].wave);
        Assert.AreEqual(20f, read.waves[0].score.dps);
        Assert.AreEqual(200f, read.waves[0].score.threat);
        Assert.IsTrue(read.waves[0].score.timedOut);
        Assert.AreEqual("abc", read.waves[0].score.fingerprint);
    }

    [Test]
    public void Write_ThenRead_GivesTheSameCharacterScores()
    {
        ScoreFile.Measure measure = new ScoreFile.Measure { teamDps = 50f };
        measure.characters.Add(new ScoreFile.CharacterEntry { character = "Cleric", score = new CharacterScore { dps = 12f, survivalTime = 8f, spellSurvivalTime = 20f, healingPerSecond = 6f, fingerprint = "def" } });

        ScoreFile.Write(_root, measure);
        ScoreFile.Measure read = ScoreFile.Read(_root);

        Assert.AreEqual(1, read.characters.Count);
        Assert.AreEqual("Cleric", read.characters[0].character);
        Assert.AreEqual(12f, read.characters[0].score.dps);
        Assert.AreEqual(20f, read.characters[0].score.spellSurvivalTime);
        Assert.AreEqual(6f, read.characters[0].score.healingPerSecond);
        Assert.AreEqual("def", read.characters[0].score.fingerprint);
    }

    [Test]
    public void Merge_KeepsThePreviousScoresNotMeasuredAgain()
    {
        ScoreFile.Measure previous = new ScoreFile.Measure { teamDps = 40f };
        previous.items.Add(new ScoreFile.ItemEntry { item = "BounceItem", score = new ItemScore { dpsGain = 1f } });
        previous.items.Add(new ScoreFile.ItemEntry { item = "PoisonItem", score = new ItemScore { dpsGain = 80f } });
        previous.waves.Add(new ScoreFile.WaveEntry { wave = "Wave_Crypt", score = new WaveScore { dps = 20f } });
        previous.characters.Add(new ScoreFile.CharacterEntry { character = "Cleric", score = new CharacterScore { dps = 12f } });
        ScoreFile.Measure measure = new ScoreFile.Measure { teamDps = 41f };
        measure.items.Add(new ScoreFile.ItemEntry { item = "PoisonItem", score = new ItemScore { dpsGain = 10f } });

        ScoreFile.Merge(measure, previous);

        // The item measured again keeps its new score, the others are added
        Assert.AreEqual(41f, measure.teamDps);
        Assert.AreEqual(2, measure.items.Count);
        Assert.AreEqual(10f, measure.items.Find(entry => entry.item == "PoisonItem").score.dpsGain);
        Assert.AreEqual(1f, measure.items.Find(entry => entry.item == "BounceItem").score.dpsGain);
        Assert.AreEqual(1, measure.waves.Count);
        Assert.AreEqual(1, measure.characters.Count);
    }

    static readonly string[] ItemLogs = { "ItemDpsSimulation.jsonl", "ItemRobustnessSimulation.jsonl" };

    // A finished item measure in folder, its fights without item in baselineFolder, with their logs when withLogs
    void WriteItemMeasure(string folder, string fingerprint, string baselineFolder, bool withLogs)
    {
        string path = Path.Combine(_root, folder);
        ScoreFile.Write(path, new ScoreFile.Measure { itemSetupFingerprint = fingerprint, itemBaselineFolder = baselineFolder });
        if (withLogs)
        {
            foreach (string log in ItemLogs)
            {
                File.WriteAllText(Path.Combine(path, log), "");
            }
        }
    }

    [Test]
    public void FindItemBaseline_TheSameSetup_TheFolderOfTheFightsWithoutItem()
    {
        WriteItemMeasure("20261009-100000", "abc", "20261009-100000", true);
        // A single item measured since, the fights without item taken from the first measure
        WriteItemMeasure("20261009-110000", "abc", "20261009-100000", false);

        Assert.AreEqual(Path.Combine(_root, "20261009-100000"), ScoreFile.FindItemBaseline(_root, "abc", ItemLogs));
    }

    [Test]
    public void FindItemBaseline_TheSetupChanged_None()
    {
        WriteItemMeasure("20261009-100000", "abc", "20261009-100000", true);

        Assert.IsNull(ScoreFile.FindItemBaseline(_root, "def", ItemLogs));
    }

    [Test]
    public void FindItemBaseline_LogsGoneOrNoMeasure_None()
    {
        Assert.IsNull(ScoreFile.FindItemBaseline(_root, "abc", ItemLogs));

        WriteItemMeasure("20261009-100000", "abc", "20261009-100000", false);

        Assert.IsNull(ScoreFile.FindItemBaseline(_root, "abc", ItemLogs));
    }

    [Test]
    public void FindItemBaseline_AMeasureFromBeforeTheFingerprint_None()
    {
        WriteItemMeasure("20261009-100000", null, null, true);

        Assert.IsNull(ScoreFile.FindItemBaseline(_root, "abc", ItemLogs));
    }

    [Test]
    public void Merge_WithoutPreviousMeasure_NothingChanges()
    {
        ScoreFile.Measure measure = new ScoreFile.Measure();
        measure.items.Add(new ScoreFile.ItemEntry { item = "PoisonItem", score = new ItemScore() });

        ScoreFile.Merge(measure, null);

        Assert.AreEqual(1, measure.items.Count);
    }

    [Test]
    public void Write_ThenRead_GivesTheSameItemScores()
    {
        ScoreFile.Measure measure = new ScoreFile.Measure { teamDps = 40f, itemBaseline = new ItemScore { dps = 40f, survivalTime = 10f, effectiveHealth = 400f } };
        measure.items.Add(new ScoreFile.ItemEntry { item = "BounceItem", score = new ItemScore { dps = 45f, dpsGain = 5f, bestHolder = "Balance Gunner", effectiveHealthGain = 0f, fingerprint = "ghi" } });

        ScoreFile.Write(_root, measure);
        ScoreFile.Measure read = ScoreFile.Read(_root);

        Assert.AreEqual(400f, read.itemBaseline.effectiveHealth);
        Assert.AreEqual(1, read.items.Count);
        Assert.AreEqual("BounceItem", read.items[0].item);
        Assert.AreEqual(5f, read.items[0].score.dpsGain);
        Assert.AreEqual("Balance Gunner", read.items[0].score.bestHolder);
        Assert.AreEqual("ghi", read.items[0].score.fingerprint);
    }

    [Test]
    public void Read_WithoutScores_IsNull()
    {
        Directory.CreateDirectory(_root);

        Assert.IsNull(ScoreFile.Read(_root));
    }

    [Test]
    public void FindLatest_TheLatestFinishedMeasure_NotAnUnfinishedOne()
    {
        ScoreFile.Write(Path.Combine(_root, "20261004-100000"), new ScoreFile.Measure());
        ScoreFile.Write(Path.Combine(_root, "20261005-100000"), new ScoreFile.Measure());
        // Logs of a measure still running or stopped: no scores.json
        Directory.CreateDirectory(Path.Combine(_root, "20261006-100000"));

        Assert.AreEqual(Path.Combine(_root, "20261005-100000"), ScoreFile.FindLatest(_root));
    }

    [Test]
    public void FindLatest_WithoutMeasure_IsNull()
    {
        Assert.IsNull(ScoreFile.FindLatest(_root));
    }
}

}
