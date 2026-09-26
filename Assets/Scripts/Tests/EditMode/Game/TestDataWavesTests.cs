using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the project data used by the Main scene: every room the map can generate must find a wave
public class TestDataWavesTests
{
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
        bool isRestFloor = _settings.restBeforeBoss && floor == _settings.floorCount - 1;
        return floor != 0 && (isRestFloor || floor == _settings.treasureFloor);
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
        for (int floor = _settings.firstEliteFloor; floor < _settings.floorCount; floor++)
        {
            if (floor != 0 && !IsNonFightFixedFloor(floor) && _dataManager.GetWavePatterns(MapNodeType.Elite, floor).Count == 0)
            {
                missing.Add(floor);
            }
        }

        CollectionAssert.IsEmpty(missing, "Floors without elite waves");
    }
}

}
