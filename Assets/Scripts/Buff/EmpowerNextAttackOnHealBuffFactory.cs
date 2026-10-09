using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/EmpowerNextAttackOnHealBuff")]
public class EmpowerNextAttackOnHealBuffFactory : BuffFactory<EmpowerNextAttackOnHealBuff, EmpowerNextAttackOnHealBuffData> { }

[Serializable]
public class EmpowerNextAttackOnHealBuffData
{
    // Damage multiplier of the hits of the first attack after a heal received
    [Min(1)]
    public float damageMultiplier = 1.5f;
}

public class EmpowerNextAttackOnHealBuff : ABuff<EmpowerNextAttackOnHealBuffData>
{
    Entity _owner;
    bool _isEmpowered = false;
    public bool isEmpowered => _isEmpowered;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.OnHealReceived.AddListener(OnHealReceived);
        _owner.OnAttack.AddListener(OnAttack);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.OnHealReceived.RemoveListener(OnHealReceived);
            _owner.OnAttack.RemoveListener(OnAttack);
        }
        _isEmpowered = false;
    }

    void OnHealReceived(GameObject source, ConsumerResult heal)
    {
        _isEmpowered = true;
    }

    void OnAttack(ProjectileAttack attack)
    {
        if (!_isEmpowered)
        {
            return;
        }

        _isEmpowered = false;
        foreach (Projectile projectile in attack.projectiles)
        {
            if (projectile != null)
            {
                projectile.OnHit.AddListener(Empower);
            }
        }
    }

    public void Empower(OnHitData onHitData)
    {
        onHitData.resourceModifier.multiplier *= data.damageMultiplier;
    }
}
