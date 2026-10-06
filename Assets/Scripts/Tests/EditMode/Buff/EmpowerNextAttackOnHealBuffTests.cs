using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Gratitude: the first attack after a heal received deals more damage
public class EmpowerNextAttackOnHealBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<GameObject> _projectiles = new List<GameObject>();
    Entity _owner;
    EmpowerNextAttackOnHealBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(50f, 100f, "Owner");
        _buff = new EmpowerNextAttackOnHealBuff { data = new EmpowerNextAttackOnHealBuffData { damageMultiplier = 1.5f } };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (GameObject projectile in _projectiles)
        {
            Object.DestroyImmediate(projectile);
        }
        _projectiles.Clear();
    }

    // An attack of the owner with one projectile, reported as the game does after shooting it
    ProjectileAttack Attack()
    {
        GameObject go = new GameObject("Projectile");
        _projectiles.Add(go);
        Projectile projectile = null;
        TestHelpers.WithLoggingDisabled(() => projectile = go.AddComponent<Projectile>());
        ProjectileAttack attack = new ProjectileAttack { source = _owner.gameObject };
        attack.projectiles.Add(projectile);
        _owner.OnAttack.Invoke(attack);
        return attack;
    }

    // The damage multiplier of a hit of the projectile of the attack
    static float Hit(ProjectileAttack attack)
    {
        OnHitData onHitData = new OnHitData();
        attack.projectiles[0].OnHit.Invoke(onHitData);
        return onHitData.resourceModifier.multiplier;
    }

    void Heal()
    {
        Entity.NotifyHealed(null, _owner.gameObject, new ConsumerResult(10f, false));
    }

    [Test]
    public void Attack_WithoutAHeal_DealsNormalDamage()
    {
        Assert.AreEqual(1f, Hit(Attack()), 0.0001f);
    }

    [Test]
    public void Attack_AfterAHeal_DealsTheMultipliedDamage()
    {
        Heal();

        Assert.AreEqual(1.5f, Hit(Attack()), 0.0001f);
    }

    [Test]
    public void Attack_OnlyTheFirstOneAfterAHealIsEmpowered()
    {
        Heal();
        Attack();

        Assert.AreEqual(1f, Hit(Attack()), 0.0001f);
    }

    [Test]
    public void Heals_SeveralBeforeAnAttack_EmpowerItOnce()
    {
        Heal();
        Heal();

        Assert.AreEqual(1.5f, Hit(Attack()), 0.0001f);
        Assert.AreEqual(1f, Hit(Attack()), 0.0001f);
    }

    [Test]
    public void Damage_Received_DoesNotEmpower()
    {
        Entity.NotifyHealed(null, _owner.gameObject, new ConsumerResult(-10f, false));

        Assert.AreEqual(1f, Hit(Attack()), 0.0001f);
    }

    [Test]
    public void Attack_AfterRemove_IsNeverEmpowered()
    {
        Heal();
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Assert.IsFalse(_buff.isEmpowered);
        Assert.AreEqual(1f, Hit(Attack()), 0.0001f);
    }
}

}
