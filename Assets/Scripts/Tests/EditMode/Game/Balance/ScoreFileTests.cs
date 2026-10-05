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
        measure.characters.Add(new ScoreFile.CharacterEntry { character = "Cleric", score = new CharacterScore { dps = 12f, survivalTime = 8f, healedSurvivalTime = 20f, healingPerSecond = 6f, fingerprint = "def" } });

        ScoreFile.Write(_root, measure);
        ScoreFile.Measure read = ScoreFile.Read(_root);

        Assert.AreEqual(1, read.characters.Count);
        Assert.AreEqual("Cleric", read.characters[0].character);
        Assert.AreEqual(12f, read.characters[0].score.dps);
        Assert.AreEqual(20f, read.characters[0].score.healedSurvivalTime);
        Assert.AreEqual(6f, read.characters[0].score.healingPerSecond);
        Assert.AreEqual("def", read.characters[0].score.fingerprint);
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
