using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class BaseCharacterSkillData
{
    public bool isSingle;
    public Entity.EntityType entityType;
}

public abstract class BaseCharacterSkill<DataType> : ACharacterSkill<DataType> where DataType : BaseCharacterSkillData
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        if (data.isSingle)
        {
            // TODO manage entity type for singletarget
            InteractionManager.instance.SetInteraction(new SingleTargetInteraction((GameObject target) => ApplySkillOnTarget(source, target), onSkillComplete));
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