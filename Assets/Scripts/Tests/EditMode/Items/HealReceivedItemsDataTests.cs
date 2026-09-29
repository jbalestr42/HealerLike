using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Items
{

// The items reacting to a heal received (new items, stage 3): each one is a droppable reward wired to
// the behaviour it describes
public class HealReceivedItemsDataTests
{
    const string EntityItems = "Assets/Data/EntityItems/";

    static readonly object[] Items =
    {
        new object[] { EntityItems + "GratitudeItem/GratitudeItem.asset", "Gratitude", "Entity" },
        new object[] { EntityItems + "ThornsOfLifeItem/ThornsOfLifeItem.asset", "Thorns of Life", "Entity" },
        new object[] { EntityItems + "MartyrsHeartItem/MartyrsHeartItem.asset", "Martyr's Heart", "Entity" },
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
    public void Gratitude_AfterAHealTheNextAttackDeals50PercentMoreDamage()
    {
        EmpowerNextAttackOnHealBuffFactory gratitude = GetBuff<EmpowerNextAttackOnHealBuffFactory>(LoadItem(EntityItems, "Gratitude"));

        Assert.AreEqual(1.5f, gratitude.data.damageMultiplier, 0.0001f);
    }

    [Test]
    public void ThornsOfLife_WhenHealedTheNearestEnemyTakes25PercentOfTheHeal()
    {
        DamageEnemyOnHealBuffFactory buff = GetBuff<DamageEnemyOnHealBuffFactory>(LoadItem(EntityItems, "ThornsOfLife"));

        Assert.AreEqual(0.25f, buff.data.ratio, 0.0001f);
        Assert.AreEqual(TargetBehaviourType.Nearest, buff.data.targetType);
    }

    [Test]
    public void MartyrsHeart_WhenHealedTheAdjacentAlliesReceive20PercentOfTheHeal()
    {
        ShareHealOnRelativeCellBuffFactory buff = GetBuff<ShareHealOnRelativeCellBuffFactory>(LoadItem(EntityItems, "MartyrsHeart"));

        Assert.AreEqual(0.2f, buff.data.ratio, 0.0001f);
        Assert.AreEqual(RelativeCellPatternType.Adjacent, buff.data.pattern);
        Assert.AreEqual(1, buff.data.range);
        // The zone is shown around the holder
        Assert.IsNotNull(buff.data.cellPrefab);
    }
}

}
