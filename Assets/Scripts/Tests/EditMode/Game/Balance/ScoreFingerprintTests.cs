using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Balance
{

public class ScoreFingerprintTests
{
    const string CryptPath = "Assets/Data/WavePatterns/Wave_Crypt.asset";

    readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    T Create<T>(string name) where T : ScriptableObject
    {
        T created = ScriptableObject.CreateInstance<T>();
        created.name = name;
        _created.Add(created);
        return created;
    }

    WavePatternData CreateWave(params EntityData[] units)
    {
        WavePatternData wave = Create<WavePatternData>("Wave");
        wave.width = units.Length;
        wave.height = 1;
        wave.slots = new EntitySlot[units.Length, 1];
        for (int i = 0; i < units.Length; i++)
        {
            wave.slots[i, 0].entity = units[i];
        }
        return wave;
    }

    [Test]
    public void SameUnitsOnTheSameSlots_SameFingerprint()
    {
        EntityData soldier = Create<EntityData>("Soldier");

        Assert.AreEqual(ScoreFingerprint.Compute(CreateWave(soldier, null), new Object[0]), ScoreFingerprint.Compute(CreateWave(soldier, null), new Object[0]));
    }

    [Test]
    public void AnotherUnitOrSlot_AnotherFingerprint()
    {
        EntityData soldier = Create<EntityData>("Soldier");
        EntityData sniper = Create<EntityData>("Sniper");
        string fingerprint = ScoreFingerprint.Compute(CreateWave(soldier, null), new Object[0]);

        Assert.AreNotEqual(fingerprint, ScoreFingerprint.Compute(CreateWave(sniper, null), new Object[0]));
        Assert.AreNotEqual(fingerprint, ScoreFingerprint.Compute(CreateWave(null, soldier), new Object[0]));
    }

    [Test]
    public void AWaveOfTheGame_DependsOnItsDataAndOnTheMeasureSetup()
    {
        WavePatternData crypt = AssetDatabase.LoadAssetAtPath<WavePatternData>(CryptPath);
        Assert.IsNotNull(crypt, CryptPath);
        SimulationPlan setup = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/WaveRobustnessSimulation.asset");

        string fingerprint = ScoreFingerprint.Compute(crypt, new Object[] { setup });

        Assert.AreEqual(fingerprint, ScoreFingerprint.Compute(crypt, new Object[] { setup }));
        Assert.AreNotEqual(fingerprint, ScoreFingerprint.Compute(crypt, new Object[0]));
    }

    [Test]
    public void DataDependencies_FollowedThroughDataOnly_NotThroughTheModelsAndTheHud()
    {
        HashSet<string> dependencies = ScoreFingerprint.CollectDataDependencies(new[] { CryptPath });

        // The wave, its units and their skills
        Assert.IsTrue(dependencies.Contains(CryptPath));
        Assert.IsTrue(dependencies.Contains("Assets/Data/Entities/SkeletonEntity/SkeletonEntity.asset"));
        Assert.IsTrue(dependencies.Contains("Assets/Data/Entities/NecromancerEntity/SummonSkillFactory.asset"));
        // The font is only reached through the HUD prefab of the units
        Assert.IsFalse(dependencies.Contains("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"));
        foreach (string dependency in dependencies)
        {
            Assert.AreEqual(".asset", System.IO.Path.GetExtension(dependency), dependency);
        }
    }

    [Test]
    public void ForData_DependsOnTheMeasuredData_NotOnTheExcludedOnes()
    {
        CharacterData cleric = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/ClericCharacter/ClericCharacter.asset");
        CharacterData druid = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/DruidCharacter/DruidCharacter.asset");
        Assert.IsNotNull(cleric);
        Assert.IsNotNull(druid);

        string clericFingerprint = ScoreFingerprint.ComputeForData(new Object[] { cleric }, new Object[0]);

        Assert.AreEqual(clericFingerprint, ScoreFingerprint.ComputeForData(new Object[] { cleric }, new Object[0]));
        Assert.AreNotEqual(clericFingerprint, ScoreFingerprint.ComputeForData(new Object[] { druid }, new Object[0]));
        // The Druid in the setup counts, unless excluded
        Assert.AreNotEqual(clericFingerprint, ScoreFingerprint.ComputeForData(new Object[] { cleric }, new Object[] { druid }));
        Assert.AreEqual(clericFingerprint, ScoreFingerprint.ComputeForData(new Object[] { cleric }, new Object[] { druid }, new Object[] { druid }));
    }

    [Test]
    public void AllForData_EachTheSameAsOnItsOwn()
    {
        CharacterData cleric = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/ClericCharacter/ClericCharacter.asset");
        CharacterData druid = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/DruidCharacter/DruidCharacter.asset");
        SimulationPlan setup = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/ItemRobustnessSimulation.asset");
        Assert.IsNotNull(setup);

        Dictionary<Object, string> fingerprints = ScoreFingerprint.ComputeAllForData(new Object[] { cleric, druid }, new Object[] { setup });

        Assert.AreEqual(ScoreFingerprint.ComputeForData(new Object[] { cleric }, new Object[] { setup }), fingerprints[cleric]);
        Assert.AreEqual(ScoreFingerprint.ComputeForData(new Object[] { druid }, new Object[] { setup }), fingerprints[druid]);
        Assert.AreNotEqual(fingerprints[cleric], fingerprints[druid]);
    }

    [Test]
    public void DataDependencies_ExcludedOnes_NeitherCountedNorFollowed()
    {
        string skeleton = "Assets/Data/Entities/SkeletonEntity/SkeletonEntity.asset";

        HashSet<string> dependencies = ScoreFingerprint.CollectDataDependencies(new[] { CryptPath }, null, new[] { skeleton });

        Assert.IsFalse(dependencies.Contains(skeleton));
        Assert.IsFalse(dependencies.Contains("Assets/Data/Entities/SkeletonEntity/ShootProjectileSkillFactory.asset"));
        Assert.IsTrue(dependencies.Contains("Assets/Data/Entities/SoldierEntity/SoldierEntity.asset"));
    }

    [Test]
    public void DataAssetsCount_FontsRewrittenInPlayModeDoNot()
    {
        Assert.IsTrue(ScoreFingerprint.CountsInFingerprint(CryptPath));
        Assert.IsTrue(ScoreFingerprint.CountsInFingerprint("Assets/Data/Balance/WaveRobustnessSimulation.asset"));
        Assert.IsFalse(ScoreFingerprint.CountsInFingerprint("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"));
        Assert.IsFalse(ScoreFingerprint.CountsInFingerprint("Assets/Scenes/BalanceSimulation.unity"));
    }
}

}
