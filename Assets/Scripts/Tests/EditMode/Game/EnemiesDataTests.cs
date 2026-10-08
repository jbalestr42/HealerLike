using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Game
{

// Checks the enemies of the project data and the waves fielding them
public class EnemiesDataTests
{
    const string EntitiesPath = "Assets/Data/Entities/";
    const string WavesPath = "Assets/Data/WavePatterns/";

    static EntityData LoadEnemy(string name)
    {
        EntityData enemy = AssetDatabase.LoadAssetAtPath<EntityData>(EntitiesPath + name + "Entity/" + name + "Entity.asset");
        Assert.IsNotNull(enemy, name);
        return enemy;
    }

    static WavePatternData LoadWave(string name)
    {
        WavePatternData wave = AssetDatabase.LoadAssetAtPath<WavePatternData>(WavesPath + name + ".asset");
        Assert.IsNotNull(wave, name);
        return wave;
    }

    // Titles of the enemies of a rank of the wave, 0 being the front one, empty slots left out
    static List<string> GetRank(WavePatternData wave, int rank)
    {
        List<string> titles = new List<string>();
        for (int i = 0; i < wave.slots.GetLength(1); i++)
        {
            EntityData entity = wave.slots[rank, i].entity;
            if (entity != null)
            {
                titles.Add(entity.title);
            }
        }
        return titles;
    }

    [Test]
    public void Archer_ShootsWeakShotsAtYourFarthestUnit()
    {
        EntityData archer = LoadEnemy("Archer");

        Assert.AreEqual("Archer", archer.title);
        Assert.IsFalse(string.IsNullOrEmpty(archer.description));
        Assert.IsNotNull(archer.model);
        Assert.AreEqual(TargetBehaviourType.Farest, archer.targetBehaviourType);
        Assert.AreEqual(100f, archer.attributes[AttributeType.HealthMax]);
        Assert.AreEqual(3f, archer.attributes[AttributeType.Damage]);
        // A shot every second
        Assert.AreEqual(1f, archer.attributes[AttributeType.AttackRate]);
        Assert.AreEqual(1, archer.skillFactories.Count, "it attacks");
    }

    [Test]
    public void FrontLine_FieldsTwoSoldiersInFrontAndTwoArchersBehind()
    {
        WavePatternData wave = LoadWave("Wave_FrontLine");

        Assert.AreEqual(2, wave.slots.GetLength(0));
        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Archer", "Archer" }, GetRank(wave, 1));
    }
}

}
