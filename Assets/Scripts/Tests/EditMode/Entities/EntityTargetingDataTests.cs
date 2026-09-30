using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Entities
{

// Checks the targeting of every entity asset: First is forbidden, each unit picks a targeting that
// matches its gameplay (First is also the default value, so a new entity must choose one)
public class EntityTargetingDataTests
{
    [Test]
    public void NoEntity_TargetsFirst()
    {
        string[] guids = AssetDatabase.FindAssets("t:EntityData", new[] { "Assets/Data" });
        Assert.IsNotEmpty(guids, "No entity asset found");

        List<string> first = new List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (data != null && data.targetBehaviourType == TargetBehaviourType.First)
            {
                first.Add(path);
            }
        }

        Assert.IsEmpty(first, "Entities targeting First");
    }

    [TestCase("SwarmEntity", TargetBehaviourType.Random)]
    [TestCase("FrostCasterEntity", TargetBehaviourType.Nearest)]
    [TestCase("NormalEntity", TargetBehaviourType.Nearest)]
    public void Entity_UsesTheTargetingOfItsGameplay(string name, TargetBehaviourType expected)
    {
        EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>($"Assets/Data/Entities/{name}/{name}.asset");

        Assert.AreEqual(expected, data.targetBehaviourType);
    }
}

}
