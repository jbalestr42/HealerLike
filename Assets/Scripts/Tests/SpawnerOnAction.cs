using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SpawnerOnAction : SerializedMonoBehaviour
{
    List<GameObject> entities = new List<GameObject>();

    [Button("Spawn")]
    public void Spawn()
    {
        LoadWave(DataManager.instance.GetRandomWavePattern());
    }

    public void LoadWave(WavePatternData waveData)
    {
        for (int i = 0; i < waveData.width; i++)
        {
            for (int j = 0; j < waveData.height; j++)
            {
                if (waveData.spawns[i, j] != null)
                {
                    GameObject entity = EntityManager.instance.SpawnEntity(waveData.spawns[i, j], transform.position - new Vector3(waveData.width / 2f, 0f, waveData.height / 2f) + new Vector3(i, 0f, j), Entity.EntityType.Computer);
                    if (entity != null)
                    {
                        entity.transform.parent = transform;
                        entities.Add(entity);
                    }
                }
            }
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
