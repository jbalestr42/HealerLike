using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// The Sacred Tome player item: +10 Heal Power for the whole run
public class HealPowerItemTests
{
    const string ItemPath = "Assets/Data/PlayerItems/HealPowerItem/HealPowerItem.asset";

    ItemFactory _item;
    GameObject _character;

    [SetUp]
    public void SetUp()
    {
        _item = AssetDatabase.LoadAssetAtPath<ItemFactory>(ItemPath);
        Assert.IsNotNull(_item);
        _character = new GameObject("Character");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_character);
    }

    [Test]
    public void Item_IsAPermanentPlayerItemWithANameDescriptionAndIcon()
    {
        Assert.AreEqual("Sacred Tome", _item.data.name);
        Assert.IsFalse(string.IsNullOrEmpty(_item.data.description));
        Assert.IsNotNull(_item.data.icon);
        Assert.IsTrue(_item.data.tags.Exists(tag => tag != null && tag.name == "Player"));
        Assert.AreEqual(1, _item.data.buffs.Count);
        Assert.AreEqual(DurationType.Infinite, _item.data.buffs[0].durationType);
    }

    [Test]
    public void Item_Adds10HealPower()
    {
        AttributeManager attributeManager = TestHelpers.CreateAttributeManager(_character, AttributeType.HealPower, 25f);
        ABuff buff = _item.data.buffs[0].buffFactoryList[0].GetBuff(null);

        buff.Add(_character, _character);
        Attribute healPower = attributeManager.Get(AttributeType.HealPower);
        healPower.Update();

        Assert.AreEqual(35f, healPower.Value, 0.0001f);
    }

    [Test]
    public void Item_IsOfferedInTheGameAndTheSandbox()
    {
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset").items.Contains(_item));
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<SandboxData>("Assets/Data/SandboxData.asset").items.Contains(_item));
    }
}

}
