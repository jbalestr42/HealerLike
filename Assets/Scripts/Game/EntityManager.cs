using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class EntityManager : Singleton<EntityManager>
{
    [HideInInspector] public UnityEvent<Entity> OnEntitySpawned = new UnityEvent<Entity>();
    [HideInInspector] public UnityEvent<Entity> OnEntityKilled = new UnityEvent<Entity>();

    [SerializeField]
    GameObject _entityBasePrefab;

    Dictionary<Entity.EntityType, List<GameObject>> _entities;
    public Dictionary<Entity.EntityType, List<GameObject>> entities => _entities;

    Dictionary<Entity.EntityType, GameObject> _parentEntities = new Dictionary<Entity.EntityType, GameObject>();
    GameObject _parentProjectile;

    void Awake()
    {
        _parentProjectile = new GameObject("Projectiles");
        _parentProjectile.transform.SetParent(transform);

        GameObject parentEntities = new GameObject("Entities");
        parentEntities.transform.SetParent(transform);

        _entities = new Dictionary<Entity.EntityType, List<GameObject>>();
        foreach (Entity.EntityType entityType in Enum.GetValues(typeof(Entity.EntityType)))
        {
            GameObject parentEntity = new GameObject();
            parentEntity.name = $"{entityType.ToString()} Entities";
            parentEntity.transform.SetParent(parentEntities.transform);
            _parentEntities[entityType] = parentEntity;
            _entities[entityType] = new List<GameObject>();
        }
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
            GameObject entity = Instantiate(_entityBasePrefab, Vector3.zero, Quaternion.identity, _parentEntities[entityType].transform);
            entity.GetComponent<Entity>().data = data;
            entity.GetComponent<Entity>().entityType = entityType;
            entity.GetComponent<Entity>().Init();
            entity.name = $"{data.title} (Entity)";
            entity.transform.position = position;
            _entities[entityType].Add(entity);

            return entity;
        }
        return null;
    }

    public void DestroyEntity(GameObject entity, Entity.EntityType entityType)
    {
        if (entity != null)
        {
            OnEntityKilled.Invoke(entity.GetComponent<Entity>());
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

    public GameObject SpawnProjectile(GameObject projectilePrefab, Vector3 position, Quaternion rotation)
    {
        return Instantiate(projectilePrefab, position, rotation, _parentProjectile.transform);
    }

    #endregion

}
