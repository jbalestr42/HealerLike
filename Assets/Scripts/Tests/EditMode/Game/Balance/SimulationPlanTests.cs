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
        Assert.IsTrue(jobs.TrueForAll(job => job.bot == null && job.character == character && job.team.units.Count == 2 && job.teamPattern == bags));
        Object.DestroyImmediate(bag);
        Object.DestroyImmediate(bags);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void BuildJobs_EachRewardItem_EveryRewardItemOnEveryUnitOfTheFixedTeam()
    {
        SimulationPlan plan = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/ItemDpsSimulation.asset");
        Assert.IsNotNull(plan);
        RewardPools rewards = RewardPools.Create(_data, null);

        List<SimulationJob> jobs = plan.BuildJobs(_data);

        int units = ReferenceTeam.FromWave(plan.fixedTeam).units.Count;
        Assert.AreEqual(5, units);
        // Each unit in turn: a fight without item, each unit item on it, each character item
        Assert.AreEqual(plan.seedCount * units * (1 + rewards.unitItems.Count + rewards.playerItems.Count), jobs.Count);
        foreach (AItemFactory item in rewards.unitItems)
        {
            List<int> holders = jobs.FindAll(job => job.item == item).ConvertAll(job => job.itemHolder);
            Assert.AreEqual(plan.seedCount * units, holders.Count, item.name);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4 }, new HashSet<int>(holders), item.name);
        }
        Assert.IsTrue(jobs.TrueForAll(job => job.teamPattern == plan.fixedTeam));
    }

    [Test]
    public void CopyFor_AnItem_OnlyThatItem_AndTheFightsWithoutItem()
    {
        SimulationPlan plan = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/ItemDpsSimulation.asset");
        AItemFactory poison = RewardPools.Create(_data, null).unitItems[0];

        SimulationPlan copy = plan.CopyFor(poison);
        List<SimulationJob> jobs = copy.BuildJobs(_data);

        // Under the same name, its log read as the plan's
        Assert.AreEqual(plan.name, copy.name);
        Assert.IsEmpty(plan.onlyItems);
        // Each turn: a fight without item, one with the item
        Assert.AreEqual(plan.seedCount * 5 * 2, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.item == null || job.item == poison));
        Object.DestroyImmediate(copy);
    }

    [Test]
    public void CopyFor_AnItem_TheFightsWithoutItemTakenFromAPreviousMeasure_OnlyTheItem()
    {
        SimulationPlan plan = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/ItemRobustnessSimulation.asset");
        AItemFactory poison = RewardPools.Create(_data, null).unitItems[0];

        SimulationPlan copy = plan.CopyFor(poison, true);
        List<SimulationJob> jobs = copy.BuildJobs(_data);

        Assert.AreEqual(plan.seedCount * 5, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.item == poison));
        Assert.IsFalse(plan.skipFightsWithoutItem);
        Object.DestroyImmediate(copy);
    }

    [Test]
    public void CopyFor_AWave_OnlyThatWave_InsteadOfEveryWaveOfThePools()
    {
        SimulationPlan waveDps = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/WaveDpsSimulation.asset");
        SimulationPlan teamDps = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/BalanceTeamDpsSimulation.asset");
        WavePatternData wave = SimulationJobBuilder.GetWaves(_data)[0].wave;

        SimulationPlan waveCopy = waveDps.CopyFor(wave);
        SimulationPlan teamCopy = teamDps.CopyFor(wave);

        CollectionAssert.AreEqual(new[] { wave }, waveCopy.waves);
        // The balance team against the dummies doesn't play the waves: copied whole
        CollectionAssert.AreEqual(teamDps.waves, teamCopy.waves);
        Object.DestroyImmediate(waveCopy);
        Object.DestroyImmediate(teamCopy);
    }

    [Test]
    public void CopyFor_ACharacter_OnlyItsBots()
    {
        SimulationPlan plan = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/CharacterRobustnessSimulation.asset");
        CharacterData druid = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/DruidCharacter/DruidCharacter.asset");
        Assert.Greater(plan.healerBots.Count, 1);

        SimulationPlan copy = plan.CopyFor(druid);

        Assert.AreEqual(1, copy.healerBots.Count);
        Assert.AreSame(druid, copy.healerBots[0].character);
        Object.DestroyImmediate(copy);
    }

    [Test]
    public void ItemPlans_TheBalanceTeamAgainstTheDummies_ThenTheMortalDummiesAgainstTheBalanceTeam()
    {
        WavePatternData attackers = AssetDatabase.LoadAssetAtPath<WavePatternData>("Assets/Data/Balance/Wave_Balance_Attackers.asset");
        WavePatternData dummies = AssetDatabase.LoadAssetAtPath<WavePatternData>("Assets/Data/Balance/Wave_Balance_Dummies.asset");
        SimulationPlan dps = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/ItemDpsSimulation.asset");
        SimulationPlan robustness = AssetDatabase.LoadAssetAtPath<SimulationPlan>("Assets/Data/Balance/ItemRobustnessSimulation.asset");

        Assert.IsTrue(dps.eachRewardItem);
        Assert.AreSame(attackers, dps.fixedTeam);
        CollectionAssert.AreEqual(new[] { dummies }, dps.waves);
        Assert.IsFalse(dps.fixedTeamDies);
        Assert.IsTrue(robustness.eachRewardItem);
        Assert.AreSame(dummies, robustness.fixedTeam);
        CollectionAssert.AreEqual(new[] { attackers }, robustness.waves);
        Assert.IsTrue(robustness.fixedTeamDies);
        // 2 seeds: 10 fights per item and way, two fights with the same seed already differing by 2 to 4%
        Assert.AreEqual(2, dps.seedCount);
        Assert.AreEqual(2, robustness.seedCount);
        // The same fights as the other measures: 30s of damage, then until the dummies die
        Assert.AreEqual(30f, dps.maxDuration);
        Assert.AreEqual(180f, robustness.maxDuration);
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
    public void BuildJobs_OnlyPoolFloors_EachWaveOnTheFloorsOfItsPools()
    {
        SimulatedWave wave = SimulationJobBuilder.GetWaves(_data).Find(simulated => simulated.roomType == MapNodeType.Combat);
        _plan.waves = new List<WavePatternData> { wave.wave };
        _plan.floors = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        _plan.onlyPoolFloors = true;
        _plan.poolFloorMargin = 0;

        List<SimulationJob> jobs = _plan.BuildJobs(_data);

        Assert.IsNotEmpty(jobs);
        Assert.IsTrue(jobs.TrueForAll(job => job.floor >= wave.minFloor && job.floor <= wave.maxFloor));
        Assert.AreEqual(_plan.CountJobs(_data), jobs.Count);
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
