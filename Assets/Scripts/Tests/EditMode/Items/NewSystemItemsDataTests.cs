using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Items
{

// The items built on new systems (new items, stage 6): each one is a droppable reward wired to the
// behaviour it describes
public class NewSystemItemsDataTests
{
    const string EntityItems = "Assets/Data/EntityItems/";
    const string PlayerItems = "Assets/Data/PlayerItems/";

    static readonly object[] Items =
    {
        new object[] { EntityItems + "GrowingSeedItem/GrowingSeedItem.asset", "Growing Seed", "Entity" },
        new object[] { PlayerItems + "MerchantsLedgerItem/MerchantsLedgerItem.asset", "Merchant's Ledger", "Player" },
    };

    static AItemFactory Load(string path)
    {
        AItemFactory item = AssetDatabase.LoadAssetAtPath<AItemFactory>(path);
        Assert.IsNotNull(item, path);
        return item;
    }

    [TestCaseSource(nameof(Items))]
    public void Item_HasANameADescriptionAnIconAndItsRewardTag(string path, string name, string tag)
    {
        AItem item = Load(path).GetItem();

        Assert.AreEqual(name, item.title);
        Assert.IsFalse(string.IsNullOrEmpty(item.description), name);
        Assert.IsNotNull(item.icon, name);
        Assert.IsTrue(item.tags.Exists(itemTag => itemTag != null && itemTag.name == tag), name);
    }

    [TestCaseSource(nameof(Items))]
    public void Item_IsOfferedInTheGameAndTheSandbox(string path, string name, string tag)
    {
        AItemFactory item = Load(path);

        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/GameData.asset").items.Contains(item), name);
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset").items.Contains(item), name);
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<SandboxData>("Assets/Data/SandboxData.asset").items.Contains(item), name);
    }

    [Test]
    public void GrowingSeed_GrowthStacksAndOutlivesTheBattles()
    {
        GrowingItemFactory item = Load(EntityItems + "GrowingSeedItem/GrowingSeedItem.asset") as GrowingItemFactory;
        Assert.IsNotNull(item);
        ABuffHandlerFactory growth = item.data.growthBuffHandlerFactory;
        GameplayTag permanent = AssetDatabase.LoadAssetAtPath<GameplayTag>("Assets/Prefabs/Tags/Permanent.asset");

        Assert.AreEqual(DurationType.Infinite, growth.durationType);
        Assert.AreEqual(0, growth.maxStacks);
        Assert.IsTrue(growth.tags.Exists(tag => tag != null && tag.IsDescendantOf(permanent)));
    }

    [Test]
    public void GrowingSeed_EachGrowthGives1DamageAnd5MaxHealth()
    {
        GrowingItemFactory item = Load(EntityItems + "GrowingSeedItem/GrowingSeedItem.asset") as GrowingItemFactory;
        Assert.IsNotNull(item);
        List<FlatModifierFactory> modifiers = item.data.growthBuffHandlerFactory.buffFactoryList.FindAll(buff => buff is FlatModifierFactory).ConvertAll(buff => (FlatModifierFactory)buff);

        FlatModifierFactory damage = modifiers.Find(modifier => modifier.data.type == AttributeType.Damage);
        FlatModifierFactory health = modifiers.Find(modifier => modifier.data.type == AttributeType.HealthMax);
        Assert.AreEqual(2, modifiers.Count);
        Assert.IsNotNull(damage);
        Assert.IsNotNull(health);
        Assert.AreEqual(AttributeModifierType.Add, damage.data.modifierType);
        Assert.AreEqual(1f, damage.data.value, 0.0001f);
        Assert.AreEqual(AttributeModifierType.Add, health.data.modifierType);
        Assert.AreEqual(5f, health.data.value, 0.0001f);
    }

    [Test]
    public void MerchantsLedger_Gives1MoreRewardChoice()
    {
        ItemFactory item = Load(PlayerItems + "MerchantsLedgerItem/MerchantsLedgerItem.asset") as ItemFactory;
        Assert.IsNotNull(item);
        List<FlatModifierFactory> modifiers = new List<FlatModifierFactory>();
        foreach (ABuffHandlerFactory handler in item.data.buffs)
        {
            Assert.AreEqual(DurationType.Infinite, handler.durationType);
            modifiers.AddRange(handler.buffFactoryList.FindAll(buff => buff is FlatModifierFactory).ConvertAll(buff => (FlatModifierFactory)buff));
        }

        Assert.AreEqual(1, modifiers.Count);
        Assert.AreEqual(AttributeType.RewardChoices, modifiers[0].data.type);
        Assert.AreEqual(AttributeModifierType.Add, modifiers[0].data.modifierType);
        Assert.AreEqual(1f, modifiers[0].data.value, 0.0001f);
    }
}

}
