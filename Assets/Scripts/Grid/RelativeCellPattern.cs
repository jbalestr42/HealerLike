using System.Collections.Generic;
using UnityEngine;

public enum RelativeCellPatternType
{
    Adjacent,
    Diagonal,
    Row,
    Column,
    Line,
}

public static class RelativeCellPattern
{
    static readonly Vector2Int[] RowDirections = { Vector2Int.left, Vector2Int.right };
    static readonly Vector2Int[] ColumnDirections = { Vector2Int.up, Vector2Int.down };
    static readonly Vector2Int[] DiagonalDirections =
    {
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
    };

    // Returns every relative offset the pattern covers, up to `range` cells away in each of its
    // directions - a bounded, explicit list, so callers can spawn exactly one thing per offset.
    public static List<Vector2Int> GetOffsets(RelativeCellPatternType patternType, int range)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();

        switch (patternType)
        {
            case RelativeCellPatternType.Adjacent:
                for (int x = -range; x <= range; x++)
                {
                    for (int y = -range; y <= range; y++)
                    {
                        if (x != 0 || y != 0)
                        {
                            offsets.Add(new Vector2Int(x, y));
                        }
                    }
                }
                break;
            case RelativeCellPatternType.Diagonal:
                AddAlongDirections(offsets, DiagonalDirections, range);
                break;
            case RelativeCellPatternType.Row:
                AddAlongDirections(offsets, RowDirections, range);
                break;
            case RelativeCellPatternType.Column:
                AddAlongDirections(offsets, ColumnDirections, range);
                break;
            case RelativeCellPatternType.Line:
                AddAlongDirections(offsets, RowDirections, range);
                AddAlongDirections(offsets, ColumnDirections, range);
                AddAlongDirections(offsets, DiagonalDirections, range);
                break;
        }

        return offsets;
    }

    // The living entities of the list, other than the center, on a cell of the pattern around it
    public static List<Entity> FindEntities(Entity center, List<GameObject> entities, RelativeCellPatternType patternType, int range, float cellSize)
    {
        List<Vector2Int> offsets = GetOffsets(patternType, range);
        List<Entity> found = new List<Entity>();
        foreach (GameObject go in entities)
        {
            Entity entity = go != null ? go.GetComponent<Entity>() : null;
            if (entity == null || entity == center || entity.health == null || entity.health.Value <= 0f)
            {
                continue;
            }
            if (offsets.Contains(GetCellOffset(center.transform.position, go.transform.position, cellSize)))
            {
                found.Add(entity);
            }
        }
        return found;
    }

    // One instance of the prefab on each cell of the pattern around the owner, children of it so they follow it
    public static List<GameObject> SpawnCells(Transform owner, RelativeCellPatternType patternType, int range, float cellSize, GameObject cellPrefab)
    {
        List<GameObject> cells = new List<GameObject>();
        if (owner == null || cellPrefab == null)
        {
            return cells;
        }
        foreach (Vector2Int offset in GetOffsets(patternType, range))
        {
            Vector3 cellPosition = owner.position + new Vector3(offset.x * cellSize, 0f, offset.y * cellSize);
            cells.Add(Object.Instantiate(cellPrefab, cellPosition, Quaternion.identity, owner));
        }
        return cells;
    }

    public static void DestroyCells(List<GameObject> cells)
    {
        foreach (GameObject cell in cells)
        {
            if (cell == null)
            {
                continue;
            }
            if (Application.isPlaying)
            {
                Object.Destroy(cell);
            }
            else
            {
                Object.DestroyImmediate(cell);
            }
        }
        cells.Clear();
    }

    // Offset in cells from one position to another on the ground plane
    public static Vector2Int GetCellOffset(Vector3 from, Vector3 to, float cellSize)
    {
        Vector3 delta = (to - from) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(delta.x), Mathf.RoundToInt(delta.z));
    }

    static void AddAlongDirections(List<Vector2Int> offsets, Vector2Int[] directions, int range)
    {
        foreach (Vector2Int direction in directions)
        {
            for (int distance = 1; distance <= range; distance++)
            {
                offsets.Add(direction * distance);
            }
        }
    }
}
