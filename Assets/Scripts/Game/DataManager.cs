using System.Collections.Generic;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    [SerializeField] GameData _data;
    public GameData data { get { return _data; } set { _data = value; } }

    public List<AWaveData> waves { get { return _data.waves; } set { _data.waves = value; } }
    public List<WavePatternData> wavePatterns { get { return _data.wavePatterns; } set { _data.wavePatterns = value; } }
    public List<TowerData> towers { get { return _data.towers; } set { _data.towers = value; } } 
    public List<EntityData> entities { get { return _data.entities; } set { _data.entities = value; } } 
    public List<AItemFactory> items { get { return _data.items; } set { _data.items = value; } } 
    public List<AConsumableFactory> consumables { get { return _data.consumables; } set { _data.consumables = value; } } 

    public EnemyData GetRandomEnemyDataFromRank(EnemyRank enemyRank)
    {
        List<EnemyData> enemyDataByRank = _data.enemies.FindAll(data => data.rank == enemyRank);
        return enemyDataByRank[Random.Range(0, enemyDataByRank.Count)];
    }

    public AItem GetRandomItem()
    {
        return _data.items[Random.Range(0, _data.items.Count)].GetItem();
    }

    public AConsumable GetRandomConsumable()
    {
        return _data.consumables[Random.Range(0, _data.consumables.Count)].GetConsumable();
    }

    public AWaveData GetRandomWave(int minimumWaveApparition)
    {
        List<AWaveData> filteredWaveData = _data.waves.FindAll(data => data.minimumWaveApparition <= minimumWaveApparition);
        return filteredWaveData[Random.Range(0, filteredWaveData.Count)];
    }

    public WavePatternData GetRandomWavePattern()
    {
        return _data.wavePatterns[Random.Range(0, _data.wavePatterns.Count)];
    }
}