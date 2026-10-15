using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Game
{

// The wave pools of the game data against the map: every floor of the map has its waves, every wave can be met
// in a run, and the new waves of the last floors hold the enemies they were made with. The waves are being rebuilt
// (Cleric rework): two different combat waves on each floor, each one on two floors at most, a new one coming in on
// each floor; the old waves set aside stay in the data, out of the pools
public class WavePoolsDataTests
{
    const string GameDataPath = "Assets/Data/TestData.asset";
    const string MapSettingsPath = "Assets/Data/Run/MapGenerationSettings.asset";
    const string WavesFolder = "Assets/Data/WavePatterns/";
    // The floors whose combat waves are rebuilt so far (0 to this one), the next ones still fighting the old waves
    const int RebuiltUpToFloor = 9;
    // The first floors, for the 2 units the Cleric starts with: small waves
    const int SmallWavesUpToFloor = 4;
    // The floors after them, once the recruits have joined: four enemies
    const int FourEnemiesUpToFloor = 9;
    // Old waves kept in the data for later, out of the pools
    static readonly string[] SetAsideWaves =
    {
        "Wave_FrontLine", "Wave_Fuse", "Wave_HiddenHealer", "Wave_FireRain", "Wave_Bursts", "Wave_HiddenSniper", "Wave_Charnel",
        "Wave_MarkAndShoot", "Wave_Storm", "Wave_Graveyard", "Wave_Winter", "Wave_Plague", "Wave_Execution", "Wave_Crypt",
    };

    GameData _data;
    MapGenerationSettings _map;

    [SetUp]
    public void SetUp()
    {
        _data = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
        _map = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(MapSettingsPath);
        Assert.IsNotNull(_data);
        Assert.IsNotNull(_map);
    }

    List<string> GetWaves(MapNodeType roomType, int floor)
    {
        return _data.wavePools
            .Where(pool => pool.roomType == roomType && floor >= pool.minFloor && floor <= pool.maxFloor)
            .SelectMany(pool => pool.wavePatterns)
            .Where(wave => wave != null)
            .Select(wave => wave.name)
            .ToList();
    }

    static Dictionary<string, int> CountEnemies(string waveName)
    {
        WavePatternData wave = AssetDatabase.LoadAssetAtPath<WavePatternData>(WavesFolder + waveName + ".asset");
        Assert.IsNotNull(wave, waveName);
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach ((int x, int y, EntityData entity) unit in wave.GetUnits())
        {
            counts.TryGetValue(unit.entity.title, out int count);
            counts[unit.entity.title] = count + 1;
        }
        return counts;
    }

    [Test]
    public void TheMap_Has15Floors_AndTheRestRightBeforeTheBoss()
    {
        Assert.AreEqual(15, _map.floorCount);
        CollectionAssert.AreEqual(new[] { 14 }, _map.GetRoomType(MapNodeType.Rest).fixedFloors);
    }

    [Test]
    public void EveryFloor_HasAtLeastTwoCombatWaves()
    {
        for (int floor = 0; floor < _map.floorCount; floor++)
        {
            Assert.GreaterOrEqual(GetWaves(MapNodeType.Combat, floor).Count, 2, $"floor {floor}");
        }
    }

    [Test]
    public void EveryFloorWithElites_HasItsOwnEliteWaves()
    {
        for (int floor = _map.GetRoomType(MapNodeType.Elite).firstFloor; floor < _map.floorCount; floor++)
        {
            Assert.IsNotEmpty(GetWaves(MapNodeType.Elite, floor), $"floor {floor}");
        }
    }

    [Test]
    public void TheBoss_IsMarkerTitan()
    {
        CollectionAssert.AreEqual(new[] { "Wave_Boss_MarkerTitan" }, GetWaves(MapNodeType.Boss, _map.floorCount));
    }

    [Test]
    public void EveryWaveOfThePools_CanBeMetOnAFloorOfTheMap()
    {
        foreach (GameData.WavePool pool in _data.wavePools.Where(pool => pool.roomType != MapNodeType.Boss))
        {
            Assert.Less(pool.minFloor, _map.floorCount, string.Join(", ", pool.wavePatterns.Select(wave => wave.name)));
            Assert.LessOrEqual(pool.minFloor, pool.maxFloor);
        }
    }

    [Test]
    public void EveryWaveOfTheProject_IsInAPool()
    {
        HashSet<string> pooled = new HashSet<string>(_data.wavePools.SelectMany(pool => pool.wavePatterns).Where(wave => wave != null).Select(wave => wave.name));
        IEnumerable<string> waves = AssetDatabase.FindAssets("t:WavePatternData", new[] { "Assets/Data/WavePatterns" })
            .Select(guid => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));

        CollectionAssert.IsSubsetOf(waves.Except(SetAsideWaves), pooled);
    }

    [Test]
    public void TheWavesSetAside_AreInNoPool()
    {
        HashSet<string> pooled = new HashSet<string>(_data.wavePools.SelectMany(pool => pool.wavePatterns).Where(wave => wave != null).Select(wave => wave.name));

        foreach (string wave in SetAsideWaves)
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<WavePatternData>(WavesFolder + wave + ".asset"), $"{wave} stays in the data");
            Assert.IsFalse(pooled.Contains(wave), wave);
        }
    }

    [Test]
    public void EachRebuiltFloor_TwoDifferentCombatWaves()
    {
        for (int floor = 0; floor <= RebuiltUpToFloor; floor++)
        {
            List<string> waves = GetWaves(MapNodeType.Combat, floor);
            Assert.AreEqual(2, waves.Count, $"floor {floor}");
            Assert.AreNotEqual(waves[0], waves[1], $"floor {floor}");
        }
    }

    [Test]
    public void EachRebuiltWave_OnTwoFloorsAtMost()
    {
        foreach (GameData.WavePool pool in _data.wavePools.Where(pool => pool.roomType == MapNodeType.Combat && pool.minFloor <= RebuiltUpToFloor))
        {
            Assert.LessOrEqual(pool.maxFloor - pool.minFloor + 1, 2, string.Join(", ", pool.wavePatterns.Select(wave => wave.name)));
        }
    }

    // Each floor brings a wave in: the waves of a floor don't all come from the floor before
    [Test]
    public void EachRebuiltFloor_ANewWave()
    {
        for (int floor = 1; floor <= RebuiltUpToFloor; floor++)
        {
            List<string> before = GetWaves(MapNodeType.Combat, floor - 1);
            Assert.IsTrue(GetWaves(MapNodeType.Combat, floor).Exists(wave => !before.Contains(wave)), $"floor {floor}");
        }
    }

    [Test]
    public void FirstFloors_TwoOrThreeEnemies()
    {
        for (int floor = 0; floor <= SmallWavesUpToFloor; floor++)
        {
            foreach (string wave in GetWaves(MapNodeType.Combat, floor))
            {
                Assert.That(CountEnemies(wave).Values.Sum(), Is.InRange(2, 3), wave);
            }
        }
    }

    // The waves coming in on these floors (Hunter, from the floor before, stays small)
    [Test]
    public void MiddleFloors_FourEnemies()
    {
        foreach (GameData.WavePool pool in _data.wavePools.Where(pool => pool.roomType == MapNodeType.Combat && pool.minFloor > SmallWavesUpToFloor && pool.minFloor <= FourEnemiesUpToFloor))
        {
            foreach (WavePatternData wave in pool.wavePatterns)
            {
                Assert.AreEqual(4, CountEnemies(wave.name).Values.Sum(), wave.name);
            }
        }
    }

    // The healing counters (Hexer, Plague Bearer) never come in the first floors
    [Test]
    public void TheHealingCounters_ComeInTheMiddleOfTheRun()
    {
        for (int floor = 0; floor <= SmallWavesUpToFloor; floor++)
        {
            foreach (string wave in GetWaves(MapNodeType.Combat, floor))
            {
                Dictionary<string, int> enemies = CountEnemies(wave);
                Assert.IsFalse(enemies.ContainsKey("Hexer") || enemies.ContainsKey("Plague Bearer"), wave);
            }
        }
        CollectionAssert.Contains(GetWaves(MapNodeType.Combat, 5), "Wave_HexedGuard");
    }

    [Test]
    public void HexedGuard_FootmanSoldierHexerArcher()
    {
        AssertEnemies("Wave_HexedGuard", ("Armored Footman", 1), ("Soldier", 1), ("Hexer", 1), ("Archer", 1));
    }

    [Test]
    public void Duel_DuelistFootmanArcherWarDrum()
    {
        AssertEnemies("Wave_Duel", ("Duelist", 1), ("Armored Footman", 1), ("Archer", 1), ("War Drum", 1));
    }

    [Test]
    public void Shrine_TwoSoldiersShamanMortar()
    {
        AssertEnemies("Wave_Shrine", ("Soldier", 2), ("Shaman", 1), ("Mortar", 1));
    }

    [Test]
    public void Frostbite_TwoFootmenFrostCasterArcher()
    {
        AssertEnemies("Wave_Frostbite", ("Armored Footman", 2), ("Frost Caster", 1), ("Archer", 1));
    }

    [Test]
    public void ExecutionSquad_FootmanSoldierHexerHeadsman()
    {
        AssertEnemies("Wave_ExecutionSquad", ("Armored Footman", 1), ("Soldier", 1), ("Hexer", 1), ("Headsman", 1));
    }

    // Set aside for now (user, 2026-10-15): no healing halved by the Plague Bearer, no kamikaze in the rebuilt waves
    [Test]
    public void TheRebuiltWaves_NoPlagueBearerNorKamikaze()
    {
        for (int floor = 0; floor <= RebuiltUpToFloor; floor++)
        {
            foreach (string wave in GetWaves(MapNodeType.Combat, floor))
            {
                Dictionary<string, int> enemies = CountEnemies(wave);
                Assert.IsFalse(enemies.ContainsKey("Plague Bearer") || enemies.ContainsKey("Kamikaze") || enemies.ContainsKey("Lit Kamikaze"), wave);
            }
        }
    }

    static void AssertEnemies(string waveName, params (string title, int count)[] expected)
    {
        Dictionary<string, int> enemies = CountEnemies(waveName);
        CollectionAssert.AreEquivalent(expected.ToDictionary(enemy => enemy.title, enemy => enemy.count), enemies, waveName);
    }

    [Test]
    public void TheEasiestWaves_StartTheRun_AndTheHardestEndIt()
    {
        CollectionAssert.Contains(GetWaves(MapNodeType.Combat, 0), "Wave_Recruits");
        CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, 0), "Wave_Bulwark");
        CollectionAssert.Contains(GetWaves(MapNodeType.Combat, 14), "Wave_Ironclad");
        CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, 14), "Wave_Recruits");
        CollectionAssert.Contains(GetWaves(MapNodeType.Elite, 13), "Wave_TotalWar");
        CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, 13), "Wave_TotalWar");
    }

    // Its threat is the one of Living Wall and Bulwark, far above the waves starting the run: met on their floors
    [Test]
    public void SlowPoison_IsMetWithLivingWallAndBulwark_NeverAtTheStart()
    {
        for (int floor = 0; floor < 10; floor++)
        {
            CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, floor), "Wave_SlowPoison", $"floor {floor}");
        }
        for (int floor = 10; floor <= 12; floor++)
        {
            CollectionAssert.Contains(GetWaves(MapNodeType.Combat, floor), "Wave_SlowPoison", $"floor {floor}");
        }
    }

    [Test]
    public void Ironclad_IsALivingWallWithASecondColossus()
    {
        Dictionary<string, int> enemies = CountEnemies("Wave_Ironclad");

        Assert.AreEqual(2, enemies["Colossus"]);
        Assert.AreEqual(1, enemies["Sniper"]);
        Assert.AreEqual(1, enemies["Archer"]);
        Assert.AreEqual(1, enemies["Shaman"]);
        Assert.AreEqual(5, enemies.Values.Sum());
    }

    [Test]
    public void Siege_IsABulwarkWithAMortar()
    {
        Dictionary<string, int> enemies = CountEnemies("Wave_Siege");

        Assert.AreEqual(1, enemies["Colossus"]);
        Assert.AreEqual(1, enemies["Sniper"]);
        Assert.AreEqual(1, enemies["Archer"]);
        Assert.AreEqual(1, enemies["Guardian"]);
        Assert.AreEqual(1, enemies["Mortar"]);
        Assert.AreEqual(5, enemies.Values.Sum());
    }
}

}
