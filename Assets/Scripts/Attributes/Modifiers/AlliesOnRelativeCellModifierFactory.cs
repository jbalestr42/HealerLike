using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/AlliesOnRelativeCellModifier")]
public class AlliesOnRelativeCellModifierFactory : BuffFactory<AttributeModifierBuff<AlliesOnRelativeCellModifier, AlliesOnRelativeCellModifierData>, AlliesOnRelativeCellModifierData> { }

[Serializable]
public class AlliesOnRelativeCellModifierData : BaseData
{
    // Given for each living ally on a cell of the pattern around the target
    public float value;
    // Gives the value once when no ally is on the pattern instead, nothing otherwise (e.g. a lone wolf)
    public bool isWhenAlone;
    public RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent;
    [Min(1)]
    public int range = 1;
}

// A value depending on the allies around the target, counted again at each update so it follows the
// placement and the deaths (e.g. armor for each adjacent ally, damage without any)
public class AlliesOnRelativeCellModifier : AttributeModifier<AlliesOnRelativeCellModifierData>
{
    Entity _target;

    public override void Init(GameObject source, GameObject target)
    {
        _target = target.GetComponent<Entity>();
    }

    public override float ApplyModifier()
    {
        int allies = CountAllies();
        if (data.isWhenAlone)
        {
            return allies == 0 ? data.value : 0f;
        }
        return data.value * allies;
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
