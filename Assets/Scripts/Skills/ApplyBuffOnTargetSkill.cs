using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ApplyBuffOnTargetSkillData : SkillDataBase
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ATargetValidatorFactory>, ATargetValidatorFactory>(targetValidators)")]
    public List<ATargetValidatorFactory> targetValidators;
    public bool singleTimeUse = false;
    [HideIf("singleTimeUse")]
    public float rate = 5f;
    public float range = 5f;
    public bool targetAlly;
}

public class ApplyBuffOnTargetSkill : ASkill<ApplyBuffOnTargetSkillData>
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
            List<GameObject> targets = _targetBehaviour.GetTargets(source, transform.position, data.range, _targetType);
            GameObject target = targets.Count > 0 ? targets[0] : null;
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
}