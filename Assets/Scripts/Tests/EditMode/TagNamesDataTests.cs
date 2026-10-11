using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace Tags
{

// Every tag name the code relies on is a tag registered in the game data, so looking it up never fails
public class TagNamesDataTests
{
    [TestCase("Assets/Data/GameData.asset")]
    [TestCase("Assets/Data/TestData.asset")]
    public void EveryTagName_IsRegistered(string gameDataPath)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);

        foreach (FieldInfo field in typeof(TagNames).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string tagName = (string)field.GetValue(null);
            Assert.IsTrue(data.tags.Exists(tag => tag != null && tag.name == tagName), $"{tagName} in {gameDataPath}");
        }
    }

    // The roles only serve the balancing: grouped under Balance so they can be filtered out
    [TestCase(TagNames.Tank)]
    [TestCase(TagNames.Damage)]
    [TestCase(TagNames.Support)]
    public void Roles_AreChildrenOfBalance(string role)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/GameData.asset");

        GameplayTag tag = data.tags.Find(registered => registered != null && registered.name == role);

        Assert.IsNotNull(tag, role);
        Assert.IsNotNull(tag.parent, role);
        Assert.AreEqual(TagNames.Balance, tag.parent.name);
    }

    // Every unit a class can recruit has a role, so the balancing can give it the right items
    [Test]
    public void RewardUnits_HaveARole()
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset");

        foreach (EntityData unit in data.entities.FindAll(entity => entity != null && entity.HasTag(TagNames.Reward)))
        {
            Assert.IsTrue(unit.HasTag(TagNames.Tank) || unit.HasTag(TagNames.Damage) || unit.HasTag(TagNames.Support), unit.title);
        }
    }
}

}
