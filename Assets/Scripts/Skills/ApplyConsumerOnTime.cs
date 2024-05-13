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

public class ApplyConsumerOnTime : ASkill<ApplyConsumerOnTimeData>, IStackableBuff
{
    [ReadOnly]
    [SerializeField]
    int _stacks = 1;

    public override bool Execute(GameObject source)
    {
        ResourceModifier resourceModifier = new ResourceModifier();
        resourceModifier.consumers.Add(data.consumerFactory.GetConsumer(source, source));
        resourceModifier.multiplier = _stacks;
        resourceModifier.source = source;

        source.GetComponent<IAttackable>().OnHit(resourceModifier);
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