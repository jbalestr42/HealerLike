using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffOnHealBuff")]
public class ApplyBuffOnHealBuffFactory : BuffFactory<ApplyBuffOnHealBuff, ApplyBuffOnHealBuffData> { }

[Serializable]
public class ApplyBuffOnHealBuffData
{
    // Given to the holder by itself on each heal it receives: its duration and max stacks say how the heals add up
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

// Each heal received by the holder gives it a buff (e.g. Radiant Archer, Paladin)
public class ApplyBuffOnHealBuff : ABuff<ApplyBuffOnHealBuffData>
{
    Entity _owner;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.OnHealReceived.AddListener(OnHealReceived);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.OnHealReceived.RemoveListener(OnHealReceived);
        }
    }

    void OnHealReceived(GameObject source, ConsumerResult heal)
    {
        BuffManager buffManager = _owner.GetComponent<BuffManager>();
        if (buffManager == null || data.buffHandlerFactory == null)
        {
            return;
        }

        buffManager.AddHandler(data.buffHandlerFactory, _owner.gameObject, _owner.gameObject);
    }
}
