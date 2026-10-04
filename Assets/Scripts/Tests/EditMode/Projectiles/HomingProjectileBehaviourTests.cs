using NUnit.Framework;
using UnityEngine;

namespace Projectiles
{

public class HomingProjectileBehaviourTests
{
    GameObject _target;
    Projectile _projectile;
    HomingProjectileBehaviour _behaviour;

    [SetUp]
    public void SetUp()
    {
        _target = new GameObject("Target");
        _target.transform.position = new Vector3(8f, 0f, 0f);
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            entity = _target.AddComponent<Entity>();
        });
        TestHelpers.SetPrivateField(entity, "_targetPoint", _target);

        _projectile = new GameObject("Projectile").AddComponent<Projectile>();
        _projectile.SetTarget(_target);

        _behaviour = _projectile.gameObject.AddComponent<HomingProjectileBehaviour>();
        _behaviour.data = new HomingProjectileBehaviourData { speed = 4f };
        _behaviour.projectile = _projectile;
        _behaviour.Init(null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_projectile.gameObject);
    }

    [Test]
    public void Advance_MovesTowardTheTargetAtItsSpeed()
    {
        _behaviour.Advance(1f);

        Assert.AreEqual(new Vector3(4f, 0f, 0f), _projectile.transform.position);
    }

    [Test]
    public void Advance_LongerThanTheDistanceLeft_StopsOnTheTarget()
    {
        _behaviour.Advance(10f);

        Assert.AreEqual(_target.transform.position, _projectile.transform.position);
    }
}

}
