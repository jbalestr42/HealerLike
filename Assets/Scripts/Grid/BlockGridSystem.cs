using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockGridSystem : GridGeneratorSystem
{
    [SerializeField] int _minSize = 1;
    [SerializeField] int _maxSize = 10;

    public override IEnumerator Spawn(GridGenerator gridGenerator)
    {
        yield return base.Spawn(gridGenerator);

        Vector2Int[] pos = new Vector2Int[]
        {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        List<GridObject> list = new List<GridObject>();
        foreach (GridObject spawnedObject in spawnedObjects)
        {
            list.Add(spawnedObject);
        }

        foreach (GridObject spawnedObject in list)
        {
            int size = gridGenerator.randomGenerator.Next(_minSize, _maxSize);

            GridCell lastCell = spawnedObject.cell;
            for (int i = 0; i < size; i++)
            {
                Vector2Int coord = lastCell.coord + pos[gridGenerator.randomGenerator.Next(0, 4)];
                GridCell newCell = gridGenerator.GetAvailableCell(coord, isWalkable);
                if (newCell != null)
                {
                    SpawnObject(newCell);
                    lastCell = newCell;
                }
                yield return gridGenerator.coroutineGenerationDelay;
            }
        }
    }
}