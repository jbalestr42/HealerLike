using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneLifeTests
{
    GameObject _root;
    GameObject _top;
    GameObject _zoneRoot;
    GameObject _fxRoot;
    StoneEffects _fx;
    StoneLife _life;
    ZoneRegistry _zones;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("TerrainLife");
        _top = new GameObject("Top");
        _top.transform.SetParent(_root.transform);
        _zoneRoot = new GameObject("Zones");
        _fx = StoneEffectsTests.CreateEffects();
        _fxRoot = _fx.gameObject;
        _life = _root.AddComponent<StoneLife>();
        _zones = _zoneRoot.AddComponent<ZoneRegistry>();
    }

    [TearDown]
    public void TearDown()
    {
        _zones.Release();
        TestHelpers.InvokePrivate(_life, "OnDestroy");
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_fxRoot);
        Object.DestroyImmediate(_zoneRoot);
    }

    [Test]
    public void Advance_PublishedHostilePulse_EmitsOncePerPulse()
    {
        _zones.Init(new ZoneFakeUpload());
        _life.Init(_fx, _zones, 1, 0.5f, true);

        _zones.AddPulse(ZoneKind.Heal, Vector3.zero, 1f, 1f, 1f);
        _zones.PublishFrame(0.01f);
        _life.Advance(0.01f);
        Assert.AreEqual(0, _fx.liveCount);

        _zones.AddPulse(ZoneKind.Hostile, Vector3.zero, 1f, 1f, 1f);
        _zones.PublishFrame(0.01f);
        _life.Advance(0.01f);
        Assert.AreEqual(5, _fx.liveCount);

        _zones.PublishFrame(0.01f);
        _life.Advance(0.01f);
        Assert.AreEqual(5, _fx.liveCount);

        _zones.Release();
        _life.Advance(0.01f);
        Assert.AreEqual(5, _fx.liveCount);
    }

    [Test]
    public void Advance_NearbyImpact_WobblesTheTopAndDisableRestoresIt()
    {
        Quaternion rest = Quaternion.Euler(0f, 30f, 0f);
        _top.transform.localRotation = rest;
        _life.Init(_fx, null, 1, 0.5f, false, _top.transform);

        _fx.RecordImpact(Vector3.right * 20f, 1);
        _life.Advance(0.05f);
        Assert.Less(Quaternion.Angle(rest, _top.transform.localRotation), 0.03f);

        _fx.RecordImpact(Vector3.zero, 1);
        _life.Advance(0.05f);
        Assert.Greater(Quaternion.Angle(rest, _top.transform.localRotation), 3);

        _life.Advance(2f);
        Assert.Less(Quaternion.Angle(rest, _top.transform.localRotation), 0.03f);

        _fx.RecordImpact(Vector3.zero, 1);
        _life.Advance(0.05f);
        _life.enabled = false;
        TestHelpers.InvokePrivate(_life, "OnDisable");
        Assert.Less(Quaternion.Angle(rest, _top.transform.localRotation), 0.03f);

        _life.PollHealth(0.3f, 0.1f, Vector3.up);
        Assert.AreEqual(15, _fx.liveCount);
    }
}

}
