using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] EnemyData _data;
    [SerializeField] int _spawnCount;
    [SerializeField] float _spawnRate;
    [SerializeField] CheckPoint _spawnStart;
    public CheckPoint spawnStart { get { return _spawnStart; } set { _spawnStart = value; } }

    bool _canSpawn;
    public bool canSpawn { get { return _canSpawn; } set { _canSpawn = value; } }

    float _timer = 0f;
    int _spawned = 0;

    void Update()
    {
        if (_canSpawn && _spawned < _spawnCount)
        {
            _timer += Time.deltaTime;
            if (_timer >= _spawnRate)
            {
                _timer -= _spawnRate;
                _spawned += 1;
                Spawn();
            }
        }
    }

    public void Reset()
    {
        _canSpawn = false;
        _spawned = 0;
        _timer = _spawnRate;
    }

    void Spawn()
    {
        GameObject go = EntityManager.instance.SpawnEnemy(_data, _spawnStart.transform.position);
        go.GetComponent<CheckPointMove>().SetNextDestination(_spawnStart.next);
    }
}
