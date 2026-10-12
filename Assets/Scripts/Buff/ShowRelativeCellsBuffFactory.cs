using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ShowRelativeCellsBuff")]
public class ShowRelativeCellsBuffFactory : BuffFactory<ShowRelativeCellsBuff, ShowRelativeCellsBuffData> { }

[Serializable]
public class ShowRelativeCellsBuffData
{
    public RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent;
    [Min(1)]
    public int range = 1;
    [Preview(75)]
    public GameObject cellPrefab;
}

// Shows the zone of an effect of the holder: the prefab on each cell of the pattern around it, following it, so the
// zone can be seen while placing it (e.g. the allies a Martyr's Heart shares its heals with). Changes nothing else
public class ShowRelativeCellsBuff : ABuff<ShowRelativeCellsBuffData>
{
    List<GameObject> _cells = new List<GameObject>();

    public IReadOnlyList<GameObject> cells => _cells;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        RelativeCellPattern.DestroyCells(_cells);
        _cells = RelativeCellPattern.SpawnCells(target.transform, data.pattern, data.range, GetCellSize(), data.cellPrefab);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        RelativeCellPattern.DestroyCells(_cells);
    }

    protected virtual float GetCellSize()
    {
        return Board.currentCellSize;
    }
}
