using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Game
{

// The wave pools of the game data against the map: every floor of the map has its waves, every wave can be met
// in a run, and the new waves of the last floors hold the enemies they were made with
public class WavePoolsDataTests
{
    const string GameDataPath = "Assets/Data/TestData.asset";
    const string MapSettingsPath = "Assets/Data/Run/MapGenerationSettings.asset";
    const string WavesFolder = "Assets/Data/WavePatterns/";

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
        foreach (EntitySlot slot in wave.slots)
        {
            if (slot.entity != null)
            {
                counts.TryGetValue(slot.entity.title, out int count);
                counts[slot.entity.title] = count + 1;
            }
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

        CollectionAssert.IsSubsetOf(waves, pooled);
    }

    [Test]
    public void TheEasiestWaves_StartTheRun_AndTheHardestEndIt()
    {
        CollectionAssert.Contains(GetWaves(MapNodeType.Combat, 0), "Wave_FrontLine");
        CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, 0), "Wave_Bulwark");
        CollectionAssert.Contains(GetWaves(MapNodeType.Combat, 14), "Wave_Ironclad");
        CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, 14), "Wave_FrontLine");
        CollectionAssert.Contains(GetWaves(MapNodeType.Elite, 13), "Wave_TotalWar");
        CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, 13), "Wave_TotalWar");
    }

    // Its threat is the one of Living Wall and Bulwark, far above the waves starting the run: met on their floors
    [Test]
    public void SlowPoison_IsMetWithLivingWallAndBulwark_NeverAtTheStart()
    {
        for (int floor = 0; floor < 8; floor++)
        {
            CollectionAssert.DoesNotContain(GetWaves(MapNodeType.Combat, floor), "Wave_SlowPoison", $"floor {floor}");
        }
        for (int floor = 8; floor <= 11; floor++)
        {
            CollectionAssert.Contains(GetWaves(MapNodeType.Combat, floor), "Wave_SlowPoison", $"floor {floor}");
        }
    }

    [Test]
    public void Ironclad_IsALivingWallWithASecondColossus()
    {
        Dictionary<string, int> enemies = CountEnemies("Wave_Ironclad");

        Assert.AreEqual(2, enemies["Colossus"]);
        Assert.AreEqual(2, enemies["Sniper"]);
        Assert.AreEqual(1, enemies["Shaman"]);
        Assert.AreEqual(5, enemies.Values.Sum());
    }

    [Test]
    public void Siege_IsABulwarkWithAMortarAndAnArcMage()
    {
        Dictionary<string, int> enemies = CountEnemies("Wave_Siege");

        Assert.AreEqual(1, enemies["Colossus"]);
        Assert.AreEqual(2, enemies["Sniper"]);
        Assert.AreEqual(1, enemies["Guardian"]);
        Assert.AreEqual(1, enemies["Mortar"]);
        Assert.AreEqual(1, enemies["Arc Mage"]);
        Assert.AreEqual(6, enemies.Values.Sum());
    }
}

}
