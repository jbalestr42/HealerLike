using System.Collections.Generic;
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
    public void Init_ProjectileLaunched_EmitsDirectionalPulseThatOutlivesIt()
    {
        _wave.Init(_owner);

        Launch();
        _owner.PublishFrame(0f);

        Assert.AreEqual(1, _owner.count);
        Assert.AreEqual((int)ZoneKind.Launch, _owner.snapshot[0].kind);
        Assert.AreEqual(4, _owner.snapshot[0].radius);
        Assert.AreEqual(1073741824u, _owner.snapshot[0].reserved);
        Assert.AreEqual(_source.transform.position, _owner.snapshot[0].position);

        Object.DestroyImmediate(_shot);
        _owner.PublishFrame(0.2f);
        Assert.AreEqual(1, _owner.count);

        _owner.PublishFrame(0.2f);
        Assert.AreEqual(0, _owner.count);
    }

    [Test]
    public void Init_WithZones_LaunchesOnePulse()
    {
        _wave.Init(_owner);

        Launch();

        Assert.AreEqual(1, _owner.liveCount);
    }

    [Test]
    public void Init_WithoutZones_EmitsNothing()
    {
        _wave.Init(null);

        Launch();

        Assert.AreEqual(0, _owner.liveCount);
    }
}

}
