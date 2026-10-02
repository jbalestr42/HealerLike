using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// The cursed unit items of the Cursed Treasure: a strong bonus and its curse on the same item, given to a
// unit, never a regular reward
public class CursedTreasureItemsDataTests
{
    const string EventItems = "Assets/Data/EventItems/";
    const string EntityItems = "Assets/Data/EntityItems/";

    // Folder, title, then the stats the item changes with the value expected from a base of 10 damage,
    // 100 max health, an attack cooldown of 1 and the default multipliers
    static readonly object[] StatItems =
    {
        new object[] { "BloodthirstBladeItem", "Bloodthirst Blade", new[] { AttributeType.Damage, AttributeType.HealingReceived }, new[] { 16f, 0.6f } },
        new object[] { "ThornedCrownItem", "Thorned Crown", new[] { AttributeType.FlatArmor, AttributeType.HealingReceived }, new[] { 6f, 0.5f } },
        new object[] { "ReapersCoinItem", "Reaper's Coin", new[] { AttributeType.CriticalChance, AttributeType.CriticalMultiplier, AttributeType.HealthMax }, new[] { 30f, 2f, 50f } },
        new object[] { "BerserkersBrandItem", "Berserker's Brand", new[] { AttributeType.AttackRate, AttributeType.Vulnerability }, new[] { 0.65f, 0.25f } },
        new object[] { "GluttonsCharmItem", "Glutton's Charm", new[] { AttributeType.HealingReceived, AttributeType.Damage }, new[] { 1.5f, 7f } },
        new object[] { "LeechFangItem", "Leech Fang", new[] { AttributeType.Damage }, new[] { 7f } },
    };

    // The Cursed Treasure ones, then the unit items that were regular rewards before becoming cursed
    static readonly string[] AllItems =
    {
        EventItems + "BloodthirstBladeItem", EventItems + "ThornedCrownItem", EventItems + "ReapersCoinItem", EventItems + "BerserkersBrandItem",
        EventItems + "GluttonsCharmItem", EventItems + "LeechFangItem", EventItems + "HungeringMaskItem",
        EntityItems + "GlassCannonItem", EntityItems + "CursedIdolItem", EntityItems + "BloodPriceItem",
    };

    GameObject _go;
    AttributeManager _attributes;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Unit");
        _attributes = TestHelpers.CreateAttributeManager(_go);
        _attributes.Add(AttributeType.Damage, new Attribute(10f));
        _attributes.Add(AttributeType.HealthMax, new Attribute(100f));
        _attributes.Add(AttributeType.AttackRate, new Attribute(1f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    static AItemFactory Load(string folder)
    {
        return LoadAt(EventItems + folder);
    }

    // The folder path, the asset being named after the folder
    static AItemFactory LoadAt(string folderPath)
    {
        string name = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
        string path = folderPath + "/" + name + ".asset";
        AItemFactory item = AssetDatabase.LoadAssetAtPath<AItemFactory>(path);
        Assert.IsNotNull(item, path);
        return item;
    }

    // Adds the stat modifiers of the item, as equipping it does
    void EquipModifiers(ItemFactory item)
    {
        foreach (ABuffHandlerFactory handler in item.data.buffs)
        {
            foreach (ABuffFactory buffFactory in handler.buffFactoryList)
            {
                if (buffFactory is FlatModifierFactory)
                {
                    buffFactory.GetBuff(null).Add(_go, _go);
                }
            }
        }
    }

    float Get(AttributeType type)
    {
        Attribute attribute = _attributes.GetOrAdd(type);
        attribute.Update();
        return attribute.Value;
    }

    [TestCaseSource(nameof(StatItems))]
    public void StatItem_GivesItsBonusAndItsCurse(string folder, string title, AttributeType[] types, float[] expected)
    {
        ItemFactory item = Load(folder) as ItemFactory;
        Assert.IsNotNull(item, folder);
        Assert.AreEqual(title, item.data.name);

        EquipModifiers(item);

        for (int i = 0; i < types.Length; i++)
        {
            Assert.AreEqual(expected[i], Get(types[i]), 0.0001f, $"{title}: {types[i]}");
        }
    }

    [TestCaseSource(nameof(AllItems))]
    public void Item_IsACursedUnitItemWithItsBonusAndCurse(string folder)
    {
        AItemFactory item = LoadAt(folder);

        Assert.IsTrue(item.HasTag(TagNames.Entity), folder);
        Assert.IsTrue(item.HasTag(TagNames.Cursed), folder);
        Assert.IsFalse(item.HasTag(TagNames.Player), folder);
        Assert.IsFalse(item.HasTag(TagNames.Library), folder);
        AItem instance = item.GetItem();
        Assert.IsNotNull(instance.icon, folder);
        Assert.IsNotEmpty(instance.description, folder);
    }

    [Test]
    public void LeechFang_HealsItsHolderFor30PercentOfItsDamage()
    {
        ItemFactory item = (ItemFactory)Load("LeechFangItem");

        LifeStealBuffFactory lifeSteal = item.data.buffs[0].buffFactoryList.Find(buff => buff is LifeStealBuffFactory) as LifeStealBuffFactory;

        Assert.IsNotNull(lifeSteal);
        Assert.AreEqual(0.3f, lifeSteal.data.ratio, 0.0001f);
        Assert.AreEqual(LifeStealTarget.Holder, lifeSteal.data.target);
    }

    [Test]
    public void HungeringMask_OneDamagePerKillAndFivePercentHealthPerBattle_TenTimesEach()
    {
        GrowingItemFactory item = Load("HungeringMaskItem") as GrowingItemFactory;
        Assert.IsNotNull(item);

        Assert.AreEqual(10, item.data.maxKillGrowth);
        Assert.AreEqual(10, item.data.maxGrowth);
        UpgradeModifierFactory damage = item.data.killGrowthBuffHandlerFactory.buffFactoryList[0] as UpgradeModifierFactory;
        UpgradeModifierFactory health = item.data.growthBuffHandlerFactory.buffFactoryList[0] as UpgradeModifierFactory;
        Assert.AreEqual(AttributeType.Damage, damage.data.type);
        Assert.AreEqual(AttributeModifierType.Add, damage.data.modifierType);
        Assert.AreEqual(1f, damage.data.value, 0.0001f);
        Assert.AreEqual(AttributeType.HealthMax, health.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, health.data.modifierType);
        Assert.AreEqual(-0.05f, health.data.value, 0.0001f);

        // Kept between the battles, stacking every time
        foreach (ABuffHandlerFactory handler in new[] { item.data.killGrowthBuffHandlerFactory, item.data.growthBuffHandlerFactory })
        {
            Assert.AreEqual(DurationType.Infinite, handler.durationType);
            Assert.AreEqual(0, handler.maxStacks);
            Assert.IsTrue(handler.HasTag(TagNames.Permanent));
        }
    }

    [TestCase("Assets/Data/GameData.asset")]
    [TestCase("Assets/Data/TestData.asset")]
    public void Items_AreTheChestOfTheCursedTreasureButNeverRegularRewards(string gameDataPath)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);
        GameObject go = new GameObject("DataManager");
        try
        {
            DataManager dataManager = go.AddComponent<DataManager>();
            dataManager.data = data;

            List<AItemFactory> chest = dataManager.GetItems(CursedTreasureEventRoom.ItemTags);
            List<AItemFactory> expected = new List<AItemFactory>();
            foreach (string folder in AllItems)
            {
                expected.Add(LoadAt(folder));
            }
            CollectionAssert.AreEquivalent(expected, chest);
            CollectionAssert.IsEmpty(dataManager.GetItems(new List<string> { TagNames.Entity }, UpgradeView.RewardExcludedTags).FindAll(expected.Contains));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

}
