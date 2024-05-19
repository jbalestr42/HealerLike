using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EntityManager : Singleton<EntityManager>
{
    [HideInInspector] public UnityEvent<Entity> OnEntitySpawned = new UnityEvent<Entity>();
    [HideInInspector] public UnityEvent<Entity> OnEntityKilled = new UnityEvent<Entity>();

    [SerializeField]
    GameObject _entityBasePrefab;

    Dictionary<Entity.EntityType, List<GameObject>> _entities;
    public Dictionary<Entity.EntityType, List<GameObject>> entities => _entities;

    void Awake()
    {
        _entities = new Dictionary<Entity.EntityType, List<GameObject>>();
    }

    #region Entities

    public List<GameObject> GetEntities(Entity.EntityType entityType)
    {
        if (!_entities.ContainsKey(entityType))
        {
            _entities[entityType] = new List<GameObject>();
        }
        return _entities[entityType];
    }

    public GameObject SpawnEntity(EntityData data, Vector3 position, Entity.EntityType entityType)
    {
        PlayerBehaviour player = PlayerBehaviour.instance;
        Vector2Int coord = player.grid.GetCoordFromPosition(position);

        if (player.grid.IsWalkable(coord.x, coord.y))
        {
            player.grid.SetWalkable(coord.x, coord.y, false);
            GameObject entity = Instantiate(_entityBasePrefab, Vector3.zero, Quaternion.identity);
            entity.GetComponent<Entity>().data = data;
            entity.GetComponent<Entity>().entityType = entityType;
            entity.GetComponent<Entity>().Init();
            entity.transform.position = position;
            if (!_entities.ContainsKey(entityType))
            {
                _entities[entityType] = new List<GameObject>();
            }
            _entities[entityType].Add(entity);
            
            return entity;
        }
        return null;
    }

    public void DestroyEntity(GameObject entity, Entity.EntityType entityType)
    {
        if (entity != null)
        {
            PlayerBehaviour player = PlayerBehaviour.instance;
            Vector2Int coord = player.grid.GetCoordFromPosition(entity.transform.position);
            player.grid.SetWalkable(coord.x, coord.y, true);
            _entities[entityType].Remove(entity);
            Destroy(entity);
        }
    }

    public bool AreAllEntityDead(Entity.EntityType entityType)
    {
        return GetEntities(entityType).Count == 0;
    }

    #endregion

}
