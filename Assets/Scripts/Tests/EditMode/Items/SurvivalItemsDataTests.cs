using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Items
{

// The items about surviving and dying (new items, stage 4): each one is a droppable reward wired to the
// behaviour it describes
public class SurvivalItemsDataTests
{
    const string EntityItems = "Assets/Data/EntityItems/";

    static readonly object[] Items =
    {
        new object[] { EntityItems + "SecondWindItem/SecondWindItem.asset", "Second Wind", "Entity" },
        new object[] { EntityItems + "PhylacteryItem/PhylacteryItem.asset", "Phylactery", "Entity" },
        new object[] { EntityItems + "BoneCharmItem/BoneCharmItem.asset", "Bone Charm", "Entity" },
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
    public void SecondWind_Below25PercentHealthTheUnitIsInvincibleFor2Seconds()
    {
        ApplyBuffBelowHealthBuffFactory buff = GetBuff<ApplyBuffBelowHealthBuffFactory>(LoadItem(EntityItems, "SecondWind"));

        Assert.AreEqual(0.25f, buff.data.threshold, 0.0001f);
        ABuffHandlerFactory invincibility = buff.data.buffHandlerFactory;
        Assert.IsNotNull(invincibility);
        Assert.AreEqual(DurationType.Duration, invincibility.durationType);
        Assert.AreEqual(2f, invincibility.duration, 0.0001f);
        Assert.IsTrue(invincibility.buffFactoryList.Exists(factory => factory is InvincibilityBuffFactory));
    }

    [Test]
    public void Phylactery_TheFirstDeathOfAFightComesBackWith30PercentHealth()
    {
        ReviveOnDeathBuffFactory buff = GetBuff<ReviveOnDeathBuffFactory>(LoadItem(EntityItems, "Phylactery"));

        Assert.AreEqual(0.3f, buff.data.healthRatio, 0.0001f);
    }

    [Test]
    public void BoneCharm_EachKilledEnemyRisesAsASkeleton()
    {
        SummonOnKillBuffFactory buff = GetBuff<SummonOnKillBuffFactory>(LoadItem(EntityItems, "BoneCharm"));

        Assert.IsNotNull(buff.data.entity);
        Assert.AreEqual("Skeleton", buff.data.entity.title);
    }
}

}
