using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Cells of the simulated team, the enemies being on the right: one group of columns per role, the tanks on the
// right (front column), then the supports, then the damage dealers on the left. Each column is filled from its
// center row outwards, the units with the most max health first, and a full column goes on in the next one
// on its left. A role without unit takes no column
public static class SimulationFormation
{
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
        foreach (IGrouping<int, int> group in ReferenceTeamGenerator.GetPlacementOrder(units).GroupBy(i => GetRoleRank(units[i])).OrderBy(group => group.Key))
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

    // The cell of each unit of a pattern placed as drawn, by its index in ReferenceTeam.FromWave(pattern): its first
    // column on the front column (as a wave faces the team, mirrored), its rows centered on centerRow
    public static Dictionary<int, Vector2Int> GetPatternCells(WavePatternData pattern, int frontColumn, int centerRow)
    {
        Dictionary<int, Vector2Int> cells = new Dictionary<int, Vector2Int>();
        if (pattern == null || pattern.slots == null)
        {
            return cells;
        }

        int index = 0;
        for (int i = 0; i < pattern.slots.GetLength(0); i++)
        {
            for (int j = 0; j < pattern.slots.GetLength(1); j++)
            {
                if (pattern.slots[i, j].entity != null)
                {
                    cells[index] = new Vector2Int(frontColumn - i, centerRow + j - (pattern.slots.GetLength(1) - 1) / 2);
                    index++;
                }
            }
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
