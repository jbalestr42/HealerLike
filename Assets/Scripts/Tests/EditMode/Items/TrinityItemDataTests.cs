using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// Trinity: a little of the three roles (damage, attack speed, health), less of each than the item of that role
// (Whetstone +5 damage, Hair Trigger -15% attack cooldown, Heart Stone +40 max health)
public class TrinityItemDataTests
{
    const string Path = "Assets/Data/EntityItems/TrinityItem/TrinityItem.asset";

    GameObject _go;
    AttributeManager _attributes;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Holder");
        _attributes = TestHelpers.CreateAttributeManager(_go);
        _attributes.Add(AttributeType.Damage, new Attribute(4f));
        _attributes.Add(AttributeType.AttackRate, new Attribute(1f));
        _attributes.Add(AttributeType.HealthMax, new Attribute(100f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    float Get(AttributeType type)
    {
        Attribute attribute = _attributes.GetOrAdd(type);
        attribute.Update();
        return attribute.Value;
    }

    void Equip()
    {
        ItemFactory item = AssetDatabase.LoadAssetAtPath<ItemFactory>(Path);
        Assert.IsNotNull(item, Path);
        foreach (ABuffHandlerFactory handler in item.data.buffs)
        {
            Assert.AreEqual(DurationType.Infinite, handler.durationType);
            foreach (ABuffFactory buffFactory in handler.buffFactoryList)
            {
                buffFactory.GetBuff(null).Add(_go, _go);
            }
        }
    }

    [Test]
    public void Trinity_Plus2Damage_Minus10PercentAttackCooldown_Plus20MaxHealth()
    {
        Equip();

        Assert.AreEqual(6f, Get(AttributeType.Damage), 0.0001f);
        Assert.AreEqual(0.9f, Get(AttributeType.AttackRate), 0.0001f);
        Assert.AreEqual(120f, Get(AttributeType.HealthMax), 0.0001f);
    }

    // Its old +10 damage made every other damage item pointless: it must stay below the item of each role
    [Test]
    public void Trinity_LessOfEachThanTheItemOfThatRole()
    {
        Equip();

        Assert.Less(Get(AttributeType.Damage) - 4f, 5f, "Whetstone");
        Assert.Greater(Get(AttributeType.AttackRate), 0.85f, "Hair Trigger");
        Assert.Less(Get(AttributeType.HealthMax) - 100f, 40f, "Heart Stone");
    }

    [Test]
    public void Trinity_DescriptionSaysItsThreeBonuses()
    {
        ItemFactory item = AssetDatabase.LoadAssetAtPath<ItemFactory>(Path);

        Assert.AreEqual("+2 damage, -10% attack cooldown and +20 max health", item.data.description);
    }
}

}
