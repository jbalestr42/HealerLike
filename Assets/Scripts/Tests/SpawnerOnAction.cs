using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

public class SpawnerOnAction : MonoBehaviour
{
    List<GameObject> entities = new List<GameObject>();

    [Button("Spawn")]
    public void Spawn()
    {
        LoadWave(DataManager.instance.GetWavePattern(MapNodeType.Combat, 0, new System.Random()));
    }

    public void LoadWave(WavePatternData waveData)
    {
        foreach (Entity entity in EntityManager.instance.SpawnWave(waveData, EntityManager.instance.board, Entity.EntityType.Computer))
        {
            entity.transform.parent = transform;
            entities.Add(entity.gameObject);
        }
    }

    [Button("Destroy")]
    public void Destroy()
    {
        foreach (GameObject entity in entities)
        {
            EntityManager.instance.DestroyEntity(entity, Entity.EntityType.Computer);
        }
        entities.Clear();
    }
}
