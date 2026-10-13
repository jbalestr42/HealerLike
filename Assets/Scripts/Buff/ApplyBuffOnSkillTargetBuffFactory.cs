using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffOnSkillTargetBuff")]
public class ApplyBuffOnSkillTargetBuffFactory : BuffFactory<ApplyBuffOnSkillTargetBuff, ApplyBuffOnSkillTargetBuffData> { }

[Serializable]
public class ApplyBuffOnSkillTargetBuffData
{
    // Given by the character to each unit one of its skills is used on
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    // Side of the units getting it (e.g. only the allies, not the enemies hit by a spell)
    public Entity.EntityType entityType = Entity.EntityType.Player;
}

// Held by the character (e.g. an item of the Cleric): each unit its skills are used on gets a buff (e.g. Blessed)
public class ApplyBuffOnSkillTargetBuff : ABuff<ApplyBuffOnSkillTargetBuffData>
{
    Character _owner;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Character>();
        if (_owner != null)
        {
            _owner.OnSkillAppliedOnTarget.AddListener(OnSkillAppliedOnTarget);
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.OnSkillAppliedOnTarget.RemoveListener(OnSkillAppliedOnTarget);
        }
    }

    void OnSkillAppliedOnTarget(GameObject target)
    {
        Entity entity = target != null ? target.GetComponent<Entity>() : null;
        if (entity == null || data.buffHandlerFactory == null || (data.entityType != Entity.EntityType.Any && entity.entityType != data.entityType))
        {
            return;
        }

        entity.GetComponent<BuffManager>().AddHandler(data.buffHandlerFactory, _owner.gameObject, entity.gameObject);
    }
}
