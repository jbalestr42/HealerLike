using System.Collections.Generic;
using HealerLike.Render.Environment;
using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class LaunchWaveTests
{
    GameObject _root;
    GameObject _shot;
    GameObject _source;
    GameObject _target;
    Ground _ground;
    EnvironmentGust _gust;
    Projectile _projectile;
    LaunchWave _wave;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("zones");
        _shot = new GameObject("projectile");
        _source = new GameObject("source");
        _target = new GameObject("target");
        _ground = new Ground();
        _gust = _root.AddComponent<EnvironmentGust>();
        TestHelpers.WithLoggingDisabled(() => _target.AddComponent<Entity>());
        _target.transform.position = Vector3.forward * 4f;
        _projectile = _shot.AddComponent<Projectile>();
        _wave = _shot.AddComponent<LaunchWave>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_shot);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_root);
        _ground.Dispose();
    }

    void Launch()
    {
        _projectile.Init(_source, _target, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
    }

    // The trail the grass gets this frame: its tail and its head on the ground
    GroundStamp Trail()
    {
        List<GroundStamp> stamps = GroundProbe.Stamps(_ground);
        Assert.AreEqual(1, stamps.Count);
        Assert.AreEqual(GroundStampKind.Body, stamps[0].kind, "A shot's trail throws the grass aside as a line.");
        return stamps[0];
    }

    [Test]
    public void Follow_ProjectileInFlight_TrailsBehindItAlongItsPath()
    {
        _wave.Init(_ground, _gust);
        Launch();

        _shot.transform.position = new Vector3(0f, 0.2f, 0.8f);
        _wave.Follow();

        GroundStamp trail = Trail();
        Assert.IsTrue(_wave.isParting);
        Assert.AreEqual(new Vector2(0f, 0f), new Vector2(trail.centreRadius.x, trail.centreRadius.y),
            "From where it left.");
        Assert.AreEqual(new Vector2(0f, 0.8f), new Vector2(trail.push.x, trail.push.y), "To under the projectile.");

        _shot.transform.position = new Vector3(0f, 0.2f, 3f);
        _wave.Follow();

        trail = Trail();
        Assert.AreEqual(3f - LaunchWave.TrailLength, trail.centreRadius.y, 1e-5f, "One trail that moves with it.");
        Assert.AreEqual(3f, trail.push.y, 1e-5f);
    }

    [Test]
    public void Follow_HighFlight_PartsNoGrass()
    {
        _wave.Init(_ground, _gust);
        Launch();

        _shot.transform.position = new Vector3(0f, LaunchWave.HighFlight + 0.5f, 1f);
        _wave.Follow();

        Assert.IsFalse(_wave.isParting);
        Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);
    }

    [Test]
    public void OnDisable_ProjectileLanded_ClearsItsTrail()
    {
        _wave.Init(_ground, _gust);
        Launch();
        _shot.transform.position = new Vector3(0f, 0f, 1f);
        _wave.Follow();
        Assert.IsTrue(_wave.isParting);

        TestHelpers.InvokePrivate(_wave, "OnDisable");
        _wave.Follow();

        Assert.IsFalse(_wave.isParting);
        Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);
    }

    [Test]
    public void Strength_Height_FallsFromTheGroundToHighFlight()
    {
        Assert.AreEqual(1f, LaunchWave.Strength(0f));
        Assert.AreEqual(1f, LaunchWave.Strength(LaunchWave.LowFlight));
        Assert.AreEqual(0f, LaunchWave.Strength(LaunchWave.HighFlight));
        Assert.AreEqual(0.5f, LaunchWave.Strength(0.5f * (LaunchWave.LowFlight + LaunchWave.HighFlight)), 1e-5f);
        Assert.AreEqual(0f, LaunchWave.Strength(float.NaN));
    }

    [Test]
    public void Init_ProjectileLaunched_PushesTheGustFromSourceToTarget()
    {
        _wave.Init(_ground, _gust);

        Launch();

        // The source at the origin, the target four units north: a plant on the path half way
        Vector3 wind = _gust.Sample(Time.timeAsDouble + LaunchWave.GustSeconds * 0.5f, Vector3.forward * 2f);
        Assert.AreEqual(LaunchWave.GustStrength, wind.z, 0.001f);
        Assert.AreEqual(0f, wind.x, 0.00001f);
        Assert.AreEqual(0f, wind.y);
        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + LaunchWave.GustSeconds + 0.01f,
            Vector3.forward * 2f).sqrMagnitude);
    }

    [Test]
    public void Init_WithoutZonesOrGust_EmitsNothing()
    {
        _wave.Init(null, null);

        Launch();
        _shot.transform.position = Vector3.forward;
        _wave.Follow();

        Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);
        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + 0.1, Vector3.forward * 2f).sqrMagnitude);
    }

    [Test]
    public void Init_NoProjectileTarget_LeavesZonesAndGustStill()
    {
        _wave.Init(_ground, _gust);

        _wave.Init(_source);

        Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);
        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + 0.1, Vector3.forward * 2f).sqrMagnitude);
    }
}

}
