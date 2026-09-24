using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyConsumerBuff")]
public class ApplyConsumerBuffFactory : BuffFactory<ApplyConsumerBuff, ApplyConsumerBuffData> {}

[Serializable]
public class ApplyConsumerBuffData
{
    [CreateDataButton]
    public AConsumerFactory consumerFactory;

    // TODO: bool canStack
}

public class ApplyConsumerBuff : ABuff<ApplyConsumerBuffData>, IStackableBuff
{
    [ReadOnly]
    [SerializeField]
    int _stacks = 1;

    public override void Instant(GameObject source, GameObject target)
    {
        target.GetComponent<IAttackable>().OnHit(ResourceModifier.Create(data.consumerFactory, source, target, _stacks));
    }

    public override void Add(GameObject source, GameObject target)
    {
    }

    public override void Remove(GameObject source, GameObject target)
    {
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }
}