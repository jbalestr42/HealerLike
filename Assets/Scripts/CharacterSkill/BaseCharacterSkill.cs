using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class BaseCharacterSkillData : CharacterSkillData
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
            InteractionManager.instance.SetInteraction(new SingleTargetInteraction((GameObject target) => ApplyOnTarget(source, target), onSkillComplete, data.entityType));
        }
        else
        {
            foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
            {
                ApplyOnTarget(source, entity);
            }
            onSkillComplete(true);
        }
    }

    // The effect of the skill, then the character told about it
    public void ApplyOnTarget(GameObject source, GameObject target)
    {
        ApplySkillOnTarget(source, target);
        Character character = source != null ? source.GetComponent<Character>() : null;
        if (character != null)
        {
            character.OnSkillAppliedOnTarget.Invoke(target);
        }
    }

    public abstract void ApplySkillOnTarget(GameObject source, GameObject target);
}