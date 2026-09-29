using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// The items hooked on the combat (new items, stage 2): each one is a droppable reward wired to the
// behaviour it describes
public class CombatItemsDataTests
{
    const string EntityItems = "Assets/Data/EntityItems/";
    const string PlayerItems = "Assets/Data/PlayerItems/";

    static readonly object[] Items =
    {
        new object[] { EntityItems + "ArmorBreakerItem/ArmorBreakerItem.asset", "Armor Breaker", "Entity" },
        new object[] { EntityItems + "BloodPriceItem/BloodPriceItem.asset", "Blood Price", "Entity" },
        new object[] { EntityItems + "ExecutionerItem/ExecutionerItem.asset", "Executioner", "Entity" },
        new object[] { EntityItems + "EchoItem/EchoItem.asset", "Echo", "Entity" },
        new object[] { PlayerItems + "TitheItem/TitheItem.asset", "Tithe", "Player" },
        new object[] { PlayerItems + "PrayerBeadsItem/PrayerBeadsItem.asset", "Prayer Beads", "Player" },
        new object[] { PlayerItems + "WarDrumsItem/WarDrumsItem.asset", "War Drums", "Player" },
        new object[] { PlayerItems + "NecronomiconItem/NecronomiconItem.asset", "Necronomicon", "Player" },
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

    static ItemFactory LoadItem(string folder, string name)
    {
        return Load(folder + name + "Item/" + name + "Item.asset");
    }

    // The only buff of the item's buff handlers of type T
    static T GetBuff<T>(ItemFactory item) where T : ABuffFactory
    {
        List<T> buffs = new List<T>();
        foreach (ABuffHandlerFactory handler in item.data.buffs)
        {
            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                if (buff is T typed)
                {
                    buffs.Add(typed);
                }
            }
        }
        Assert.AreEqual(1, buffs.Count, typeof(T).Name);
        return buffs[0];
    }

    // Adds every buff of the handler, as applying it does
    void AddBuffs(ABuffHandlerFactory handler)
    {
        foreach (ABuffFactory buffFactory in handler.buffFactoryList)
        {
            buffFactory.GetBuff(null).Add(_go, _go);
        }
    }

    float Get(AttributeType type)
    {
        Attribute attribute = _attributes.GetOrAdd(type);
        attribute.Update();
        return attribute.Value;
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
    public void ArmorBreaker_EachHitRemoves1ArmorUntilTheEndOfTheFight_NeverBelow0()
    {
        ItemFactory item = LoadItem(EntityItems, "ArmorBreaker");
        Assert.AreEqual(1, item.data.onHitEffects.Count);
        ABuffHandlerFactory breaker = item.data.onHitEffects[0];
        _attributes.Add(AttributeType.FlatArmor, new Attribute(2f));

        ABuff buff = breaker.buffFactoryList[0].GetBuff(null);
        buff.Add(_go, _go);
        Assert.AreEqual(1f, Get(AttributeType.FlatArmor), 0.0001f);
        // 2 more hits than the armor of the target
        ((IStackableBuff)buff).Stack(_go, _go);
        ((IStackableBuff)buff).Stack(_go, _go);

        Assert.AreEqual(DurationType.Infinite, breaker.durationType);
        // Removed with the other temporary buffs at the end of the fight
        Assert.IsEmpty(breaker.tags);
        Assert.AreEqual(0, breaker.maxStacks);
        Assert.AreEqual(0f, Get(AttributeType.FlatArmor), 0.0001f);
    }

    [Test]
    public void BloodPrice_DoublesDamage_AndEachAttackCosts3HealthThroughArmor()
    {
        ItemFactory item = LoadItem(EntityItems, "BloodPrice");
        _attributes.Add(AttributeType.Damage, new Attribute(10f));
        AddBuffs(item.data.buffs.Find(handler => handler.buffFactoryList[0] is FlatModifierFactory));

        AConsumer cost = GetBuff<ConsumerOnAttackBuffFactory>(item).data.consumerFactory.GetConsumer(_go, _go);

        Assert.AreEqual(20f, Get(AttributeType.Damage), 0.0001f);
        Assert.AreEqual(-3f, cost.GetValue(), 0.0001f);
        Assert.IsTrue(cost.ignoreDamageReduction);
    }

    [Test]
    public void Executioner_HitsKillTheTargetsBelow10PercentHealth()
    {
        ItemFactory item = LoadItem(EntityItems, "Executioner");

        Assert.AreEqual(1, item.data.onHitConsumers.Count);
        ExecuteConsumerFactory execute = (ExecuteConsumerFactory)item.data.onHitConsumers[0];
        Assert.AreEqual(0.1f, execute.data.healthThreshold, 0.0001f);
        Assert.IsTrue(execute.data.ignoreDamageReduction);
        Assert.IsFalse(execute.data.ignoreConsumerPrevention);
    }

    [Test]
    public void Echo_Every4thAttackIsMadeTwice_AShortWhileApart()
    {
        EchoAttackBuffFactory echo = GetBuff<EchoAttackBuffFactory>(LoadItem(EntityItems, "Echo"));

        Assert.AreEqual(4, echo.data.attackCount);
        Assert.AreEqual(0.2f, echo.data.delay, 0.0001f);
    }

    [Test]
    public void Tithe_Gives3ManaForEachEnemyKilled()
    {
        ManaOnKillBuffFactory tithe = GetBuff<ManaOnKillBuffFactory>(LoadItem(PlayerItems, "Tithe"));

        // The consumer value is added to the mana
        Assert.AreEqual(3f, tithe.data.consumerFactory.GetConsumer(_go, _go).GetValue(), 0.0001f);
        Assert.AreEqual(Entity.EntityType.Computer, tithe.data.killedType);
    }

    [Test]
    public void PrayerBeads_Removes15PercentSkillCooldown()
    {
        AddBuffs(LoadItem(PlayerItems, "PrayerBeads").data.buffs[0]);

        Assert.AreEqual(0.85f, Get(AttributeType.SkillCooldownMultiplier), 0.0001f);
    }

    [Test]
    public void WarDrums_AtBattleStart_TheUnitsGetMinus30PercentAttackCooldownFor5s()
    {
        ApplyBuffOnEventBuffFactory drums = GetBuff<ApplyBuffOnEventBuffFactory>(LoadItem(PlayerItems, "WarDrums"));
        ABuffHandlerFactory handler = drums.data.buffHandlerFactory;
        _attributes.Add(AttributeType.AttackRate, new Attribute(2f));

        AddBuffs(handler);

        Assert.AreEqual(BuffEventTrigger.BattleStart, drums.data.triggers);
        Assert.AreEqual(Entity.EntityType.Player, drums.data.entityType);
        Assert.AreEqual(DurationType.Duration, handler.durationType);
        Assert.AreEqual(5f, handler.duration, 0.0001f);
        Assert.AreEqual(1.4f, Get(AttributeType.AttackRate), 0.0001f);
    }

    [Test]
    public void Necronomicon_TheSummonsGetPlus50PercentMaxHealthAndDamage()
    {
        ApplyBuffOnEventBuffFactory necronomicon = GetBuff<ApplyBuffOnEventBuffFactory>(LoadItem(PlayerItems, "Necronomicon"));
        _attributes.Add(AttributeType.HealthMax, new Attribute(100f));
        _attributes.Add(AttributeType.Damage, new Attribute(10f));

        AddBuffs(necronomicon.data.buffHandlerFactory);

        Assert.AreEqual(BuffEventTrigger.Summoned, necronomicon.data.triggers);
        Assert.AreEqual(Entity.EntityType.Player, necronomicon.data.entityType);
        Assert.AreEqual(DurationType.Infinite, necronomicon.data.buffHandlerFactory.durationType);
        Assert.AreEqual(150f, Get(AttributeType.HealthMax), 0.0001f);
        Assert.AreEqual(15f, Get(AttributeType.Damage), 0.0001f);
    }
}

}
