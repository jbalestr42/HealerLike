using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Ascension
{

// The ascension levels: level N plays with the maluses of the levels 1 to N, level 0 without any
public class AscensionSettingsTests
{
    const string GameDataPath = "Assets/Data/TestData.asset";

    AscensionSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _settings = ScriptableObject.CreateInstance<AscensionSettings>();
        for (int i = 1; i <= 3; i++)
        {
            _settings.levels.Add(new AscensionLevel { title = "Level " + i, description = "Malus " + i });
        }
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_settings);
    }

    [Test]
    public void MaxLevel_IsTheLevelCount()
    {
        Assert.AreEqual(3, _settings.maxLevel);
    }

    [TestCase(-1, 0)]
    [TestCase(0, 0)]
    [TestCase(2, 2)]
    [TestCase(3, 3)]
    [TestCase(4, 3)]
    public void ClampLevel_StaysBetweenNoMalusAndTheHighestLevel(int level, int expected)
    {
        Assert.AreEqual(expected, _settings.ClampLevel(level));
    }

    [Test]
    public void GetActiveLevels_LevelZero_HasNoMalus()
    {
        Assert.IsEmpty(_settings.GetActiveLevels(0));
    }

    [Test]
    public void GetActiveLevels_AddsTheMalusOfEveryLevelUpToTheChosenOne()
    {
        List<AscensionLevel> active = _settings.GetActiveLevels(2);

        CollectionAssert.AreEqual(new[] { _settings.levels[0], _settings.levels[1] }, active);
    }

    [Test]
    public void GetActiveLevels_AboveTheHighestLevel_HasEveryMalus()
    {
        CollectionAssert.AreEqual(_settings.levels, _settings.GetActiveLevels(10));
    }

    [Test]
    public void GetMalusText_LevelZero_SaysThereIsNoMalus()
    {
        Assert.AreEqual("No malus.", _settings.GetMalusText(0));
    }

    [Test]
    public void GetMalusText_ListsEveryActiveMalusWithItsLevel()
    {
        Assert.AreEqual("1. <b>Level 1</b>: Malus 1\n2. <b>Level 2</b>: Malus 2", _settings.GetMalusText(2));
    }

    [Test]
    public void GetTooltip_StartsWithTheLevelThenItsMaluses()
    {
        Assert.AreEqual("<b>Ascension 1</b>\n1. <b>Level 1</b>: Malus 1", _settings.GetTooltip(1));
        StringAssert.StartsWith("<b>Ascension 3</b>", _settings.GetTooltip(8));
    }

    [TestCase(1, 0, false)]
    [TestCase(1, 1, true)]
    [TestCase(3, 2, false)]
    [TestCase(3, 5, true)]
    [TestCase(0, 5, false)]
    public void IsActive_FromItsOwnLevel(int malusLevel, int runLevel, bool expected)
    {
        Assert.AreEqual(expected, AscensionSettings.IsActive(malusLevel, runLevel));
    }

    [Test]
    public void GetAscensionLevel_KeepsTheChosenLevelWithinTheGameLevels()
    {
        Assert.AreEqual(2, AscensionGameType.GetAscensionLevel(_settings, 2));
        Assert.AreEqual(3, AscensionGameType.GetAscensionLevel(_settings, 7));
    }

    [Test]
    public void GetAscensionLevel_WithoutLevels_PlaysWithoutMalus()
    {
        Assert.AreEqual(0, AscensionGameType.GetAscensionLevel(null, 4));
    }

    [Test]
    public void RunState_KeepsItsAscensionLevel()
    {
        RunMap map = new RunMap(new List<List<MapNode>>(), new MapNode(0, 0, MapNodeType.Boss), 0);

        Assert.AreEqual(0, new RunState(map).ascensionLevel);
        Assert.AreEqual(4, new RunState(map, 4).ascensionLevel);
    }

    [Test]
    public void BlocksPlayerCenter_FromTheLevelThatBlocksIt()
    {
        _settings.levels[1].blocksPlayerCenter = true;

        Assert.IsFalse(_settings.BlocksPlayerCenter(0));
        Assert.IsFalse(_settings.BlocksPlayerCenter(1));
        Assert.IsTrue(_settings.BlocksPlayerCenter(2));
        Assert.IsTrue(_settings.BlocksPlayerCenter(3));
    }

    // Cursed like the other bad items, yet never drawn by an event: they are in no item list of the game
    [Test]
    public void Game_EveryMalusItemIsCursed_AndOnlyGivenByItsLevel()
    {
        GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);

        foreach (AscensionLevel level in gameData.ascension.levels)
        {
            List<AItemFactory> items = new List<AItemFactory>(level.characterItems);
            items.AddRange(level.enemyItems);
            foreach (AItemFactory item in items)
            {
                Assert.IsTrue(item.HasTag(TagNames.Cursed), $"{level.title}: {item.name}");
                CollectionAssert.DoesNotContain(gameData.items, item, $"{level.title}: {item.name}");
            }
        }
    }

    [Test]
    public void Game_Level4BlocksTheCenterOfThePlayerBoard_NoOtherLevel()
    {
        GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);

        for (int i = 0; i < gameData.ascension.levels.Count; i++)
        {
            Assert.AreEqual(i == 3, gameData.ascension.levels[i].blocksPlayerCenter, gameData.ascension.levels[i].title);
        }
        Assert.IsFalse(gameData.ascension.BlocksPlayerCenter(3));
        Assert.IsTrue(gameData.ascension.BlocksPlayerCenter(4));
    }

    [Test]
    public void Game_HasTenLevelsWithATitleAndADescription()
    {
        GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);

        Assert.IsNotNull(gameData.ascension);
        Assert.AreEqual(10, gameData.ascension.maxLevel);
        foreach (AscensionLevel level in gameData.ascension.levels)
        {
            Assert.IsFalse(string.IsNullOrEmpty(level.title), "A level has no title");
            Assert.IsFalse(string.IsNullOrEmpty(level.description), $"{level.title} has no description");
        }
    }
}

}
