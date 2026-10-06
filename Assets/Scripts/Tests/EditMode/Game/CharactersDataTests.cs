using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the playable characters of the project data
public class CharactersDataTests
{
    const string GameDataPath = "Assets/Data/TestData.asset";
    const string SandboxDataPath = "Assets/Data/SandboxData.asset";

    GameData _gameData;
    SandboxData _sandboxData;

    [SetUp]
    public void SetUp()
    {
        _gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
        _sandboxData = AssetDatabase.LoadAssetAtPath<SandboxData>(SandboxDataPath);
    }

    [Test]
    public void Game_OffersTheClericTheDruidAndTheWarlock()
    {
        List<string> titles = _gameData.characters.ConvertAll(character => character != null ? character.title : null);

        CollectionAssert.AreEqual(new[] { "Cleric", "Druid", "Warlock" }, titles);
    }

    [Test]
    public void Sandbox_CanPlayEveryGameCharacter()
    {
        CollectionAssert.AreEqual(_gameData.characters, _sandboxData.characters);
        Assert.IsNotNull(_sandboxData.character);
    }

    [Test]
    public void EveryCharacter_HasADescriptionManaHealPowerSkillsAndUnits()
    {
        List<string> invalid = new List<string>();
        foreach (CharacterData character in _gameData.characters)
        {
            bool isValid = !string.IsNullOrEmpty(character.text)
                && character.attributes.ContainsKey(AttributeType.ManaMax)
                && character.attributes.ContainsKey(AttributeType.HealPower)
                && character.skills.Count > 0 && !character.skills.Contains(null)
                && character.entities.Count > 0 && !character.entities.Contains(null);
            if (!isValid)
            {
                invalid.Add(character.name);
            }
        }

        CollectionAssert.IsEmpty(invalid, "Characters with a missing description, attribute, skill or unit");
    }

    // The test entity is only meant for the sandbox
    [Test]
    public void NoCharacter_RecruitsTheTestEntity()
    {
        foreach (CharacterData character in _gameData.characters)
        {
            Assert.IsFalse(character.entities.Exists(entity => entity.name == "TestEntity"), character.name);
        }
    }

    // Each character recruits the units tagged with its class and Reward: its starting units at least
    [TestCase("Assets/Data/GameData.asset")]
    [TestCase("Assets/Data/TestData.asset")]
    public void EveryCharacter_CanRecruitItsStartingUnits(string gameDataPath)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);
        GameObject go = new GameObject("DataManager");
        try
        {
            DataManager dataManager = go.AddComponent<DataManager>();
            dataManager.data = data;
            foreach (CharacterData character in data.characters)
            {
                Assert.IsNotNull(character.classTag, character.title);
                Assert.AreEqual(character.title, character.classTag.name);

                List<EntityData> recruitable = dataManager.GetRewardEntities(character);
                foreach (EntityData unit in character.entities)
                {
                    CollectionAssert.Contains(recruitable, unit, $"{character.title}: {unit.title}");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

}
