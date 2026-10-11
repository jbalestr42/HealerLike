using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Game.Ascension
{

// The ascension level each character unlocked: winning at a level unlocks the next one for that character only,
// and the progress survives a save and a load
public class AscensionProgressTests
{
    const int MaxLevel = 10;

    string _folder;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "AscensionProgressTests_" + System.Guid.NewGuid());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Test]
    public void NewProgress_OnlyLevelZeroIsUnlocked()
    {
        Assert.AreEqual(0, new AscensionProgress().GetUnlockedLevel("ClericCharacter"));
    }

    [Test]
    public void RecordWin_UnlocksTheNextLevel()
    {
        AscensionProgress progress = new AscensionProgress();

        Assert.IsTrue(progress.RecordWin("ClericCharacter", 0, MaxLevel));
        Assert.AreEqual(1, progress.GetUnlockedLevel("ClericCharacter"));

        Assert.IsTrue(progress.RecordWin("ClericCharacter", 1, MaxLevel));
        Assert.AreEqual(2, progress.GetUnlockedLevel("ClericCharacter"));
    }

    [Test]
    public void RecordWin_OnlyUnlocksForTheCharacterThatWon()
    {
        AscensionProgress progress = new AscensionProgress();

        progress.RecordWin("ClericCharacter", 3, MaxLevel);

        Assert.AreEqual(4, progress.GetUnlockedLevel("ClericCharacter"));
        Assert.AreEqual(0, progress.GetUnlockedLevel("DruidCharacter"));
    }

    [Test]
    public void RecordWin_AtALowerLevel_KeepsTheHighestUnlockedLevel()
    {
        AscensionProgress progress = new AscensionProgress();
        progress.RecordWin("ClericCharacter", 5, MaxLevel);

        Assert.IsFalse(progress.RecordWin("ClericCharacter", 2, MaxLevel));
        Assert.AreEqual(6, progress.GetUnlockedLevel("ClericCharacter"));
    }

    [Test]
    public void RecordWin_AtTheHighestLevel_UnlocksNothingMore()
    {
        AscensionProgress progress = new AscensionProgress();
        progress.RecordWin("ClericCharacter", MaxLevel - 1, MaxLevel);

        Assert.IsFalse(progress.RecordWin("ClericCharacter", MaxLevel, MaxLevel));
        Assert.AreEqual(MaxLevel, progress.GetUnlockedLevel("ClericCharacter"));
    }

    [Test]
    public void RecordWin_WithoutCharacter_UnlocksNothing()
    {
        AscensionProgress progress = new AscensionProgress();

        Assert.IsFalse(progress.RecordWin(null, 0, MaxLevel));
        Assert.IsFalse(progress.RecordWin("", 0, MaxLevel));
    }

    [Test]
    public void SaveThenLoad_KeepsTheLevelOfEveryCharacter()
    {
        string path = Path.Combine(_folder, "ascension.json");
        AscensionProgress progress = new AscensionProgress();
        progress.RecordWin("ClericCharacter", 2, MaxLevel);
        progress.RecordWin("WarlockCharacter", 0, MaxLevel);

        progress.Save(path);
        AscensionProgress loaded = AscensionProgress.Load(path);

        Assert.AreEqual(3, loaded.GetUnlockedLevel("ClericCharacter"));
        Assert.AreEqual(1, loaded.GetUnlockedLevel("WarlockCharacter"));
        Assert.AreEqual(0, loaded.GetUnlockedLevel("DruidCharacter"));
    }

    [Test]
    public void Load_WithoutFile_StartsFromLevelZero()
    {
        AscensionProgress progress = AscensionProgress.Load(Path.Combine(_folder, "missing.json"));

        Assert.AreEqual(0, progress.GetUnlockedLevel("ClericCharacter"));
    }

    [Test]
    public void FromJson_BrokenJson_StartsFromLevelZero()
    {
        AscensionProgress progress = null;
        TestHelpers.WithLoggingDisabled(() => progress = AscensionProgress.FromJson("{ not json"));

        Assert.IsNotNull(progress);
        Assert.AreEqual(0, progress.GetUnlockedLevel("ClericCharacter"));
    }

    [Test]
    public void GetKey_IsTheCharacterAssetName()
    {
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
        character.name = "ClericCharacter";

        Assert.AreEqual("ClericCharacter", AscensionProgress.GetKey(character));
        Assert.IsNull(AscensionProgress.GetKey(null));

        Object.DestroyImmediate(character);
    }
}

}
