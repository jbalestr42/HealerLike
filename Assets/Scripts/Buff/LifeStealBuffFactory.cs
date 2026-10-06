using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/LifeStealBuff")]
public class LifeStealBuffFactory : BuffFactory<LifeStealBuff, LifeStealBuffData> { }

// Who the stolen life heals
public enum LifeStealTarget
{
    MostWoundedAlly,
    Holder,
}

[Serializable]
public class LifeStealBuffData
{
    // Part of the damage dealt by the holder turned into a heal
    public float ratio = 0.5f;
    public LifeStealTarget target = LifeStealTarget.MostWoundedAlly;
}

public class LifeStealBuff : ABuff<LifeStealBuffData>
{
    Entity _owner;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.OnDamageDealt.AddListener(OnDamageDealt);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.OnDamageDealt.RemoveListener(OnDamageDealt);
        }
    }

    void OnDamageDealt(GameObject target, float damage)
    {
        if (data.target == LifeStealTarget.Holder)
        {
            LifeSteal.Heal(_owner.gameObject, _owner, damage * data.ratio);
        }
        else
        {
            LifeSteal.HealMostWounded(_owner.gameObject, GetAllies(), damage * data.ratio);
        }
    }

    protected virtual List<GameObject> GetAllies()
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null ? entityManager.GetEntities(_owner.entityType) : new List<GameObject>();
    }
}
