using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/AlliesOnRelativeCellModifier")]
public class AlliesOnRelativeCellModifierFactory : BuffFactory<AttributeModifierBuff<AlliesOnRelativeCellModifier, AlliesOnRelativeCellModifierData>, AlliesOnRelativeCellModifierData> { }

[Serializable]
public class AlliesOnRelativeCellModifierData : BaseData
{
    // Given for each living ally on a cell of the pattern around the target
    public float valuePerAlly;
    public RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent;
    [MinValue(1)]
    public int range = 1;
}

// A value growing with the allies around the target, counted again at each update so it follows the
// placement and the deaths (e.g. armor for each adjacent ally)
public class AlliesOnRelativeCellModifier : AttributeModifier<AlliesOnRelativeCellModifierData>
{
    Entity _target;

    public override void Init(GameObject source, GameObject target)
    {
        _target = target.GetComponent<Entity>();
    }

    public override float ApplyModifier()
    {
        return data.valuePerAlly * CountAllies();
    }

    public int CountAllies()
    {
        if (_target == null)
        {
            return 0;
        }
        return RelativeCellPattern.FindEntities(_target, GetAllies(), data.pattern, data.range, GetCellSize()).Count;
    }

    protected virtual List<GameObject> GetAllies()
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null ? entityManager.GetEntities(_target.entityType) : new List<GameObject>();
    }

    protected virtual float GetCellSize()
    {
        PlayerBehaviour player = PlayerBehaviour.existingInstance;
        return player != null && player.grid != null ? player.grid.size : 1f;
    }
}
