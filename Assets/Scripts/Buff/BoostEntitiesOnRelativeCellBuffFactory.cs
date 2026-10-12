using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/BoostEntitiesOnRelativeCellBuff")]
public class BoostEntitiesOnRelativeCellBuffFactory : BuffFactory<BoostEntitiesOnRelativeCellBuff, BoostEntitiesOnRelativeCellBuffData> { }

[Serializable]
public class BoostEntitiesOnRelativeCellBuffData
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    public RelativeCellPatternType pattern;
    public int range = 1;
    [Preview(75)]
    public GameObject boostCellPrefab;
}

public class BoostEntitiesOnRelativeCellBuff : ABuff<BoostEntitiesOnRelativeCellBuffData>
{
    List<GameObject> _cells = new List<GameObject>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _cells = RelativeCellPattern.SpawnCells(source.transform, data.pattern, data.range, Board.currentCellSize, data.boostCellPrefab);
        foreach (GameObject cell in _cells)
        {
            cell.GetComponent<BoostCell>().Init(source, data.buffHandlerFactory);
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        RelativeCellPattern.DestroyCells(_cells);
    }
}
