using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyStatusOnRelativeCellBuff")]
public class ApplyStatusOnRelativeCellBuffFactory : BuffFactory<ApplyStatusOnRelativeCellBuff, ApplyStatusOnRelativeCellBuffData> { }

[Serializable]
public class ApplyStatusOnRelativeCellBuffData
{
    // Given by the holder to the allies on the cells of the pattern around it (e.g. Blessed)
    public StatusData status;
    public RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent;
    [Min(1)]
    public int range = 1;
}

// Instant: the living allies around the holder get the status from it (e.g. the Censer spreading its blessing)
public class ApplyStatusOnRelativeCellBuff : ABuff<ApplyStatusOnRelativeCellBuffData>
{
    public override void Instant(GameObject source, GameObject target)
    {
        Entity owner = target != null ? target.GetComponent<Entity>() : null;
        if (owner == null || data.status == null)
        {
            return;
        }

        foreach (Entity ally in RelativeCellPattern.FindEntities(owner, GetAllies(owner), data.pattern, data.range, GetCellSize()))
        {
            ally.ApplyStatus(data.status, owner.gameObject);
        }
    }

    public override void Add(GameObject source, GameObject target) { }

    public override void Remove(GameObject source, GameObject target) { }

    protected virtual List<GameObject> GetAllies(Entity owner)
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null ? entityManager.GetEntities(owner.entityType) : new List<GameObject>();
    }

    protected virtual float GetCellSize()
    {
        return Board.currentCellSize;
    }
}
