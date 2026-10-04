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
}

}
