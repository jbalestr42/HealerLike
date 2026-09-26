using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ApplyConsumerOnTimeData : SkillDataBase
{
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
    public float rate = 1f;
}

public class ApplyConsumerOnTime : ACooldownSkill<ApplyConsumerOnTimeData>, IStackableBuff
{
    [ReadOnly]
    [SerializeField]
    int _stacks = 1;

    public override bool Execute(GameObject source)
    {
        source.GetComponent<IAttackable>().OnHit(ResourceModifier.Create(data.consumerFactory, source, source, _stacks));
        return true;
    }

    public override float cooldownDuration => data.rate;

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }
}