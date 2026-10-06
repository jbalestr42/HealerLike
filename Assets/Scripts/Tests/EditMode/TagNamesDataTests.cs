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
}

}
