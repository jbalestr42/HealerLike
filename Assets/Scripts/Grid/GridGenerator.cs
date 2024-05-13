using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class GridGenerator : MonoBehaviour 
{
    [SerializeField] List<GridGeneratorSystem> _gridGenerators;

    List<GridCell> _availableCells = new List<GridCell>();
    GridManager _gridManager;

    System.Random _random;
    public System.Random randomGenerator { get { return _random; } }

    YieldInstruction _coroutineGenerationDelay;
    public YieldInstruction coroutineGenerationDelay { get { return _coroutineGenerationDelay; } }

    public void Generate(GridManager grid, Transform ground, int seed)
    {
        Debug.Log("[GridGenerator] Seed: " + seed);
        _random = new System.Random(seed);
        _coroutineGenerationDelay = new WaitForEndOfFrame();
        _gridManager = grid;
        DespawnSystems();
        InitAvailablePositions(grid);
        StartCoroutine(SpawnSystems(grid, ground));
    }

    IEnumerator SpawnSystem(GridManager grid, GridGeneratorSystem gridGenerator, Transform ground)
    {
        yield return gridGenerator.Spawn(this);
        foreach (GridGeneratorSystem.GridObject spawnedObject in gridGenerator.spawnedObjects)
        {
            spawnedObject.gameObject.transform.SetParent(ground);
        }
    }

    public GridCell GetAvailableCell(Vector2Int coord, bool isWalkable)
    {
        if (!_gridManager.IsValidCoord(coord))
        {
            return null;
        }

        GridCell cell = _gridManager.GetCell(coord);
        if (_availableCells.Contains(cell))
        {
            _availableCells.Remove(cell);

            if (isWalkable || _gridManager.CanPlaceObject(cell))
            {
                _gridManager.SetWalkable(cell, isWalkable);
                return cell;
            }
        }

        return null;
    }

    public GridCell GetRandomCell(bool isWalkable)
    {
        while (_availableCells.Count > 0)
        {
            int index = _random.Next(0, _availableCells.Count);
            GridCell cell = _availableCells[index];
            _availableCells.RemoveAt(index);

            if (isWalkable || _gridManager.CanPlaceObject(cell))
            {
                _gridManager.SetWalkable(cell, isWalkable);
                return cell;
            }
        }

        return null;
    }

    void InitAvailablePositions(GridManager grid)
    {
        _availableCells.Clear();
        foreach (GridCell cell in grid.cells)
        {
            grid.SetWalkable(cell, true);
            _availableCells.Add(cell);
        }
    }

    IEnumerator SpawnSystems(GridManager grid, Transform ground)
    {
        foreach (GridGeneratorSystem gridGenerator in _gridGenerators)
        {
            yield return SpawnSystem(grid, gridGenerator, ground);
        }
        // TODO: on done event to start playing the game
    }

    void DespawnSystems()
    {
        foreach (GridGeneratorSystem gridGenerator in _gridGenerators)
        {
            gridGenerator.DespawnAll();
        }
    }
}