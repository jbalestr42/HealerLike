using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// Round End Heal: the player item healing every unit for a part of its max health after each fight, offered
// in the runs since the health of the units is kept from one fight to the next (Round End Mana is not, the
// mana being refilled before each fight)
public class RoundEndHealItemDataTests
{
    const string RoundEndHealPath = "Assets/Data/PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem.asset";
    const string RoundEndManaPath = "Assets/Data/PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem.asset";

    GameObject _go;
    AttributeManager _attributes;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Unit");
        _attributes = TestHelpers.CreateAttributeManager(_go);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    static ItemFactory Load(string path)
    {
        ItemFactory item = AssetDatabase.LoadAssetAtPath<ItemFactory>(path);
        Assert.IsNotNull(item, path);
        return item;
    }

    static GameData LoadGameData(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameData>(path);
    }

    static HealAllEntitiesOnRoundEndBuffFactory GetHealBuff(ItemFactory item)
    {
        Assert.AreEqual(1, item.data.buffs.Count);
        Assert.AreEqual(1, item.data.buffs[0].buffFactoryList.Count);
        HealAllEntitiesOnRoundEndBuffFactory buff = item.data.buffs[0].buffFactoryList[0] as HealAllEntitiesOnRoundEndBuffFactory;
        Assert.IsNotNull(buff);
        return buff;
    }

    [Test]
    public void RoundEndHeal_HasANameADescriptionAnIconAndThePlayerTag()
    {
        ItemFactory item = Load(RoundEndHealPath);

        Assert.AreEqual("Round End Heal", item.data.name);
        StringAssert.Contains("15%", item.data.description);
        Assert.IsNotNull(item.data.icon);
        Assert.IsTrue(item.data.tags.Exists(tag => tag != null && tag.name == TagNames.Player));
    }

    [Test]
    public void RoundEndHeal_IsOfferedInTheRunsTheTestsAndTheSandbox()
    {
        ItemFactory item = Load(RoundEndHealPath);

        Assert.IsTrue(LoadGameData("Assets/Data/GameData.asset").items.Contains(item));
        Assert.IsTrue(LoadGameData("Assets/Data/TestData.asset").items.Contains(item));
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<SandboxData>("Assets/Data/SandboxData.asset").items.Contains(item));
    }

    [Test]
    public void RoundEndHeal_LastsAsLongAsTheItemIsEquipped()
    {
        ItemFactory item = Load(RoundEndHealPath);

        Assert.AreEqual(DurationType.Infinite, item.data.buffs[0].durationType);
    }

    [Test]
    public void RoundEndHeal_HealsEachUnitFor15PercentOfItsMaxHealth()
    {
        HealAllEntitiesOnRoundEndBuffFactory buff = GetHealBuff(Load(RoundEndHealPath));
        _attributes.Add(AttributeType.HealthMax, new Attribute(200f));

        AConsumer heal = buff.data.consumerFactory.GetConsumer(_go, _go);

        // The consumer value is added to the health of the unit
        Assert.AreEqual(30f, heal.GetValue(), 0.0001f);
    }

    [Test]
    public void RoundEndHeal_ScalesWithTheMaxHealthOfTheHealedUnit()
    {
        HealAllEntitiesOnRoundEndBuffFactory buff = GetHealBuff(Load(RoundEndHealPath));
        _attributes.Add(AttributeType.HealthMax, new Attribute(500f));

        AConsumer heal = buff.data.consumerFactory.GetConsumer(_go, _go);

        Assert.AreEqual(75f, heal.GetValue(), 0.0001f);
    }

    [Test]
    public void RoundEndMana_IsNotOfferedInTheRuns_ButStaysInTheSandbox()
    {
        ItemFactory item = Load(RoundEndManaPath);

        Assert.IsFalse(LoadGameData("Assets/Data/GameData.asset").items.Contains(item));
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<SandboxData>("Assets/Data/SandboxData.asset").items.Contains(item));
    }
}

}
