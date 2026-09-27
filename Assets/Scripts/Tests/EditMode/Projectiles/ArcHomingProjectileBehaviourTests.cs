using NUnit.Framework;
using UnityEngine;

namespace Projectiles
{

public class ArcHomingProjectileBehaviourTests
{
    GameObject _target;
    GameObject _otherTarget;
    Projectile _projectile;
    ArcHomingProjectileBehaviour _behaviour;

    [SetUp]
    public void SetUp()
    {
        _target = CreateTarget("Target", new Vector3(8f, 0f, 0f));
        _otherTarget = CreateTarget("OtherTarget", new Vector3(0f, 0f, 8f));

        _projectile = new GameObject("Projectile").AddComponent<Projectile>();
        _projectile.SetTarget(_target);

        _behaviour = _projectile.gameObject.AddComponent<ArcHomingProjectileBehaviour>();
        _behaviour.data = new ArcHomingProjectileBehaviourData { speed = 4f, angleMin = 45f, angleMax = 45f };
        _behaviour.projectile = _projectile;
        _behaviour.Init(null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_otherTarget);
        Object.DestroyImmediate(_projectile.gameObject);
    }

    GameObject CreateTarget(string name, Vector3 position)
    {
        GameObject target = new GameObject(name);
        target.transform.position = position;
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            entity = target.AddComponent<Entity>();
        });
        TestHelpers.SetPrivateField(entity, "_targetPoint", target);
        return target;
    }

    [Test]
    public void GetHorizontalDistance_IgnoresTheHeight()
    {
        Assert.AreEqual(5f, ArcHomingProjectileBehaviour.GetHorizontalDistance(new Vector3(0f, 10f, 0f), new Vector3(3f, 0f, 4f)), 0.0001f);
    }

    [Test]
    public void GetApexHeight_At45Degrees_IsAQuarterOfTheDistance()
    {
        Assert.AreEqual(2f, ArcHomingProjectileBehaviour.GetApexHeight(8f, 45f), 0.0001f);
    }

    [Test]
    public void GetArcPosition_StartsAtStartAndEndsAtEnd()
    {
        Vector3 start = new Vector3(0f, 1f, 0f);
        Vector3 end = new Vector3(8f, 0f, 2f);

        Assert.AreEqual(start, ArcHomingProjectileBehaviour.GetArcPosition(start, end, 0f, 2f));
        Assert.AreEqual(end, ArcHomingProjectileBehaviour.GetArcPosition(start, end, 1f, 2f));
    }

    [Test]
    public void GetArcPosition_Middle_IsTheApexAboveTheMidpoint()
    {
        Vector3 position = ArcHomingProjectileBehaviour.GetArcPosition(Vector3.zero, new Vector3(8f, 0f, 0f), 0.5f, 2f);

        Assert.AreEqual(new Vector3(4f, 2f, 0f), position);
    }

    [Test]
    public void GetArcPosition_GoesDownAfterTheApex()
    {
        Vector3 end = new Vector3(8f, 0f, 0f);
        float apexY = ArcHomingProjectileBehaviour.GetArcPosition(Vector3.zero, end, 0.5f, 2f).y;
        float laterY = ArcHomingProjectileBehaviour.GetArcPosition(Vector3.zero, end, 0.75f, 2f).y;

        Assert.Less(laterY, apexY);
    }

    [Test]
    public void Advance_HalfwayThroughTheFlight_IsAtTheApex()
    {
        // 8 units at 4 units/s: 2s of flight
        _behaviour.Advance(1f);

        Assert.AreEqual(4f, _projectile.transform.position.x, 0.0001f);
        Assert.AreEqual(2f, _projectile.transform.position.y, 0.0001f);
    }

    [Test]
    public void Advance_WholeFlight_LandsOnTheTargetPoint()
    {
        _behaviour.Advance(1f);
        _behaviour.Advance(1f);

        Assert.AreEqual(_target.transform.position, _projectile.transform.position);
    }

    [Test]
    public void Advance_PastTheEndOfTheFlight_StaysOnTheTargetPoint()
    {
        _behaviour.Advance(5f);

        Assert.AreEqual(_target.transform.position, _projectile.transform.position);
    }

    [Test]
    public void Advance_WithANewTarget_StartsANewArcFromTheCurrentPosition()
    {
        _behaviour.Advance(1f);
        Vector3 positionWhenRetargeted = _projectile.transform.position;

        _projectile.SetTarget(_otherTarget);
        _behaviour.Advance(0f);

        Assert.AreEqual(positionWhenRetargeted, _projectile.transform.position);
    }

    [Test]
    public void Advance_WithANewTarget_LandsOnTheNewTarget()
    {
        _behaviour.Advance(1f);
        _projectile.SetTarget(_otherTarget);

        _behaviour.Advance(10f);

        Assert.AreEqual(_otherTarget.transform.position, _projectile.transform.position);
    }

    [Test]
    public void Advance_WithoutTarget_DoesNotMove()
    {
        _projectile.SetTarget(null);

        _behaviour.Advance(1f);

        Assert.AreEqual(Vector3.zero, _projectile.transform.position);
    }
}

}
