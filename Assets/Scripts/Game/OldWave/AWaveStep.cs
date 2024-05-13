using System;

[Serializable]
public abstract class AWaveStep
{
    public int count = 1;
    public float spawnRate = 1f;
    public abstract EnemyData GetEnemyData();
}