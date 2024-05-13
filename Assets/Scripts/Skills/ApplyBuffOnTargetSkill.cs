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
    public bool isSelfTarget = false;
    [HideIf("isSelfTarget")]
    public float range = 5f;
}

public class ApplyBuffOnTargetSkill : ASkill<ApplyBuffOnTargetSkillData>
{
    int _usageCount = 0;
    ATargetBehaviour _targetBehaviour;

    void Start()
    {
        _targetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.LowestHealth);
        foreach (ATargetValidatorFactory targetValidator in data.targetValidators)
        {
            _targetBehaviour.targetValidators.Add(targetValidator.GetTargetValidator());
        }
    }

    public override bool Execute(GameObject source)
    {
        if (CanUseSkill())
        {
            List<GameObject> targets = _targetBehaviour.GetTargets(transform.position, data.range, source.GetComponent<Entity>().GetTargetType());
            GameObject target = data.isSelfTarget ? targets.Find(t => t == source) : targets.Count > 0 ? targets[0] : null;
            if (target != null)
            {
                _usageCount++;
                Debug.Log($"[ApplyBuffOnTargetSkill] Use skill isSelfTarget={data.isSelfTarget} | source={source} | target={target}");
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