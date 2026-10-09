using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

public class SpawnerOnAction : Sirenix.OdinInspector.SerializedMonoBehaviour
{
    List<GameObject> entities = new List<GameObject>();

    [Button("Spawn")]
    public void Spawn()
    {
        LoadWave(DataManager.instance.GetWavePattern(MapNodeType.Combat, 0, new System.Random()));
    }

    public void LoadWave(WavePatternData waveData)
    {
        foreach ((int x, int y, EntityData data) unit in waveData.GetUnits())
        {
            GameObject entity = EntityManager.instance.SpawnEntity(unit.data, waveData.GetSlotPosition(transform.position, unit.x, unit.y), Entity.EntityType.Computer);
            if (entity != null)
            {
                entity.transform.parent = transform;
                entities.Add(entity);
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
