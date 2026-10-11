using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class EntityManager : Singleton<EntityManager>
{
    // Tag given to the entities summoned during a battle

    [HideInInspector] public UnityEvent<Entity> OnEntitySpawned = new UnityEvent<Entity>();
    [HideInInspector] public UnityEvent<Entity> OnEntityKilled = new UnityEvent<Entity>();
    // An entity summoned during a battle, once it's tagged as a summon
    [HideInInspector] public UnityEvent<Entity> OnEntitySummoned = new UnityEvent<Entity>();
    [HideInInspector] public UnityEvent<GameObject> OnProjectileSpawned = new UnityEvent<GameObject>();

    [SerializeField]
    GameObject _entityBasePrefab;

    // The board of the fights, the units of a wave standing on its cells
    [SerializeField] Board _board;
    public Board board { get { return _board; } set { _board = value; } }

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

    // Spawns every entity of the pattern around center, returns the ones actually spawned
    public List<Entity> SpawnWave(WavePatternData waveData, Vector3 center, Entity.EntityType entityType)
    {
        List<Entity> spawned = new List<Entity>();
        foreach ((int x, int y, EntityData data) unit in waveData.GetUnits())
        {
            GameObject entity = SpawnEntity(unit.data, waveData.GetSlotPosition(center, unit.x, unit.y), entityType);
            if (entity != null)
            {
                spawned.Add(entity.GetComponent<Entity>());
            }
        }
        return spawned;
    }

    // Spawns every entity of the pattern on its cell of the board, returns the ones actually spawned: a cell not free
    // for the side of the units is skipped
    public List<Entity> SpawnWave(WavePatternData waveData, Board targetBoard, Entity.EntityType entityType)
    {
        List<Entity> spawned = new List<Entity>();
        BoardCellType side = BoardLayout.GetCellType(entityType);
        foreach ((int x, int y, EntityData data) unit in waveData.GetUnits())
        {
            Vector2Int coord = new Vector2Int(unit.x, unit.y);
            if (!targetBoard.IsFree(coord, side))
            {
                continue;
            }
            GameObject entity = SpawnEntity(unit.data, targetBoard.GetUnitPosition(coord), entityType);
            if (entity != null)
            {
                targetBoard.Occupy(coord, entity, side);
                spawned.Add(entity.GetComponent<Entity>());
            }
        }
        return spawned;
    }

    public void DestroyEntity(GameObject entity, Entity.EntityType entityType)
    {
        if (entity != null)
        {
            OnEntityKilled.Invoke(entity.GetComponent<Entity>());
            RemoveEntity(entity, entityType);
        }
    }

    // Tags the entity as a summon, removed at the end of the battle
    public void AddSummon(Entity summon)
    {
        summon.AddTag(DataManager.instance.GetTagWithName(TagNames.Summon));
        OnEntitySummoned.Invoke(summon);
    }

    // Summons only last for the battle they were summoned in: removed without being killed
    public void RemoveSummons()
    {
        GameplayTag summonTag = DataManager.instance.GetTagWithName(TagNames.Summon);
        foreach (Entity.EntityType entityType in Enum.GetValues(typeof(Entity.EntityType)))
        {
            foreach (GameObject summon in FindSummons(GetEntities(entityType), summonTag))
            {
                RemoveEntity(summon, entityType);
            }
        }
    }

    public static List<GameObject> FindSummons(List<GameObject> entities, GameplayTag summonTag)
    {
        return entities.FindAll(entity => entity != null && entity.GetComponent<Entity>().HasTag(summonTag));
    }

    void RemoveEntity(GameObject entity, Entity.EntityType entityType)
    {
        PlayerBehaviour player = PlayerBehaviour.instance;
        Vector2Int coord = player.grid.GetCoordFromPosition(entity.transform.position);
        player.grid.SetWalkable(coord.x, coord.y, true);
        if (_board != null)
        {
            _board.Release(entity);
        }
        _entities[entityType].Remove(entity);
        Destroy(entity);
    }

    public bool AreAllEntityDead(Entity.EntityType entityType)
    {
        return GetEntities(entityType).Count == 0;
    }

    public GameObject SpawnProjectile(GameObject projectilePrefab, Vector3 position, Quaternion rotation)
    {
        GameObject projectileGo = Instantiate(projectilePrefab, position, rotation, _parentProjectile.transform);
        OnProjectileSpawned.Invoke(projectileGo);
        return projectileGo;
    }

    #endregion

}
