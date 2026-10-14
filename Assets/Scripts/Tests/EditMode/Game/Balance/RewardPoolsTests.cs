using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Game.Balance
{

public class RewardPoolsTests
{
    const string GameDataPath = "Assets/Data/TestData.asset";

    static GameData LoadGameData()
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
        Assert.IsNotNull(data, GameDataPath);
        return data;
    }

    static IEnumerable<string> CharacterPaths()
    {
        yield return "Assets/Data/Characters/ClericCharacter/ClericCharacter.asset";
        yield return "Assets/Data/Characters/DruidCharacter/DruidCharacter.asset";
        yield return "Assets/Data/Characters/WarlockCharacter/WarlockCharacter.asset";
    }

    [TestCaseSource(nameof(CharacterPaths))]
    public void Create_UnitsOfTheClassOfTheCharacterTaggedReward(string characterPath)
    {
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(characterPath);
        Assert.IsNotNull(character, characterPath);

        RewardPools pools = RewardPools.Create(LoadGameData(), character);

        Assert.IsNotEmpty(pools.units);
        Assert.IsTrue(pools.units.TrueForAll(unit => unit.HasTag(character.classTag) && unit.HasTag(TagNames.Reward)));
    }

    [Test]
    public void Create_ItemsLikeTheRewardScreen_NeitherCursedNorLibrary()
    {
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/DruidCharacter/DruidCharacter.asset");

        RewardPools pools = RewardPools.Create(LoadGameData(), character);

        Assert.IsNotEmpty(pools.unitItems);
        Assert.IsNotEmpty(pools.playerItems);
        Assert.IsTrue(pools.unitItems.TrueForAll(item => item.HasTag(TagNames.Entity)));
        Assert.IsTrue(pools.playerItems.TrueForAll(item => item.HasTag(TagNames.Player)));
        List<AItemFactory> all = new List<AItemFactory>(pools.unitItems);
        all.AddRange(pools.playerItems);
        Assert.IsFalse(all.Exists(item => item.HasTag(TagNames.Cursed) || item.HasTag(TagNames.Library)));
    }

    // The items of its class or of no class, never the ones of another class
    [TestCaseSource(nameof(CharacterPaths))]
    public void Create_ItemsOfTheClassOfTheCharacterOrOfNoClass(string characterPath)
    {
        GameData data = LoadGameData();
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(characterPath);

        RewardPools pools = RewardPools.Create(data, character);

        List<AItemFactory> all = new List<AItemFactory>(pools.unitItems);
        all.AddRange(pools.playerItems);
        foreach (CharacterData other in data.characters)
        {
            if (other != character && other.classTag != character.classTag)
            {
                Assert.IsFalse(all.Exists(item => item.HasTag(other.classTag) && !item.HasTag(character.classTag)), other.name);
            }
        }
    }

    // Holy Water only works with the Blessed status of the Cleric
    [Test]
    public void Create_HolyWater_OnlyForTheCleric()
    {
        GameData data = LoadGameData();
        AItemFactory holyWater = AssetDatabase.LoadAssetAtPath<AItemFactory>("Assets/Data/EntityItems/HolyWaterItem/HolyWaterItem.asset");
        Assert.IsNotNull(holyWater);

        foreach (string path in CharacterPaths())
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            RewardPools pools = RewardPools.Create(data, character);
            Assert.AreEqual(character.classTag.name == "Cleric", pools.unitItems.Contains(holyWater), path);
        }
    }

    [Test]
    public void Create_ChancesOfTheGameData()
    {
        GameData data = LoadGameData();

        RewardPools pools = RewardPools.Create(data, null);

        Assert.AreEqual(data.unitRewardChance, pools.unitChance);
        Assert.AreEqual(data.playerItemChance, pools.playerItemChance);
        Assert.IsEmpty(pools.units);
    }
}

}
