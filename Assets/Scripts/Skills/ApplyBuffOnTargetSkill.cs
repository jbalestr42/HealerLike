using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[Serializable]
public class ApplyBuffOnTargetSkillData : SkillDataBase
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    [CreateDataButton]
    public List<ATargetValidatorFactory> targetValidators;
    public bool singleTimeUse = false;
    [HideIf("singleTimeUse")]
    public float rate = 5f;
    public float range = 5f;
    public bool targetAlly;
}

public class ApplyBuffOnTargetSkill : ACooldownSkill<ApplyBuffOnTargetSkillData>, ISkillTargetPreview
{
    int _usageCount = 0;
    ATargetBehaviour _targetBehaviour;
    Entity.EntityType _targetType;

    void Start()
    {
        // TODO: use the TargetProvider but we need to rework it a little bit to select a custom entitytype based on the skill
        _targetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.LowestHealth);
        foreach (ATargetValidatorFactory targetValidator in data.targetValidators)
        {
            _targetBehaviour.targetValidators.Add(targetValidator.GetTargetValidator());
        }
        _targetType = data.targetAlly ? gameObject.GetComponent<Entity>().entityType : gameObject.GetComponent<Entity>().GetTargetType();
    }

    public override bool Execute(GameObject source)
    {
        if (CanUseSkill())
        {
            GameObject target = FindTarget(source);
            if (target != null)
            {
                _usageCount++;
                Debug.Log($"[ApplyBuffOnTargetSkill] Use skill targetAlly={data.targetAlly} | source={source} | target={target}");
                target.GetComponent<BuffManager>().AddHandler(data.buffHandlerFactory, gameObject, target);
                return true;
            }
        }
        return false;
    }

    public override float cooldownDuration => data.rate;

    bool CanUseSkill()
    {
        return !data.singleTimeUse || (data.singleTimeUse && _usageCount < 1);
    }

    // The unit with the lowest health in range, null without any
    GameObject FindTarget(GameObject source)
    {
        List<GameObject> targets = _targetBehaviour.GetTargets(source, transform.position, data.range, _targetType);
        return targets.Count > 0 ? targets[0] : null;
    }

    #region ISkillTargetPreview

    public GameObject GetUpcomingTarget()
    {
        // Before Start, or once a single use is spent
        if (_targetBehaviour == null || !CanUseSkill())
        {
            return null;
        }
        return FindTarget(gameObject);
    }

    #endregion
}