using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// A target provider without any target: shooting spawns no projectile (they need the EntityManager
// singleton), only the attack itself is checked
public class NoTargetProvider : MonoBehaviour, ITargetProvider
{
    public List<GameObject> GetTargets() => new List<GameObject>();
    public int targetCount { get; set; }
    public TargetBehaviourType targetBehaviourType { get; set; }
}

// An attack shooting projectiles, shared by the shoot skill and the shoot skill step
public class ProjectileAttackTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _attacker;
    ProjectileData _projectileData;

    [SetUp]
    public void SetUp()
    {
        _attacker = _units.Create(100f, 100f, "Attacker");
        _attacker.gameObject.AddComponent<NoTargetProvider>();
        _projectileData = new ProjectileData();
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void Shoot_IsReportedAsAnAttackOfTheSource()
    {
        List<ProjectileAttack> attacks = new List<ProjectileAttack>();
        _attacker.OnAttack.AddListener(attacks.Add);

        ProjectileAttack attack = ProjectileAttack.Shoot(_attacker.gameObject, _projectileData);

        CollectionAssert.AreEqual(new[] { attack }, attacks);
        Assert.AreSame(_attacker.gameObject, attack.source);
        Assert.AreSame(_projectileData, attack.projectileData);
    }

    [Test]
    public void Repeat_IsNotReportedAsANewAttack()
    {
        ProjectileAttack attack = ProjectileAttack.Shoot(_attacker.gameObject, _projectileData);
        int attackCount = 0;
        _attacker.OnAttack.AddListener(_ => attackCount++);

        attack.Repeat();

        Assert.AreEqual(0, attackCount);
    }

    [Test]
    public void Repeat_AfterTheSourceIsGone_DoesNothing()
    {
        ProjectileAttack attack = new ProjectileAttack { source = null, projectileData = _projectileData };

        Assert.DoesNotThrow(attack.Repeat);
    }
}

}
