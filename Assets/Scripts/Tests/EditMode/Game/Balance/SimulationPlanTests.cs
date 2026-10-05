using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Balance
{

public class SimulationPlanTests
{
    const string GameDataPath = "Assets/Data/TestData.asset";

    SimulationPlan _plan;
    HealerBotProfile _bot;
    GameData _data;

    [SetUp]
    public void SetUp()
    {
        _data = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
        Assert.IsNotNull(_data, GameDataPath);
        _plan = ScriptableObject.CreateInstance<SimulationPlan>();
        _plan.mapSettings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Data/Run/MapGenerationSettings.asset");
        _bot = ScriptableObject.CreateInstance<HealerBotProfile>();
        _bot.character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/DruidCharacter/DruidCharacter.asset");
        _plan.healerBots = new List<HealerBotProfile> { _bot };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_plan);
        Object.DestroyImmediate(_bot);
    }

    [Test]
    public void GetWaves_NoWaveListed_EveryWaveOfThePools()
    {
        Assert.AreEqual(SimulationJobBuilder.GetWaves(_data).Count, _plan.GetWaves(_data).Count);
    }

    [Test]
    public void GetWaves_WavesListed_OnlyThoseInTheRoomTypeOfTheirPool()
    {
        List<SimulatedWave> all = SimulationJobBuilder.GetWaves(_data);
        SimulatedWave elite = all.Find(wave => wave.roomType == MapNodeType.Elite);
        SimulatedWave combat = all.Find(wave => wave.roomType == MapNodeType.Combat);
        _plan.waves = new List<WavePatternData> { elite.wave, combat.wave };

        List<SimulatedWave> waves = _plan.GetWaves(_data);

        Assert.AreEqual(2, waves.Count);
        Assert.IsTrue(waves.Exists(wave => wave.wave == elite.wave && wave.roomType == MapNodeType.Elite));
        Assert.IsTrue(waves.Exists(wave => wave.wave == combat.wave && wave.roomType == MapNodeType.Combat));
    }

    [Test]
    public void BuildJobs_EveryListedWaveOnEveryFloorForEverySeed()
    {
        List<SimulatedWave> all = SimulationJobBuilder.GetWaves(_data);
        _plan.waves = new List<WavePatternData> { all[0].wave, all[1].wave };
        _plan.floors = new List<int> { 0, 4 };
        _plan.seedCount = 3;

        List<SimulationJob> jobs = _plan.BuildJobs(_data);

        // 1 bot x 3 seeds x 2 floors x 2 waves
        Assert.AreEqual(12, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.bot == _bot && job.team != null && job.team.units.Count >= _bot.character.entities.Count));
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, new HashSet<int>(jobs.ConvertAll(job => job.seed)));
    }

    [Test]
    public void BuildJobs_TheTeamOfAFloorHasTheRewardsOfTheRoomsBefore()
    {
        _plan.waves = new List<WavePatternData> { SimulationJobBuilder.GetWaves(_data)[0].wave };
        _plan.floors = new List<int> { 0, 6 };

        List<SimulationJob> jobs = _plan.BuildJobs(_data);

        Assert.AreEqual(0, jobs.Find(job => job.floor == 0).team.rewardCount);
        Assert.Greater(jobs.Find(job => job.floor == 6).team.rewardCount, 0);
    }

    [Test]
    public void BuildJobs_InitialTeams_TheStartingUnitsOfTheBots_InsteadOfTheFloors()
    {
        WavePatternData attackers = AssetDatabase.LoadAssetAtPath<WavePatternData>("Assets/Data/Balance/Wave_Balance_Attackers.asset");
        _plan.waves = new List<WavePatternData> { attackers };
        _plan.initialTeams = true;
        _plan.castSpells = false;
        _plan.seedCount = 2;

        List<SimulationJob> jobs = _plan.BuildJobs(_data);

        // 1 bot x 2 seeds x 1 wave, whatever the floors
        Assert.AreEqual(2, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.bot == null && job.character == _bot.character && job.wave == attackers && job.floor == 0));
        CollectionAssert.AreEqual(_bot.character.entities, jobs[0].team.units);
    }

    [Test]
    public void BuildJobs_FixedTeam_EveryWaveAgainstItsUnits_InsteadOfTheBots()
    {
        EntityData bag = ScriptableObject.CreateInstance<EntityData>();
        WavePatternData bags = ScriptableObject.CreateInstance<WavePatternData>();
        bags.slots = new EntitySlot[1, 2];
        bags.slots[0, 0].entity = bag;
        bags.slots[0, 1].entity = bag;
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
        _plan.fixedTeam = bags;
        _plan.fixedTeamCharacter = character;
        _plan.seedCount = 2;

        List<SimulationJob> jobs = _plan.BuildJobs(_data);

        // 2 seeds x every wave, whatever the floors
        Assert.AreEqual(2 * SimulationJobBuilder.GetWaves(_data).Count, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.bot == null && job.character == character && job.team.units.Count == 2));
        Object.DestroyImmediate(bag);
        Object.DestroyImmediate(bags);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void GetWaves_AListedWaveInNoPool_PlayedAsACombat()
    {
        WavePatternData dummies = ScriptableObject.CreateInstance<WavePatternData>();
        _plan.waves = new List<WavePatternData> { dummies };

        List<SimulatedWave> waves = _plan.GetWaves(_data);

        Object.DestroyImmediate(dummies);
        Assert.AreEqual(1, waves.Count);
        Assert.AreEqual(MapNodeType.Combat, waves[0].roomType);
    }

    [Test]
    public void GetMaxDuration_LongerForAnEliteOrABoss()
    {
        _plan.maxDuration = 30f;
        _plan.eliteMaxDuration = 60f;

        Assert.AreEqual(30f, _plan.GetMaxDuration(MapNodeType.Combat));
        Assert.AreEqual(60f, _plan.GetMaxDuration(MapNodeType.Elite));
        Assert.AreEqual(60f, _plan.GetMaxDuration(MapNodeType.Boss));
    }
}

}
