using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// The items made of stat modifiers (new items, stage 1): each one is a droppable reward and
// changes the stats it describes
public class StatItemsDataTests
{
    const string EntityItems = "Assets/Data/EntityItems/";

    static readonly object[] Items =
    {
        new object[] { EntityItems + "IronPlatingItem/IronPlatingItem.asset", "Iron Plating", "Entity" },
        new object[] { EntityItems + "HeartStoneItem/HeartStoneItem.asset", "Heart Stone", "Entity" },
        new object[] { EntityItems + "WhetstoneItem/WhetstoneItem.asset", "Whetstone", "Entity" },
        new object[] { EntityItems + "HairTriggerItem/HairTriggerItem.asset", "Hair Trigger", "Entity" },
        new object[] { EntityItems + "LuckyCoinItem/LuckyCoinItem.asset", "Lucky Coin", "Entity" },
        new object[] { EntityItems + "ExecutionersEdgeItem/ExecutionersEdgeItem.asset", "Executioner's Edge", "Entity" },
        new object[] { EntityItems + "BlessedCharmItem/BlessedCharmItem.asset", "Blessed Charm", "Entity" },
        new object[] { EntityItems + "GlassCannonItem/GlassCannonItem.asset", "Glass Cannon", "Entity" },
        new object[] { EntityItems + "TowerShieldItem/TowerShieldItem.asset", "Tower Shield", "Entity" },
        new object[] { EntityItems + "CursedIdolItem/CursedIdolItem.asset", "Cursed Idol", "Entity" },
        new object[] { EntityItems + "AdrenalineItem/AdrenalineItem.asset", "Adrenaline", "Entity" },
        new object[] { EntityItems + "WarBannerItem/WarBannerItem.asset", "War Banner", "Entity" },
        new object[] { EntityItems + "HuntersMarkItem/HuntersMarkItem.asset", "Hunter's Mark", "Entity" },
        new object[] { "Assets/Data/PlayerItems/ManaCrystalItem/ManaCrystalItem.asset", "Mana Crystal", "Player" },
    };

    GameObject _go;
    AttributeManager _attributes;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Holder");
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

    static ItemFactory LoadEntityItem(string name)
    {
        return Load(EntityItems + name + "Item/" + name + "Item.asset");
    }

    // Adds every buff of the handlers, as equipping the item does
    void AddBuffs(List<ABuffHandlerFactory> handlers)
    {
        foreach (ABuffHandlerFactory handler in handlers)
        {
            foreach (ABuffFactory buffFactory in handler.buffFactoryList)
            {
                buffFactory.GetBuff(null).Add(_go, _go);
            }
        }
    }

    float Get(AttributeType type)
    {
        Attribute attribute = _attributes.GetOrAdd(type);
        attribute.Update();
        return attribute.Value;
    }

    void Set(AttributeType type, float value)
    {
        _attributes.Add(type, new Attribute(value));
    }

    void Equip(string name)
    {
        AddBuffs(LoadEntityItem(name).data.buffs);
    }

    [TestCaseSource(nameof(Items))]
    public void Item_HasANameADescriptionAnIconAndItsRewardTag(string path, string name, string tag)
    {
        ItemFactory item = Load(path);

        Assert.AreEqual(name, item.data.name);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.description), name);
        Assert.IsNotNull(item.data.icon, name);
        Assert.IsTrue(item.data.tags.Exists(itemTag => itemTag != null && itemTag.name == tag), name);
    }

    [TestCaseSource(nameof(Items))]
    public void Item_IsOfferedInTheGameAndTheSandbox(string path, string name, string tag)
    {
        ItemFactory item = Load(path);

        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/GameData.asset").items.Contains(item), name);
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset").items.Contains(item), name);
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<SandboxData>("Assets/Data/SandboxData.asset").items.Contains(item), name);
    }

    [TestCaseSource(nameof(Items))]
    public void Item_EveryBuffLastsAsLongAsTheItemIsEquipped(string path, string name, string tag)
    {
        foreach (ABuffHandlerFactory handler in Load(path).data.buffs)
        {
            Assert.AreEqual(DurationType.Infinite, handler.durationType, name);
        }
    }

    [Test]
    public void IronPlating_Adds3FlatArmor()
    {
        Equip("IronPlating");

        Assert.AreEqual(3f, Get(AttributeType.FlatArmor), 0.0001f);
    }

    [Test]
    public void HeartStone_Adds40MaxHealth()
    {
        Set(AttributeType.HealthMax, 100f);

        Equip("HeartStone");

        Assert.AreEqual(140f, Get(AttributeType.HealthMax), 0.0001f);
    }

    [Test]
    public void Whetstone_Adds5Damage()
    {
        Set(AttributeType.Damage, 10f);

        Equip("Whetstone");

        Assert.AreEqual(15f, Get(AttributeType.Damage), 0.0001f);
    }

    [Test]
    public void HairTrigger_Removes15PercentAttackCooldown()
    {
        Set(AttributeType.AttackRate, 2f);

        Equip("HairTrigger");

        Assert.AreEqual(1.7f, Get(AttributeType.AttackRate), 0.0001f);
    }

    [Test]
    public void LuckyCoin_Adds15CriticalChance_WithTheDefaultCriticalMultiplier()
    {
        Equip("LuckyCoin");

        Assert.AreEqual(15f, Get(AttributeType.CriticalChance), 0.0001f);
        Assert.AreEqual(1.5f, Get(AttributeType.CriticalMultiplier), 0.0001f);
    }

    [Test]
    public void ExecutionersEdge_Adds10CriticalChance_AndCriticalHitsDealDoubleDamage()
    {
        Equip("ExecutionersEdge");

        Assert.AreEqual(10f, Get(AttributeType.CriticalChance), 0.0001f);
        Assert.AreEqual(2f, Get(AttributeType.CriticalMultiplier), 0.0001f);
    }

    [Test]
    public void BlessedCharm_Adds30PercentHealingReceived()
    {
        Equip("BlessedCharm");

        Assert.AreEqual(1.3f, Get(AttributeType.HealingReceived), 0.0001f);
    }

    [Test]
    public void GlassCannon_DoublesDamage_AndHalvesMaxHealth()
    {
        Set(AttributeType.Damage, 10f);
        Set(AttributeType.HealthMax, 100f);

        Equip("GlassCannon");

        Assert.AreEqual(20f, Get(AttributeType.Damage), 0.0001f);
        Assert.AreEqual(50f, Get(AttributeType.HealthMax), 0.0001f);
    }

    [Test]
    public void TowerShield_Adds40PercentArmor_And30PercentAttackCooldown()
    {
        Set(AttributeType.AttackRate, 2f);

        Equip("TowerShield");

        Assert.AreEqual(0.4f, Get(AttributeType.PercentArmor), 0.0001f);
        Assert.AreEqual(2.6f, Get(AttributeType.AttackRate), 0.0001f);
    }

    [Test]
    public void CursedIdol_Adds60PercentDamage()
    {
        Set(AttributeType.Damage, 10f);
        ItemFactory item = LoadEntityItem("CursedIdol");

        AddBuffs(item.data.buffs.FindAll(handler => !handler.GetBuffHandler().isPeriodic));

        Assert.AreEqual(16f, Get(AttributeType.Damage), 0.0001f);
    }

    [Test]
    public void CursedIdol_EverySecond_TheHolderLoses2PercentOfItsMaxHealthThroughArmor()
    {
        Set(AttributeType.HealthMax, 200f);
        ABuffHandlerFactory curse = LoadEntityItem("CursedIdol").data.buffs.Find(handler => handler.GetBuffHandler().isPeriodic);
        Assert.IsNotNull(curse);
        Assert.AreEqual(1f, ((BuffHandlerFactory)curse).data.periodDuration, 0.0001f);

        ApplyConsumerBuffFactory applyConsumer = (ApplyConsumerBuffFactory)curse.buffFactoryList[0];
        AConsumer consumer = applyConsumer.data.consumerFactory.GetConsumer(_go, _go);

        // Damage is negative
        Assert.AreEqual(-4f, consumer.GetValue(), 0.0001f);
        Assert.IsTrue(consumer.ignoreDamageReduction);
    }

    [Test]
    public void Adrenaline_Removes30PercentAttackCooldown_OnlyBelowHalfHealth()
    {
        Set(AttributeType.AttackRate, 2f);
        Set(AttributeType.HealthMax, 100f);
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = _go.AddComponent<Entity>());
        ResourceAttribute health = _go.AddComponent<ResourceAttribute>();
        health.Init(AttributeType.HealthMax);
        TestHelpers.SetPrivateField(entity, "_health", health);

        Equip("Adrenaline");

        Assert.AreEqual(2f, Get(AttributeType.AttackRate), 0.0001f);
        health.SetValue(40f);
        Assert.AreEqual(1.4f, Get(AttributeType.AttackRate), 0.0001f);
    }

    [Test]
    public void WarBanner_TheAlliesOnThe8CellsAroundGetMinus15PercentAttackCooldown()
    {
        ABuffHandlerFactory handler = LoadEntityItem("WarBanner").data.buffs[0];
        BoostEntitiesOnRelativeCellBuffFactory cells = (BoostEntitiesOnRelativeCellBuffFactory)handler.buffFactoryList[0];
        Set(AttributeType.AttackRate, 2f);

        AddBuffs(new List<ABuffHandlerFactory> { cells.data.buffHandlerFactory });

        Assert.AreEqual(RelativeCellPatternType.Adjacent, cells.data.pattern);
        Assert.AreEqual(8, RelativeCellPattern.GetOffsets(cells.data.pattern, cells.data.range).Count);
        Assert.AreEqual(1.7f, Get(AttributeType.AttackRate), 0.0001f);
    }

    [Test]
    public void HuntersMark_HitsMakeTheTargetTake20PercentMoreDamageFor3s_WithoutStacking()
    {
        ItemFactory item = LoadEntityItem("HuntersMark");
        Assert.AreEqual(1, item.data.onHitEffects.Count);
        ABuffHandlerFactory mark = item.data.onHitEffects[0];

        AddBuffs(item.data.onHitEffects);

        Assert.AreEqual(DurationType.Duration, mark.durationType);
        Assert.AreEqual(3f, mark.duration, 0.0001f);
        Assert.AreEqual(1, mark.maxStacks);
        Assert.AreEqual(0.2f, Get(AttributeType.Vulnerability), 0.0001f);
    }

    [Test]
    public void ManaCrystal_Adds20MaxMana()
    {
        Set(AttributeType.ManaMax, 100f);

        AddBuffs(Load("Assets/Data/PlayerItems/ManaCrystalItem/ManaCrystalItem.asset").data.buffs);

        Assert.AreEqual(120f, Get(AttributeType.ManaMax), 0.0001f);
    }
}

}
