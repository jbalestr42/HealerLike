using System;
using UnityEngine;

[Serializable]
public class RankDataStep : AWaveStep
{
    public EnemyRank enemyRank;

    public override EnemyData GetEnemyData()
    {
        return DataManager.instance.GetRandomEnemyDataFromRank(enemyRank);
    }
}

[CreateAssetMenu(menuName = "Custom/Data/WaveRankData")]
public class WaveRankData : AWaveData<RankDataStep> { }