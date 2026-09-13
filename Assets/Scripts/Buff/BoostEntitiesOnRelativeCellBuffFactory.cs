using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/BoostEntitiesOnRelativeCellBuff")]
public class BoostEntitiesOnRelativeCellBuffFactory : BuffFactory<BoostEntitiesOnRelativeCellBuff, BoostEntitiesOnRelativeCellBuffData> { }

[Serializable]
public class BoostEntitiesOnRelativeCellBuffData
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    public RelativeCellPatternType pattern;
    public int range = 1;
    [PreviewField(75)]
    public GameObject boostCellPrefab;
}

public class BoostEntitiesOnRelativeCellBuff : ABuff<BoostEntitiesOnRelativeCellBuffData>
{
    List<GameObject> _cells = new List<GameObject>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        GridManager grid = PlayerBehaviour.instance.grid;

        foreach (Vector2Int offset in RelativeCellPattern.GetOffsets(data.pattern, data.range))
        {
            Vector3 localOffset = new Vector3(offset.x * grid.size, 0f, offset.y * grid.size);
            Vector3 cellPosition = source.transform.position + localOffset;

            GameObject cell = GameObject.Instantiate(data.boostCellPrefab, cellPosition, Quaternion.identity, source.transform);
            cell.GetComponent<BoostCell>().Init(source, data.buffHandlerFactory);
            _cells.Add(cell);
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        foreach (GameObject cell in _cells)
        {
            if (cell != null)
            {
                GameObject.Destroy(cell);
            }
        }
        _cells.Clear();
    }
}
