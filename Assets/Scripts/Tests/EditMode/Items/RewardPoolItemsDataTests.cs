using NUnit.Framework;
using UnityEditor;

namespace Items
{

// The items taken out of the rewards: out of the items of the game data, so no reward or simulation offers them,
// still in the sandbox
public class RewardPoolItemsDataTests
{
    const string IncreaseDamageWithProjectileDistancePath = "Assets/Data/EntityItems/IncreaseDamageWithProjectileDistanceItem/IncreaseDamageWithProjectileDistanceItem.asset";

    [TestCase("Assets/Data/GameData.asset")]
    [TestCase("Assets/Data/TestData.asset")]
    public void IncreaseDamageWithProjectileDistance_IsNotARewardItem(string gameDataPath)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);
        AItemFactory item = AssetDatabase.LoadAssetAtPath<AItemFactory>(IncreaseDamageWithProjectileDistancePath);
        Assert.IsNotNull(item);

        CollectionAssert.DoesNotContain(data.items, item);
    }

    [Test]
    public void IncreaseDamageWithProjectileDistance_StaysInTheSandbox()
    {
        SandboxData sandbox = AssetDatabase.LoadAssetAtPath<SandboxData>("Assets/Data/SandboxData.asset");
        AItemFactory item = AssetDatabase.LoadAssetAtPath<AItemFactory>(IncreaseDamageWithProjectileDistancePath);

        CollectionAssert.Contains(sandbox.items, item);
    }
}

}
