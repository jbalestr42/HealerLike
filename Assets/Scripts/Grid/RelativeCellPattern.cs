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
