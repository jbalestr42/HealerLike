using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EntityManager : Singleton<EntityManager>
{
    [HideInInspector] public UnityEvent<Enemy> OnEnemySpawned = new UnityEvent<Enemy>();
    [HideInInspector] public UnityEvent<Enemy, bool> OnEnemyKilled = new UnityEvent<Enemy, bool>();

    [SerializeField]
    GameObject _towerBasePrefab;

    [SerializeField]
    GameObject _enemyBasePrefab;

    [SerializeField]
    GameObject _entityBasePrefab;

    List<GameObject> _enemies;
    public List<GameObject> enemies => _enemies;

    List<GameObject> _towers;
    public List<GameObject> towers => _towers;

    Dictionary<Entity.EntityType, List<GameObject>> _entities;
    public Dictionary<Entity.EntityType, List<GameObject>> entities => _entities;

    void Awake()
    {
        _enemies = new List<GameObject>();
        _towers = new List<GameObject>();
        _entities = new Dictionary<Entity.EntityType, List<GameObject>>();
    }

    #region Enemies

    public GameObject SpawnEnemy(EnemyData data, Vector3 position)
    {
        GameObject enemy = Instantiate(_enemyBasePrefab, position, Quaternion.identity);
        enemy.GetComponent<Enemy>().data = data;
        enemy.GetComponent<Enemy>().Init();
        _enemies.Add(enemy);
        return enemy;
    }

    public void DestroyEnemy(GameObject enemy, bool hasReachedEnd)
    {
        OnEnemyKilled.Invoke(enemy.GetComponent<Enemy>(), hasReachedEnd);
        _enemies.Remove(enemy);
        Destroy(enemy);
    }

    public bool AreAllEnemyDead()
    {
        return _enemies.Count == 0;
    }

    #endregion

    #region Towers

    public GameObject SpawnTower(TowerData data, Vector3 position)
    {
        PlayerBehaviour player = PlayerBehaviour.instance;
        Vector2Int coord = player.grid.GetCoordFromPosition(position);

        GameObject tower = null;
        if (player.grid.IsWalkable(coord.x, coord.y) && player.HasEnoughGold(data.price))
        {
            tower = Instantiate(_towerBasePrefab, Vector3.zero, Quaternion.identity);
            tower.GetComponent<Tower>().data = data;
            tower.transform.position = position;

            if (player.grid.CanPlaceObject(coord))
            {
                player.grid.SetWalkable(coord.x, coord.y, false);
                player.gold -= data.price;
                _towers.Add(tower);
            }
            else
            {
                GameObject.Destroy(tower);
            }
        }
        return tower;
    }

    public void DestroyTower(GameObject tower)
    {
        _towers.Remove(tower);
        Destroy(tower);
    }

    #endregion

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

    #endregion

}
