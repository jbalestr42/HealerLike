using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class HealPulseTests
{
    GameObject _zonesGo;
    GameObject _source;
    GameObject _target;
    ZoneRegistry _zones;
    RenderRegistry _registry;
    HealPulse _pulse;

    [SetUp]
    public void SetUp()
    {
        _zonesGo = new GameObject("zones");
        _zones = _zonesGo.AddComponent<ZoneRegistry>();
        _zones.Init(new ZoneFakeUpload());
        _registry = new RenderRegistry();
        _source = new GameObject("source");
        _target = new GameObject("target");
        _target.transform.position = new Vector3(1f, 2f, 3f);
        _pulse = _source.AddComponent<HealPulse>();
        _pulse.Init(_source, _registry, _zones);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_zonesGo);
    }

    [Test]
    public void NotifyHealth_Heal_PulsesOnTheTargetInCellUnitsThenExpires()
    {
        _registry.NotifyHealth(_source, _target, 4f, true);
        _zones.PublishFrame(0f);

        Assert.AreEqual(1, _zones.count);
        Assert.AreEqual((int)ZoneKind.Heal, _zones.snapshot[0].kind);
        Assert.AreEqual(_target.transform.position, _zones.snapshot[0].position);
        Assert.AreEqual(HealPulse.PulseCells * StageCalibration.CellSize, _zones.snapshot[0].radius);

        _target.transform.position = Vector3.zero;
        _zones.PublishFrame(0.225f);

        Assert.AreEqual(0.5f, _zones.snapshot[0].strength, 0.0001f); // half of 0.45 s
        Assert.AreEqual(Vector3.zero, _zones.snapshot[0].position);

        _zones.PublishFrame(0.225f);

        Assert.AreEqual(0, _zones.count);
    }

    [Test]
    public void OnEnable_AfterDisable_SubscribesOnce()
    {
        _pulse.enabled = false;
        _registry.NotifyHealth(_source, _target, 1f, false);

        Assert.AreEqual(0, _zones.liveCount);

        _pulse.enabled = true;
        _pulse.enabled = false;
        _pulse.enabled = true;
        _registry.NotifyHealth(_source, _target, 1f, false);

        Assert.AreEqual(1, _zones.liveCount);
    }

    [Test]
    public void Init_NewSource_DetachesTheOldOne()
    {
        _pulse.Init(_target, _registry, _zones);

        _registry.NotifyHealth(_source, _target, 1f, false);

        Assert.AreEqual(0, _zones.liveCount);

        _registry.NotifyHealth(_target, _source, 1f, false);

        Assert.AreEqual(1, _zones.liveCount);
    }

    [Test]
    public void Init_OtherRegistry_IgnoresTheFirst()
    {
        RenderRegistry other = new RenderRegistry();
        _pulse.Init(_source, other, _zones);

        _registry.NotifyHealth(_source, _target, 1f, false);

        Assert.AreEqual(0, _zones.liveCount);

        other.NotifyHealth(_source, _target, 1f, false);

        Assert.AreEqual(1, _zones.liveCount);
    }

    [Test]
    public void Pulse_SourceDisabled_EndsOnlyWithTheTarget()
    {
        _pulse.Pulse(_target.transform);
        _pulse.enabled = false;
        _zones.PublishFrame(0.1f);

        Assert.AreEqual(1, _zones.count);

        Object.DestroyImmediate(_target);
        _zones.PublishFrame(0f);

        Assert.AreEqual(0, _zones.count);
    }

    [Test]
    public void NotifyHealth_DamageZeroNonFiniteOrNoTarget_Ignored()
    {
        foreach (float value in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
        {
            _registry.NotifyHealth(_source, _target, value, false);
        }

        _registry.NotifyHealth(_source, null, 1f, false);

        Assert.AreEqual(0, _zones.liveCount);
    }

    [Test]
    public void Init_NoZones_PulsesNothing()
    {
        _pulse.Init(_source, _registry, null);

        _registry.NotifyHealth(_source, _target, 1f, false);

        Assert.AreEqual(0, _zones.liveCount);
    }
}

}
