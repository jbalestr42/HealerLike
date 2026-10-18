using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Cells of a team placed without the player (the simulated teams, the starting units of the sandbox character), the
// enemies being on the right: one group of columns per role, the tanks on the right (front column), then the supports,
// then the damage dealers on the left. Each column is filled from its center row outwards, the units with the most
// max health first, and a full column goes on in the next one on its left. A role without unit takes no column
public static class TeamFormation
{
    // The cell of each unit on the player cells of the board, by its index in units
    public static Dictionary<int, Vector2Int> GetCells(IReadOnlyList<EntityData> units, BoardLayout layout)
    {
        return GetCells(units, layout.GetFrontColumn(BoardCellType.Player), GetCenterRow(layout), layout.height);
    }

    // The row in the middle of the board, the lower one of the two middle rows for an even height
    public static int GetCenterRow(BoardLayout layout)
    {
        return (layout.height - 1) / 2;
    }

    // Placement order, the front first: the units with the most max health, ties in team order
    public static List<int> GetPlacementOrder(IReadOnlyList<EntityData> units)
    {
        return Enumerable.Range(0, units.Count)
            .OrderByDescending(i => GetMaxHealth(units[i]))
            .ToList();
    }

    // Base value of the unit, 0 without the attribute
    public static float GetMaxHealth(EntityData unit)
    {
        return unit != null && unit.attributes != null && unit.attributes.TryGetValue(AttributeType.HealthMax, out float value) ? value : 0f;
    }

    // Order of the groups, from the front: a unit without role stands with the damage dealers
    public static int GetRoleRank(EntityData unit)
    {
        if (unit != null && unit.HasTag(TagNames.Tank))
        {
            return 0;
        }
        if (unit != null && unit.HasTag(TagNames.Support))
        {
            return 1;
        }
        return 2;
    }

    // The cell of each unit, by its index in units
    public static Dictionary<int, Vector2Int> GetCells(IReadOnlyList<EntityData> units, int frontColumn, int centerRow, int rowsPerColumn)
    {
        rowsPerColumn = Mathf.Max(1, rowsPerColumn);
        Dictionary<int, Vector2Int> cells = new Dictionary<int, Vector2Int>();
        int column = frontColumn;
        foreach (IGrouping<int, int> group in GetPlacementOrder(units).GroupBy(i => GetRoleRank(units[i])).OrderBy(group => group.Key))
        {
            List<int> members = group.ToList();
            for (int i = 0; i < members.Count; i++)
            {
                cells[members[i]] = new Vector2Int(column - i / rowsPerColumn, centerRow + GetRowOffset(i % rowsPerColumn));
            }
            column -= (members.Count + rowsPerColumn - 1) / rowsPerColumn;
        }
        return cells;
    }

    // The cell of each unit of a pattern placed as drawn, by its index in ReferenceTeam.FromWave(pattern): its front
    // column (the nearest to the team on its board) on the front column (as a wave faces the team, mirrored), its rows centered on centerRow
    public static Dictionary<int, Vector2Int> GetPatternCells(WavePatternData pattern, int frontColumn, int centerRow)
    {
        Dictionary<int, Vector2Int> cells = new Dictionary<int, Vector2Int>();
        if (pattern == null)
        {
            return cells;
        }

        int index = 0;
        foreach ((int rank, int y, EntityData entity) unit in pattern.GetUnitsByRank())
        {
            cells[index] = new Vector2Int(frontColumn - unit.rank, centerRow + unit.y - (pattern.height - 1) / 2);
            index++;
        }
        return cells;
    }

    // 0, 1, -1, 2, -2, ...
    public static int GetRowOffset(int index)
    {
        int distance = (index + 1) / 2;
        return index % 2 == 1 ? distance : -distance;
    }
}
