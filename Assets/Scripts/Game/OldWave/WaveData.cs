using System;
using UnityEngine;

[Serializable]
public class DataStep : AWaveStep
{
    public EnemyData enemyData;

    public override EnemyData GetEnemyData()
    {
        return enemyData;
    }
}

[CreateAssetMenu(menuName = "Custom/Data/WaveData")]
public class WaveData : AWaveData<DataStep> { }