using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Game
{

// The boss room at the top of the map: a real fight against the boss wave, beating it wins the run
public class BossRoomTests
{
    const string GameDataPath = "Assets/Data/TestData.asset";

    [Test]
    public void BeatingTheBoss_WinsTheRun()
    {
        Assert.IsTrue(AscensionGameType.IsRunWon(MapNodeType.Boss));
    }

    [TestCase(MapNodeType.Combat)]
    [TestCase(MapNodeType.Elite)]
    public void BeatingAnotherFight_DoesNotWinTheRun(MapNodeType roomType)
    {
        Assert.IsFalse(AscensionGameType.IsRunWon(roomType));
    }

    [Test]
    public void EndScreen_TellsAVictoryFromAGameOver()
    {
        Assert.AreEqual("Victory!", GameOverView.GetResultTitle(true));
        Assert.AreEqual("Game Over", GameOverView.GetResultTitle(false));
    }

    // Whatever the number of floors of the map: the boss room is on the floor right after the last one
    [TestCase(10)]
    [TestCase(15)]
    public void GameData_HasABossWaveForTheBossFloor(int bossFloor)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
        Assert.IsNotNull(data);

        List<WavePatternData> waves = new List<WavePatternData>();
        foreach (GameData.WavePool pool in data.wavePools)
        {
            if (pool.roomType == MapNodeType.Boss && bossFloor >= pool.minFloor && bossFloor <= pool.maxFloor)
            {
                waves.AddRange(pool.wavePatterns);
            }
        }

        Assert.IsNotEmpty(waves, "No boss wave");
    }

    [Test]
    public void BossWave_IsTheMarkerTitanAlone()
    {
        WavePatternData wave = AssetDatabase.LoadAssetAtPath<WavePatternData>("Assets/Data/WavePatterns/Wave_Boss_MarkerTitan.asset");
        Assert.IsNotNull(wave);

        List<string> titles = new List<string>();
        foreach (EntitySlot slot in wave.slots)
        {
            if (slot.entity != null)
            {
                titles.Add(slot.entity.title);
            }
        }

        CollectionAssert.AreEqual(new[] { "Marker Titan" }, titles);
    }
}

}
