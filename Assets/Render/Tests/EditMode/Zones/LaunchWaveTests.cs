using System.Collections.Generic;
using HealerLike.Render.Environment;
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
    ZoneRegistry _owner;
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
        _owner = _root.AddComponent<ZoneRegistry>();
        _owner.Init(new ZoneFakeUpload());
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
    }

    void Launch()
    {
        _projectile.Init(_source, _target, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
    }

    [Test]
    public void Follow_ProjectileInFlight_TrailsBehindItAlongItsPath()
    {
        _wave.Init(_owner, _gust);
        Launch();

        _shot.transform.position = new Vector3(0f, 0.2f, 0.8f);
        _wave.Follow();
        _owner.PublishFrame(0f);

        Assert.AreEqual(1, _owner.count);
        Assert.AreEqual((int)ZoneKind.Launch, _owner.snapshot[0].kind);
        Assert.AreEqual(new Vector3(0f, 0f, 0.8f), _owner.snapshot[0].position, "Under the projectile.");
        Assert.AreEqual(0.8f, _owner.snapshot[0].radius, 1e-5f, "As long as it has flown so far.");
        Assert.AreEqual(1073741824u, _owner.snapshot[0].reserved, "Heading north.");

        _shot.transform.position = new Vector3(0f, 0.2f, 3f);
        _wave.Follow();
        _owner.PublishFrame(0.1f);

        Assert.AreEqual(1, _owner.count, "One trail that moves with its projectile.");
        Assert.AreEqual(new Vector3(0f, 0f, 3f), _owner.snapshot[0].position);
        Assert.AreEqual(LaunchWave.TrailLength, _owner.snapshot[0].radius, 1e-5f);
    }

    [Test]
    public void Follow_HighFlight_PartsNoGrass()
    {
        _wave.Init(_owner, _gust);
        Launch();

        _shot.transform.position = new Vector3(0f, LaunchWave.HighFlight + 0.5f, 1f);
        _wave.Follow();

        Assert.AreEqual(0, _owner.liveCount);
    }

    [Test]
    public void OnDisable_ProjectileLanded_ClearsItsTrail()
    {
        _wave.Init(_owner, _gust);
        Launch();
        _shot.transform.position = new Vector3(0f, 0f, 1f);
        _wave.Follow();
        Assert.AreEqual(1, _owner.liveCount);

        TestHelpers.InvokePrivate(_wave, "OnDisable");
        _wave.Follow();

        Assert.AreEqual(0, _owner.liveCount);
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
        _wave.Init(_owner, _gust);

        Launch();

        Vector3 wind = _gust.Sample(Time.timeAsDouble + LaunchWave.GustSeconds * 0.5f);
        Assert.AreEqual(LaunchWave.GustStrength, wind.z, 0.001f);
        Assert.AreEqual(0f, wind.x, 0.00001f);
        Assert.AreEqual(0f, wind.y);
        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + LaunchWave.GustSeconds + 0.01f).sqrMagnitude);
    }

    [Test]
    public void Init_WithoutZonesOrGust_EmitsNothing()
    {
        _wave.Init(null, null);

        Launch();
        _shot.transform.position = Vector3.forward;
        _wave.Follow();

        Assert.AreEqual(0, _owner.liveCount);
        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + 0.1).sqrMagnitude);
    }

    [Test]
    public void Init_NoProjectileTarget_LeavesZonesAndGustStill()
    {
        _wave.Init(_owner, _gust);

        _wave.Init(_source);

        Assert.AreEqual(0, _owner.liveCount);
        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + 0.1).sqrMagnitude);
    }
}

}
