using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ConsumerOnAttackBuff")]
public class ConsumerOnAttackBuffFactory : BuffFactory<ConsumerOnAttackBuff, ConsumerOnAttackBuffData> { }

[Serializable]
public class ConsumerOnAttackBuffData
{
    // Applied to the entity carrying the buff each time it attacks (e.g. a health cost)
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
}

public class ConsumerOnAttackBuff : ABuff<ConsumerOnAttackBuffData>, IStackableBuff
{
    int _stacks = 1;
    Entity _owner;

    public void OnAttack(ProjectileAttack attack)
    {
        _owner.health.AddResourceModifier(ResourceModifier.Create(data.consumerFactory, _owner.gameObject, _owner.gameObject, _stacks));
    }

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.OnAttack.AddListener(OnAttack);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        _owner.OnAttack.RemoveListener(OnAttack);
    }

    #region IStackableBuff

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }

    #endregion
}
