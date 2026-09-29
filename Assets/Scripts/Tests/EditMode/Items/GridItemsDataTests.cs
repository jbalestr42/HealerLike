using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Items
{

// The items about the grid and the targeting (new items, stage 5): each one is a droppable reward wired
// to the behaviour it describes
public class GridItemsDataTests
{
    const string EntityItems = "Assets/Data/EntityItems/";

    static readonly object[] Items =
    {
        new object[] { EntityItems + "PhalanxItem/PhalanxItem.asset", "Phalanx", "Entity" },
        new object[] { EntityItems + "LoneWolfItem/LoneWolfItem.asset", "Lone Wolf", "Entity" },
    };

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
    public void Phalanx_Gives2ArmorForEachAdjacentAlly()
    {
        AlliesOnRelativeCellModifierFactory buff = GetBuff<AlliesOnRelativeCellModifierFactory>(LoadItem(EntityItems, "Phalanx"));

        Assert.AreEqual(AttributeType.FlatArmor, buff.data.type);
        Assert.AreEqual(AttributeModifierType.Add, buff.data.modifierType);
        Assert.AreEqual(2f, buff.data.value, 0.0001f);
        Assert.IsFalse(buff.data.isWhenAlone);
        Assert.AreEqual(RelativeCellPatternType.Adjacent, buff.data.pattern);
        Assert.AreEqual(1, buff.data.range);
    }

    [Test]
    public void LoneWolf_Gives50PercentDamageWithoutAdjacentAlly()
    {
        AlliesOnRelativeCellModifierFactory buff = GetBuff<AlliesOnRelativeCellModifierFactory>(LoadItem(EntityItems, "LoneWolf"));

        Assert.AreEqual(AttributeType.Damage, buff.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, buff.data.modifierType);
        Assert.AreEqual(0.5f, buff.data.value, 0.0001f);
        Assert.IsTrue(buff.data.isWhenAlone);
        Assert.AreEqual(RelativeCellPatternType.Adjacent, buff.data.pattern);
        Assert.AreEqual(1, buff.data.range);
    }
}

}
