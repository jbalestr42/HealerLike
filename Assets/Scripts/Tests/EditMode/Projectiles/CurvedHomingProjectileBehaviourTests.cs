using NUnit.Framework;
using UnityEngine;

namespace Projectiles
{

public class CurvedHomingProjectileBehaviourTests
{
    GameObject _source;
    GameObject _target;
    Projectile _projectile;
    CurvedHomingProjectileBehaviour _behaviour;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
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
        _projectile.source = _source;
        _projectile.SetTarget(_target);

        _behaviour = _projectile.gameObject.AddComponent<CurvedHomingProjectileBehaviour>();
        _behaviour.data = new CurvedHomingProjectileBehaviourData { speed = 4f, additionnalSpeedOverDistance = 0f };
        _behaviour.projectile = _projectile;
        _behaviour.Init(_source);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_projectile.gameObject);
    }

    [Test]
    public void Advance_GetsCloserToTheTarget()
    {
        _behaviour.Advance(1f);

        Assert.Less(Vector3.Distance(_projectile.transform.position, _target.transform.position), 8f);
    }

    [Test]
    public void Advance_LongerThanTheDistanceLeft_StopsOnTheTarget()
    {
        _behaviour.Advance(10f);

        Assert.AreEqual(_target.transform.position, _projectile.transform.position);
    }
}

}
