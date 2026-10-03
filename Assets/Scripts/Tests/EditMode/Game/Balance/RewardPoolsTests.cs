using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
