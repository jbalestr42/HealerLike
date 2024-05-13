using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridGeneratorSystem : MonoBehaviour
{
    public class GridObject
    {
        public GridCell cell;
        public GameObject gameObject;
    }

    [SerializeField] int _min = 2;
    public int min { get { return _min; } set { _min = value; } }

    [SerializeField] int _max = 5;
    public int max { get { return _max; } set { _max = value; } }

    [SerializeField] GameObject _prefab;

    [SerializeField] bool _isWalkable = false;
    public bool isWalkable { get { return _isWalkable; } set { _isWalkable = value; } }

    List<GridObject> _spawnedObjects = new List<GridObject>();
    public List<GridObject> spawnedObjects { get { return _spawnedObjects; } }

    public int count { get { return _spawnedObjects.Count; } }

    public virtual IEnumerator Spawn(GridGenerator gridGenerator)
    {
        int count = gridGenerator.randomGenerator.Next(_min, _max);
        for (int i = 0; i < count; i++)
        {
            GridCell cell = gridGenerator.GetRandomCell(_isWalkable);
            if (cell != null)
            {
                SpawnObject(cell);
            }

            yield return gridGenerator.coroutineGenerationDelay;
        }
    }

    public virtual GameObject SpawnObject(GridCell cell)
    {
        GameObject go = Instantiate(_prefab, cell.center, Quaternion.identity);
        _spawnedObjects.Add(new GridObject() { cell = cell, gameObject = go });
        return go;
    }

    public virtual void DespawnAll()
    {
        foreach (GridObject gridObject in _spawnedObjects)
        {
            Destroy(gridObject.gameObject);
        }
        _spawnedObjects.Clear();
    }
}