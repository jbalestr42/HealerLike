using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class BaseCharacterSkillData : CharacterSkillData
{
    [BoxGroup("Common/Split/Left/Target")]
    public bool isSingle;

    [BoxGroup("Common/Split/Left/Target")]
    public Entity.EntityType entityType;
}

public abstract class BaseCharacterSkill<DataType> : ACharacterSkill<DataType> where DataType : BaseCharacterSkillData
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        if (data.isSingle)
        {
            InteractionManager.instance.SetInteraction(new SingleTargetInteraction((GameObject target) => ApplySkillOnTarget(source, target), onSkillComplete, data.entityType));
        }
        else
        {
            foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
            {
                ApplySkillOnTarget(source, entity);
            }
            onSkillComplete(true);
        }
    }

    public abstract void ApplySkillOnTarget(GameObject source, GameObject target);
}