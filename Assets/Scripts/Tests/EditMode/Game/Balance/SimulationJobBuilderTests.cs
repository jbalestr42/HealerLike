using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Balance
{

public class SimulationJobBuilderTests
{
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

    static List<ReferenceTeam> Teams(params int[] floors)
    {
        return new List<int>(floors).ConvertAll(floor => new ReferenceTeam { floor = floor });
    }

    [Test]
    public void GetTeamOnFloor_TheTeamEnteringTheFloor()
    {
        List<ReferenceTeam> teams = Teams(0, 1, 2, 3);

        Assert.AreSame(teams[2], SimulationJobBuilder.GetTeamOnFloor(teams, 2));
    }

    [Test]
    public void GetTeamOnFloor_FloorNotOnThePath_TheLastTeamBefore()
    {
        List<ReferenceTeam> teams = Teams(0, 1, 4);

        Assert.AreSame(teams[1], SimulationJobBuilder.GetTeamOnFloor(teams, 3));
        Assert.AreSame(teams[2], SimulationJobBuilder.GetTeamOnFloor(teams, 9));
    }

    [Test]
    public void GetTeamOnFloor_BeforeTheFirstRoom_None()
    {
        Assert.IsNull(SimulationJobBuilder.GetTeamOnFloor(Teams(2, 3), 1));
    }

    HealerBotProfile CreateBot(CharacterData character)
    {
        HealerBotProfile bot = Create<HealerBotProfile>($"{(character != null ? character.name : "No")}Bot");
        bot.character = character;
        return bot;
    }

    [Test]
    public void BuildForInitialTeams_EveryWaveAgainstTheStartingUnitsOfEveryCharacter_ForEverySeed()
    {
        EntityData knight = Create<EntityData>("Knight");
        EntityData archer = Create<EntityData>("Archer");
        CharacterData cleric = Create<CharacterData>("Cleric");
        cleric.entities = new List<EntityData> { knight, archer };
        CharacterData druid = Create<CharacterData>("Druid");
        druid.entities = new List<EntityData> { archer };
        HealerBotProfile clericBot = CreateBot(cleric);
        HealerBotProfile druidBot = CreateBot(druid);
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave_Balance_Attackers"), roomType = MapNodeType.Combat } };

        List<SimulationJob> jobs = SimulationJobBuilder.BuildForInitialTeams(new[] { clericBot, druidBot, CreateBot(null) }, waves, 2, true);

        // 2 characters x 2 seeds x 1 wave, the bot without character skipped
        Assert.AreEqual(4, jobs.Count);
        List<SimulationJob> clericJobs = jobs.FindAll(job => job.character == cleric);
        Assert.AreEqual(2, clericJobs.Count);
        Assert.IsTrue(clericJobs.TrueForAll(job => job.bot == clericBot && job.floor == 0));
        // As a run starts: the starting units, no reward
        CollectionAssert.AreEqual(new[] { knight, archer }, clericJobs[0].team.units);
        Assert.AreEqual(0, clericJobs[0].team.unitItems.Count);
        Assert.AreEqual(0, clericJobs[0].team.playerItems.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, clericJobs.ConvertAll(job => job.seed));
    }

    [Test]
    public void BuildForInitialTeams_WithoutSpells_TheCharacterCastsNothing()
    {
        CharacterData cleric = Create<CharacterData>("Cleric");
        HealerBotProfile clericBot = CreateBot(cleric);
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave_Balance_Dummies"), roomType = MapNodeType.Combat } };

        List<SimulationJob> jobs = SimulationJobBuilder.BuildForInitialTeams(new[] { clericBot }, waves, 1, false);

        Assert.AreEqual(1, jobs.Count);
        Assert.IsNull(jobs[0].bot);
        Assert.AreSame(cleric, jobs[0].character);
    }

    [Test]
    public void Build_EveryWaveOnEveryFloorForEveryBotAndSeed()
    {
        CharacterData cleric = Create<CharacterData>("Cleric");
        CharacterData druid = Create<CharacterData>("Druid");
        HealerBotProfile clericBot = CreateBot(cleric);
        HealerBotProfile druidBot = CreateBot(druid);
        WavePatternData crypt = Create<WavePatternData>("Wave_Crypt");
        WavePatternData bastion = Create<WavePatternData>("Wave_Elite_Bastion");
        List<SimulatedWave> waves = new List<SimulatedWave>
        {
            new SimulatedWave { wave = crypt, roomType = MapNodeType.Combat },
            new SimulatedWave { wave = bastion, roomType = MapNodeType.Elite },
        };
        List<(CharacterData, int)> generated = new List<(CharacterData, int)>();

        List<SimulationJob> jobs = SimulationJobBuilder.Build(new List<HealerBotProfile> { clericBot, druidBot }, waves, new List<int> { 0, 5 }, 2, (character, seed) =>
        {
            generated.Add((character, seed));
            return Teams(0, 1, 2, 3, 4, 5);
        });

        // 2 characters x 2 seeds x 2 floors x 2 waves
        Assert.AreEqual(16, jobs.Count);
        CollectionAssert.AreEqual(new[] { (cleric, 1), (cleric, 2), (druid, 1), (druid, 2) }, generated);
        SimulationJob job = jobs.Find(found => found.bot == druidBot && found.seed == 2 && found.floor == 5 && found.wave == bastion);
        Assert.IsNotNull(job);
        Assert.AreSame(druid, job.character);
        Assert.AreEqual(MapNodeType.Elite, job.roomType);
        Assert.AreEqual(5, job.team.floor);
    }

    [Test]
    public void Build_FloorBeforeTheFirstRoom_Skipped()
    {
        List<SimulationJob> jobs = SimulationJobBuilder.Build(new List<HealerBotProfile> { CreateBot(Create<CharacterData>("Cleric")) },
            new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave") } }, new List<int> { 0, 1 }, 1, (character, seed) => Teams(1));

        Assert.AreEqual(1, jobs.Count);
        Assert.AreEqual(1, jobs[0].floor);
    }

    [Test]
    public void Build_TwoBotsOfTheSameCharacter_EachPlaysEveryFight()
    {
        CharacterData cleric = Create<CharacterData>("Cleric");
        HealerBotProfile cautious = CreateBot(cleric);
        HealerBotProfile noHeal = CreateBot(cleric);
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave") } };

        List<SimulationJob> jobs = SimulationJobBuilder.Build(new List<HealerBotProfile> { cautious, noHeal }, waves, new List<int> { 0 }, 1, (character, seed) => Teams(0));

        Assert.AreEqual(2, jobs.Count);
        Assert.AreSame(cautious, jobs[0].bot);
        Assert.AreSame(noHeal, jobs[1].bot);
        Assert.IsTrue(jobs.TrueForAll(job => job.character == cleric));
    }

    [Test]
    public void Build_NoBotOrBotWithoutCharacter_Skipped()
    {
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave") } };

        List<SimulationJob> jobs = SimulationJobBuilder.Build(new List<HealerBotProfile> { null, CreateBot(null) }, waves, new List<int> { 0 }, 1, (character, seed) => Teams(0));

        Assert.IsEmpty(jobs);
    }

    [Test]
    public void GetWaves_EachWaveOnce_InTheRoomTypeOfItsFirstPool()
    {
        GameData data = Create<GameData>("Data");
        WavePatternData crypt = Create<WavePatternData>("Wave_Crypt");
        WavePatternData bastion = Create<WavePatternData>("Wave_Elite_Bastion");
        data.wavePools = new List<GameData.WavePool>
        {
            new GameData.WavePool { roomType = MapNodeType.Combat, wavePatterns = new List<WavePatternData> { crypt, null } },
            new GameData.WavePool { roomType = MapNodeType.Elite, wavePatterns = new List<WavePatternData> { bastion } },
            new GameData.WavePool { roomType = MapNodeType.Combat, wavePatterns = new List<WavePatternData> { crypt } },
        };

        List<SimulatedWave> waves = SimulationJobBuilder.GetWaves(data);

        Assert.AreEqual(2, waves.Count);
        Assert.AreSame(crypt, waves[0].wave);
        Assert.AreEqual(MapNodeType.Combat, waves[0].roomType);
        Assert.AreSame(bastion, waves[1].wave);
        Assert.AreEqual(MapNodeType.Elite, waves[1].roomType);
    }

    [Test]
    public void GetWaves_TheFloorsOfAllThePoolsOfTheWave()
    {
        GameData data = Create<GameData>("Data");
        WavePatternData crypt = Create<WavePatternData>("Wave_Crypt");
        data.wavePools = new List<GameData.WavePool>
        {
            new GameData.WavePool { roomType = MapNodeType.Combat, minFloor = 5, maxFloor = 8, wavePatterns = new List<WavePatternData> { crypt } },
            new GameData.WavePool { roomType = MapNodeType.Combat, minFloor = 2, maxFloor = 6, wavePatterns = new List<WavePatternData> { crypt } },
        };

        SimulatedWave wave = SimulationJobBuilder.GetWaves(data)[0];

        Assert.IsTrue(wave.hasPoolFloors);
        Assert.AreEqual(2, wave.minFloor);
        Assert.AreEqual(8, wave.maxFloor);
    }

    [Test]
    public void IsPlayedOnFloor_TheFloorsOfItsPools_GiveOrTakeTheMargin()
    {
        SimulatedWave wave = new SimulatedWave { hasPoolFloors = true, minFloor = 4, maxFloor = 7 };

        Assert.IsFalse(SimulationJobBuilder.IsPlayedOnFloor(wave, 2, 1));
        Assert.IsTrue(SimulationJobBuilder.IsPlayedOnFloor(wave, 3, 1));
        Assert.IsTrue(SimulationJobBuilder.IsPlayedOnFloor(wave, 8, 1));
        Assert.IsFalse(SimulationJobBuilder.IsPlayedOnFloor(wave, 9, 1));
        Assert.IsFalse(SimulationJobBuilder.IsPlayedOnFloor(wave, 3, 0));
    }

    [Test]
    public void IsPlayedOnFloor_NegativeMarginOrAWaveInNoPool_EveryFloor()
    {
        Assert.IsTrue(SimulationJobBuilder.IsPlayedOnFloor(new SimulatedWave { hasPoolFloors = true, minFloor = 4, maxFloor = 7 }, 0, -1));
        Assert.IsTrue(SimulationJobBuilder.IsPlayedOnFloor(new SimulatedWave(), 12, 1));
    }

    [Test]
    public void Build_WithAMargin_EachWaveOnlyOnTheFloorsOfItsPools()
    {
        HealerBotProfile bot = CreateBot(Create<CharacterData>("Cleric"));
        WavePatternData early = Create<WavePatternData>("Wave_Early");
        WavePatternData late = Create<WavePatternData>("Wave_Late");
        List<SimulatedWave> waves = new List<SimulatedWave>
        {
            new SimulatedWave { wave = early, hasPoolFloors = true, minFloor = 0, maxFloor = 1 },
            new SimulatedWave { wave = late, hasPoolFloors = true, minFloor = 4, maxFloor = 5 },
        };
        List<int> floors = new List<int> { 0, 1, 2, 3, 4, 5 };

        List<SimulationJob> jobs = SimulationJobBuilder.Build(new[] { bot }, waves, floors, 1, (character, seed) => Teams(0), poolFloorMargin: 1);

        CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, jobs.FindAll(job => job.wave == early).ConvertAll(job => job.floor));
        CollectionAssert.AreEquivalent(new[] { 3, 4, 5 }, jobs.FindAll(job => job.wave == late).ConvertAll(job => job.floor));
    }

    [Test]
    public void CountJobs_IsTheCountOfTheFightsBuilt()
    {
        HealerBotProfile cleric = CreateBot(Create<CharacterData>("Cleric"));
        HealerBotProfile druid = CreateBot(Create<CharacterData>("Druid"));
        List<SimulatedWave> waves = new List<SimulatedWave>
        {
            new SimulatedWave { wave = Create<WavePatternData>("Wave_Early"), hasPoolFloors = true, minFloor = 0, maxFloor = 1 },
            new SimulatedWave { wave = Create<WavePatternData>("Wave_Late"), hasPoolFloors = true, minFloor = 4, maxFloor = 5 },
        };
        List<int> floors = new List<int> { 0, 1, 2, 3, 4, 5 };
        HealerBotProfile[] bots = { cleric, druid, CreateBot(null) };

        int built = SimulationJobBuilder.Build(bots, waves, floors, 3, (character, seed) => Teams(0), poolFloorMargin: 1).Count;

        Assert.AreEqual(built, SimulationJobBuilder.CountJobs(bots, waves, floors, 3, 1));
        // 2 bots x 3 seeds x 6 floors x 2 waves without margin
        Assert.AreEqual(72, SimulationJobBuilder.CountJobs(bots, waves, floors, 3));
    }

    [Test]
    public void GetWaves_GameDataOfTheGame_HasEliteAndBossWaves()
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset");

        List<SimulatedWave> waves = SimulationJobBuilder.GetWaves(data);

        Assert.IsNotEmpty(waves);
        Assert.IsTrue(waves.Exists(wave => wave.roomType == MapNodeType.Elite));
        Assert.IsTrue(waves.Exists(wave => wave.roomType == MapNodeType.Boss));
    }

    [Test]
    public void BuildForFixedTeam_EveryWaveForEverySeed_WithoutBot()
    {
        CharacterData character = Create<CharacterData>("Balance");
        ReferenceTeam team = new ReferenceTeam();
        WavePatternData crypt = Create<WavePatternData>("Wave_Crypt");
        WavePatternData bastion = Create<WavePatternData>("Wave_Elite_Bastion");
        List<SimulatedWave> waves = new List<SimulatedWave>
        {
            new SimulatedWave { wave = crypt, roomType = MapNodeType.Combat },
            new SimulatedWave { wave = bastion, roomType = MapNodeType.Elite },
        };

        List<SimulationJob> jobs = SimulationJobBuilder.BuildForFixedTeam(character, team, waves, 3);

        // 3 seeds x 2 waves
        Assert.AreEqual(6, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.bot == null && job.character == character && job.team == team));
        Assert.IsNotNull(jobs.Find(job => job.wave == bastion && job.roomType == MapNodeType.Elite && job.seed == 3));
    }

    [Test]
    public void BuildForFixedTeam_NoCharacter_None()
    {
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave") } };

        Assert.IsEmpty(SimulationJobBuilder.BuildForFixedTeam(null, new ReferenceTeam(), waves, 1));
    }

    [Test]
    public void BuildForFixedTeam_WithAPattern_EveryJobPlacesTheTeamAsDrawn()
    {
        WavePatternData dummies = Create<WavePatternData>("Wave_Balance_Dummies");
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave") } };

        List<SimulationJob> jobs = SimulationJobBuilder.BuildForFixedTeam(Create<CharacterData>("Balance"), new ReferenceTeam(), waves, 2, dummies);

        Assert.AreEqual(2, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.teamPattern == dummies));
    }

    AItemFactory CreateItem(string name)
    {
        ItemFactory item = Create<ItemFactory>(name);
        item.data = new ItemData { name = name };
        return item;
    }

    // A team of three units against one wave, with two unit items and one character item
    List<SimulationJob> BuildForEachItem(int seedCount, out ReferenceTeam team, out AItemFactory bounce, out AItemFactory poison, out AItemFactory beads)
    {
        team = new ReferenceTeam { units = new List<EntityData> { Create<EntityData>("Sniper"), Create<EntityData>("Gunner"), Create<EntityData>("Mortar") } };
        bounce = CreateItem("BounceItem");
        poison = CreateItem("PoisonItem");
        beads = CreateItem("PrayerBeadsItem");
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave_Balance_Dummies"), roomType = MapNodeType.Combat } };
        return SimulationJobBuilder.BuildForEachItem(Create<CharacterData>("Balance"), team, waves, seedCount, new[] { bounce, poison }, new[] { beads });
    }

    [Test]
    public void BuildForEachItem_EachUnitItemHeldByEveryUnitInTurn_EachCharacterItemAsOften_AndAFightWithoutItemPerTurn()
    {
        List<SimulationJob> jobs = BuildForEachItem(1, out ReferenceTeam team, out AItemFactory bounce, out AItemFactory poison, out AItemFactory beads);

        // 3 turns x (no item + 2 unit items + 1 character item)
        Assert.AreEqual(12, jobs.Count);
        List<SimulationJob> bounceJobs = jobs.FindAll(job => job.item == bounce);
        CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, bounceJobs.ConvertAll(job => job.itemHolder));
        foreach (SimulationJob job in bounceJobs)
        {
            Assert.AreEqual(1, job.team.unitItems.Count);
            Assert.AreEqual(job.itemHolder, job.team.unitItems[0].unit);
            Assert.AreSame(bounce, job.team.unitItems[0].item);
            Assert.IsEmpty(job.team.playerItems);
        }

        List<SimulationJob> beadsJobs = jobs.FindAll(job => job.item == beads);
        Assert.AreEqual(3, beadsJobs.Count);
        Assert.IsTrue(beadsJobs.TrueForAll(job => job.itemHolder == SimulationJob.NoHolder && job.team.playerItems.Count == 1 && job.team.playerItems[0] == beads && job.team.unitItems.Count == 0));

        List<SimulationJob> withoutItem = jobs.FindAll(job => job.item == null);
        Assert.AreEqual(3, withoutItem.Count);
        Assert.IsTrue(withoutItem.TrueForAll(job => job.team == team && job.itemHolder == SimulationJob.NoHolder));
        // The team itself is never given an item
        Assert.IsEmpty(team.unitItems);
        Assert.IsEmpty(team.playerItems);
    }

    [Test]
    public void BuildForEachItem_TheFightsOfATurnShareTheSeedOfItsFightWithoutItem()
    {
        List<SimulationJob> jobs = BuildForEachItem(2, out ReferenceTeam _, out AItemFactory bounce, out AItemFactory _, out AItemFactory beads);

        // 2 seeds x 3 turns: a seed each
        List<SimulationJob> withoutItem = jobs.FindAll(job => job.item == null);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, withoutItem.ConvertAll(job => job.seed));
        foreach (SimulationJob job in jobs.FindAll(job => job.item != null))
        {
            Assert.AreEqual(1, withoutItem.FindAll(reference => reference.seed == job.seed).Count);
        }
        // Each unit holds the item once per seed, with another seed each time
        List<SimulationJob> bounceOnGunner = jobs.FindAll(job => job.item == bounce && job.itemHolder == 1);
        CollectionAssert.AreEquivalent(new[] { 2, 5 }, bounceOnGunner.ConvertAll(job => job.seed));
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, jobs.FindAll(job => job.item == beads).ConvertAll(job => job.seed));
    }

    [Test]
    public void BuildForEachItem_WithoutTheFightsWithoutItem_OnlyTheItems()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { Create<EntityData>("Sniper"), Create<EntityData>("Gunner") } };
        AItemFactory bounce = CreateItem("BounceItem");
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave_Balance_Dummies") } };

        List<SimulationJob> jobs = SimulationJobBuilder.BuildForEachItem(Create<CharacterData>("Balance"), team, waves, 2, new[] { bounce }, new AItemFactory[0], null, false);

        // 2 seeds x 2 turns, each with the item only, on the same seeds as with the fights without item
        Assert.AreEqual(4, jobs.Count);
        Assert.IsTrue(jobs.TrueForAll(job => job.item == bounce));
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, jobs.ConvertAll(job => job.seed));
    }

    [Test]
    public void BuildForEachItem_NoCharacter_None()
    {
        List<SimulatedWave> waves = new List<SimulatedWave> { new SimulatedWave { wave = Create<WavePatternData>("Wave") } };

        Assert.IsEmpty(SimulationJobBuilder.BuildForEachItem(null, new ReferenceTeam(), waves, 1, new[] { CreateItem("BounceItem") }, new AItemFactory[0]));
    }
}

}
