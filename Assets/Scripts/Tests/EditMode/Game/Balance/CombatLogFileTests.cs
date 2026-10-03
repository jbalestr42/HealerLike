using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class CombatLogFileTests
{
    string _directory;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "HealerLikeCombatLogTests", System.Guid.NewGuid().ToString());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [Test]
    public void Append_MissingFolder_CreatesItAndWritesOneLinePerFight()
    {
        string path = Path.Combine(_directory, "Balance", CombatLogFile.FileName);

        CombatLogFile.Append(path, new CombatStats { wave = "Wave_A", floor = 1 });
        CombatLogFile.Append(path, new CombatStats { wave = "Wave_B", floor = 2 });

        string[] lines = File.ReadAllLines(path);
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual("Wave_A", JsonUtility.FromJson<CombatStats>(lines[0]).wave);
        Assert.AreEqual(2, JsonUtility.FromJson<CombatStats>(lines[1]).floor);
    }

    [Test]
    public void DefaultPath_InTheBalanceFolderOfTheProjectLogs()
    {
        string expected = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "Balance", CombatLogFile.FileName);

        Assert.AreEqual(expected, CombatLogFile.defaultPath);
    }
}

}
