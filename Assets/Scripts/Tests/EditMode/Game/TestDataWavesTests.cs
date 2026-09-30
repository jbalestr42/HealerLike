using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the project data used by the Main scene: every room the map can generate must find a wave
public class TestDataWavesTests
{
    // The 15 floors of the Slay the Spire map: the first enemies up to the treasure floor (8), the others
    // on the floors after it, up to the one before the rests (14)
    const int LastEarlyFloor = 8;
    const int LastFightFloor = 13;

    const string GameDataPath = "Assets/Data/TestData.asset";
    const string MapSettingsPath = "Assets/Data/Run/MapGenerationSettings.asset";

    GameObject _go;
    DataManager _dataManager;
    MapGenerationSettings _settings;

    [SetUp]
    public void SetUp()
    {
        // Standalone component, never going through DataManager.instance
        _go = new GameObject("DataManager");
        _dataManager = _go.AddComponent<DataManager>();
        _dataManager.data = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
        _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(MapSettingsPath);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    // Floors whose rooms are all of a single type, which is not a fight except for the first floor
    bool IsNonFightFixedFloor(int floor)
    {
        MapNodeType? fixedType = _settings.GetFixedType(floor);
        return fixedType.HasValue && fixedType.Value != MapNodeType.Combat && fixedType.Value != MapNodeType.Elite;
    }

    [Test]
    public void Assets_AreFound()
    {
        Assert.IsNotNull(_dataManager.data, GameDataPath);
        Assert.IsNotNull(_settings, MapSettingsPath);
    }

    [Test]
    public void EveryFloorWithCombats_HasCombatWaves()
    {
        List<int> missing = new List<int>();
        for (int floor = 0; floor < _settings.floorCount; floor++)
        {
            if (!IsNonFightFixedFloor(floor) && _dataManager.GetWavePatterns(MapNodeType.Combat, floor).Count == 0)
            {
                missing.Add(floor);
            }
        }

        CollectionAssert.IsEmpty(missing, "Floors without combat waves");
    }

    [Test]
    public void EveryFloorWithElites_HasItsOwnEliteWaves()
    {
        List<int> missing = new List<int>();
        for (int floor = _settings.GetRoomType(MapNodeType.Elite).firstFloor; floor < _settings.floorCount; floor++)
        {
            if (floor != 0 && !IsNonFightFixedFloor(floor) && _dataManager.GetWavePatterns(MapNodeType.Elite, floor).Count == 0)
            {
                missing.Add(floor);
            }
        }

        CollectionAssert.IsEmpty(missing, "Floors without elite waves");
    }

    [Test]
    public void EveryPooledWave_HasSlotsMatchingItsSize_AndAtLeastOneEntity()
    {
        List<string> invalid = new List<string>();
        foreach (GameData.WavePool pool in _dataManager.data.wavePools)
        {
            foreach (WavePatternData wave in pool.wavePatterns)
            {
                if (wave == null)
                {
                    invalid.Add($"null wave (floors {pool.minFloor}-{pool.maxFloor})");
                    continue;
                }

                bool sizeMatches = wave.slots != null && wave.slots.GetLength(0) == wave.width && wave.slots.GetLength(1) == wave.height;
                if (!sizeMatches || GetEntities(wave).Count == 0)
                {
                    invalid.Add(wave.name);
                }
            }
        }

        CollectionAssert.IsEmpty(invalid, "Waves with a wrong size or without any entity");
    }

    // The first floors must be varied: at least two combat waves to pick from
    [Test]
    public void FirstFloors_OfferSeveralCombatWaves()
    {
        List<int> monotonous = new List<int>();
        for (int floor = 0; floor <= LastEarlyFloor; floor++)
        {
            if (!IsNonFightFixedFloor(floor) && _dataManager.GetWavePatterns(MapNodeType.Combat, floor).Count < 2)
            {
                monotonous.Add(floor);
            }
        }

        CollectionAssert.IsEmpty(monotonous, "Floors with less than two combat waves");
    }

    [Test]
    public void FirstFloors_IntroduceTheFirstNewEnemies()
    {
        HashSet<EntityData> met = new HashSet<EntityData>();
        for (int floor = 0; floor <= LastEarlyFloor; floor++)
        {
            foreach (WavePatternData wave in _dataManager.GetWavePatterns(MapNodeType.Combat, floor))
            {
                met.UnionWith(GetEntities(wave));
            }
        }

        string[] expected =
        {
            "SniperEntity", "PoisonerEntity", "MortarEntity", "ShamanEntity", "HexerEntity",
            "KamikazeEntity", "PlagueBearerEntity",
        };
        foreach (string entityName in expected)
        {
            EntityData entity = AssetDatabase.LoadAssetAtPath<EntityData>($"Assets/Data/Entities/{entityName}/{entityName}.asset");
            Assert.IsNotNull(entity, entityName);
            Assert.IsTrue(met.Contains(entity), $"{entityName} is never met on floors 0-{LastEarlyFloor}");
        }
    }

    // The last floors must be varied too
    [Test]
    public void LastFloors_OfferSeveralCombatWaves()
    {
        List<int> monotonous = new List<int>();
        for (int floor = LastEarlyFloor + 1; floor <= LastFightFloor; floor++)
        {
            if (!IsNonFightFixedFloor(floor) && _dataManager.GetWavePatterns(MapNodeType.Combat, floor).Count < 2)
            {
                monotonous.Add(floor);
            }
        }

        CollectionAssert.IsEmpty(monotonous, "Floors with less than two combat waves");
    }

    [Test]
    public void LastFloors_IntroduceTheOtherNewEnemies()
    {
        HashSet<EntityData> met = new HashSet<EntityData>();
        for (int floor = LastEarlyFloor + 1; floor <= LastFightFloor; floor++)
        {
            foreach (WavePatternData wave in _dataManager.GetWavePatterns(MapNodeType.Combat, floor))
            {
                met.UnionWith(GetEntities(wave));
            }
        }

        string[] expected =
        {
            "MachineGunnerEntity", "ArcMageEntity", "WarDrumEntity", "FrostCasterEntity", "ColossusEntity", "BerserkerEntity", "ScavengerEntity",
            "NecromancerEntity", "SiphonerEntity", "GuardianEntity",
        };
        foreach (string entityName in expected)
        {
            EntityData entity = LoadEntity(entityName);
            Assert.IsNotNull(entity, entityName);
            Assert.IsTrue(met.Contains(entity), $"{entityName} is never met on floors {LastEarlyFloor + 1}-{LastFightFloor}");
        }
    }

    [Test]
    public void FirstElite_IsTheBastionHeldByTheColossus()
    {
        List<WavePatternData> elites = _dataManager.GetWavePatterns(MapNodeType.Elite, _settings.GetRoomType(MapNodeType.Elite).firstFloor);

        Assert.AreEqual(1, elites.Count);
        Assert.AreEqual("Wave_Elite_Bastion", elites[0].name);
        CollectionAssert.Contains(GetEntities(elites[0]), LoadEntity("ColossusEntity"));
    }

    [Test]
    public void LastFloorsElites_AreTheCultAndTheArtillery()
    {
        for (int floor = LastEarlyFloor + 1; floor <= LastFightFloor; floor++)
        {
            List<string> names = _dataManager.GetWavePatterns(MapNodeType.Elite, floor).ConvertAll(wave => wave.name);

            CollectionAssert.AreEquivalent(new[] { "Wave_Elite_Cult", "Wave_Elite_Artillery" }, names, $"Floor {floor}");
        }
    }

    static EntityData LoadEntity(string entityName)
    {
        return AssetDatabase.LoadAssetAtPath<EntityData>($"Assets/Data/Entities/{entityName}/{entityName}.asset");
    }

    static List<EntityData> GetEntities(WavePatternData wave)
    {
        List<EntityData> entities = new List<EntityData>();
        foreach (EntitySlot slot in wave.slots)
        {
            if (slot.entity != null)
            {
                entities.Add(slot.entity);
            }
        }
        return entities;
    }
}

}
