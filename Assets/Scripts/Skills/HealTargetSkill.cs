using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class HealTargetSkillData : SkillDataBase
{
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ATargetValidatorFactory>, ATargetValidatorFactory>(targetValidators)")]
    public List<ATargetValidatorFactory> targetValidators;
    public float rate = 5f;
    public float range = 5f;
}

public class HealTargetSkill : ASkill<HealTargetSkillData>
{
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
        List<GameObject> targets = _targetBehaviour.GetTargets(transform.position, data.range, source.GetComponent<Entity>().GetTargetType());
        if (targets.Count > 0)
        {
            ResourceModifier resourceModifier = new ResourceModifier();
            resourceModifier.consumers.Add(data.consumerFactory.GetConsumer(source, targets[0]));
            resourceModifier.source = source;

            targets[0].GetComponent<IAttackable>().OnHit(resourceModifier);
            return true;
        }
        return false;
    }

    public override float cooldownDuration => data.rate;
}