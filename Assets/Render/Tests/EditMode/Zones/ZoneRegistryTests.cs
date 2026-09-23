using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Zones
{

public class ZoneFakeUpload : IZoneUpload
{
    public readonly List<string> calls = new List<string>();
    public readonly Zone[] data = new Zone[64];
    public int count;

    public GraphicsBuffer buffer { get { return null; } }

    public void Upload(Zone[] zones)
    {
        zones.CopyTo(data, 0);
        calls.Add("upload");
    }

    public void Bind()
    {
        calls.Add("bind");
    }

    public void PublishCount(int count)
    {
        this.count = count;
        calls.Add("count:" + count);
    }

    public void Unbind()
    {
        calls.Add("unbind");
    }

    public void Dispose()
    {
        calls.Add("dispose");
    }
}

public class ZoneRegistryTests
{
    static readonly string overflowWarning = "ZoneRegistry: cosmetic zone capacity exceeded; "
                                             + "feedback reserved before decorative footprints; "
                                             + "first registered wins within each kind.";

    GameObject _go;
    ZoneRegistry _registry;
    ZoneFakeUpload _upload;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("zone test");
        _registry = _go.AddComponent<ZoneRegistry>();
        _upload = new ZoneFakeUpload();
        _registry.Init(_upload);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    int Add(float x)
    {
        return _registry.Add(ZoneKind.Heal, new Vector3(x, 0f, 0f), 1f, 1f);
    }

    [Test]
    public void InvalidLaunchAndMissingHealTargetAreRejected()
    {
        Assert.AreEqual(0, _registry.AddLaunch(Vector3.zero, Vector3.up));
        Assert.AreEqual(0, _registry.AddLaunch(Vector3.zero, new Vector3(float.NaN, 0, 0)));
        Assert.AreEqual(0, _registry.AddHealPulse(null, 1));
        Assert.AreEqual(0, _registry.liveCount);
    }

    [Test]
    public void ActsAsTheRegistryZoneOwner()
    {
        IZoneOwner owner = _registry;
        int handle = owner.AddPulse(ZoneKind.Hostile, Vector3.zero, 2f, 1f, 0.8f);
        Assert.That(handle, Is.GreaterThan(0));
        Assert.IsTrue(_registry.Contains(handle));

        _registry.PublishFrame(0.8f);
        Assert.IsFalse(_registry.Contains(handle));
    }

    [Test]
    public void UpdatesPreserveOrderAndPublishCanonicalSnapshotEveryFrame()
    {
        int first = Add(1f);
        Add(2f);
        Add(3f);
        _registry.UpdateZone(first, ZoneKind.Hostile, Vector3.right * 9f, 2f, 5f);
        _upload.calls.Clear();

        _registry.PublishFrame(0.25f);

        CollectionAssert.AreEqual(new[] { "upload", "bind", "count:3" }, _upload.calls);
        Assert.AreEqual(9, _upload.data[0].position.x);
        Assert.AreEqual(2, _upload.data[1].position.x);
        Assert.AreEqual(3, _upload.data[2].position.x);
        Assert.AreEqual(1, _upload.data[0].strength);
        Assert.AreEqual(0.25f, _upload.data[0].age);

        _registry.PublishFrame(0.25f);
        Assert.AreEqual(0.5f, _registry.snapshot[0].age);
    }

    [Test]
    public void OverflowKeepsFirst64AndReportsOncePerEpisode()
    {
        int first = Add(0f);
        for (int i = 1; i < 65; i++)
        {
            Add(i);
        }

        LogAssert.Expect(LogType.Warning, overflowWarning);
        _registry.PublishFrame(0f);

        Assert.AreEqual(64, _upload.count);
        Assert.AreEqual(1, _registry.overflowCount);
        Assert.AreEqual(63, _upload.data[63].position.x);

        _registry.PublishFrame(0f);
        _registry.Remove(first);
        _registry.PublishFrame(0f);

        Assert.AreEqual(64, _upload.data[63].position.x);
        Assert.AreEqual(0, _registry.overflowCount);

        Add(65f);
        LogAssert.Expect(LogType.Warning, overflowWarning);
        _registry.PublishFrame(0f);
    }

    [Test]
    public void PersistentFootprintsCannotStarveHealAndExpiredFeedbackReturnsCapacity()
    {
        for (int i = 0; i < 80; i++)
        {
            _registry.Add(ZoneKind.Trample, Vector3.right * i, 1f, 1f);
        }

        int heal = _registry.AddPulse(ZoneKind.Heal, Vector3.right * 100f, 2f, 1f, 0.45f);
        _registry.AddPulse(ZoneKind.Hostile, Vector3.right * 101f, 2f, 1f, 0.8f);
        LogAssert.Expect(LogType.Warning, overflowWarning);

        _registry.PublishFrame(0.1f);

        Assert.AreEqual(64, _registry.count);
        Assert.AreEqual(82, _registry.liveCount);
        Assert.AreEqual(18, _registry.overflowCount);
        for (int i = 0; i < 62; i++)
        {
            Assert.AreEqual(i, _registry.snapshot[i].position.x);
        }

        Assert.AreEqual((int)ZoneKind.Heal, _registry.snapshot[62].kind);
        Assert.AreEqual((int)ZoneKind.Hostile, _registry.snapshot[63].kind);

        _registry.PublishFrame(0.4f);
        Assert.IsFalse(_registry.Contains(heal));
        Assert.AreEqual(62, _registry.snapshot[62].position.x);
        Assert.AreEqual((int)ZoneKind.Hostile, _registry.snapshot[63].kind);

        _registry.PublishFrame(0.4f);
        Assert.AreEqual(63, _registry.snapshot[63].position.x);
    }

    [Test]
    public void RemovedSlotsAreReusedWithoutReusingHandlesOrReordering()
    {
        int old = Add(1f);
        Add(2f);
        _registry.Remove(old);
        _registry.Remove(old);
        int replacement = Add(3f);
        Assert.AreNotEqual(old, replacement);

        _registry.UpdateZone(old, ZoneKind.Hostile, Vector3.zero, 9f, 1f);
        _registry.PublishFrame(0f);

        Assert.AreEqual(2, _upload.count);
        Assert.AreEqual(2, _upload.data[0].position.x);
        Assert.AreEqual(3, _upload.data[1].position.x);
        Assert.AreEqual(default(Zone), _upload.data[2]);
    }

    [Test]
    public void PulseFadesLinearlyAndAutoRemovesAtDuration()
    {
        int pulse = _registry.AddPulse(ZoneKind.Hostile, Vector3.one, 2f, 0.8f, 0.8f);

        _registry.PublishFrame(0.4f);
        Assert.AreEqual(0.4f, _upload.data[0].strength, 0.0001f);

        _registry.PublishFrame(0f);
        Assert.AreEqual(0.4f, _upload.data[0].age);

        _registry.PublishFrame(0.4f);
        Assert.IsFalse(_registry.Contains(pulse));
        Assert.AreEqual(0, _registry.liveCount);
        Assert.AreEqual(0, _upload.count);
        Assert.AreEqual(default(Zone), _upload.data[0]);
    }

    [Test]
    public void TeardownPublishesZeroBeforeUnbindingAndDisposingExactlyOnce()
    {
        int old = Add(1f);
        _registry.PublishFrame(0f);
        _upload.calls.Clear();

        _registry.Release();
        _registry.Release();

        CollectionAssert.AreEqual(new[] { "count:0", "unbind", "dispose" }, _upload.calls);
        Assert.AreEqual(0, _registry.count);
        Assert.AreEqual(0, Add(2f));

        _registry.Init(new ZoneFakeUpload());
        Assert.AreNotEqual(old, Add(3f));
    }

    [Test]
    public void InvalidInputCannotCreateOrKeepAnActiveZone()
    {
        Assert.AreEqual(0, _registry.Add(ZoneKind.None, Vector3.zero, 1f, 1f));
        Assert.AreEqual(0, _registry.Add(ZoneKind.Heal, Vector3.zero, 0f, 1f));
        Assert.AreEqual(0, _registry.Add(ZoneKind.Heal, Vector3.zero, 1f, 0f));
        Assert.AreEqual(0, _registry.AddPulse(ZoneKind.Heal, Vector3.zero, 1f, 1f, float.NaN));
        Assert.AreEqual(0, _registry.AddPulse(ZoneKind.Heal, Vector3.zero, 1f, 1f, 0f));

        int handle = Add(1f);
        _registry.UpdateZone(handle, ZoneKind.Heal, Vector3.zero, 1f, float.NaN);
        Assert.IsFalse(_registry.Contains(handle));
    }

    [Test]
    public void PublishFrame_NegativeDeltaTime_LogsAndLeavesTheSnapshot()
    {
        Add(1f);
        _registry.PublishFrame(0.5f);
        _upload.calls.Clear();

        LogAssert.Expect(LogType.Error, new Regex(@"^\[ZoneRegistry\] PublishFrame needs a finite delta time"));
        _registry.PublishFrame(-1f);

        Assert.AreEqual(0, _upload.calls.Count);
        Assert.AreEqual(1, _registry.count);
        Assert.AreEqual(0.5f, _upload.data[0].age);
    }

    [Test]
    public void PublishFrame_NaNDeltaTime_LogsAndLeavesTheSnapshot()
    {
        Add(1f);
        _upload.calls.Clear();

        LogAssert.Expect(LogType.Error, new Regex(@"^\[ZoneRegistry\] PublishFrame needs a finite delta time"));
        _registry.PublishFrame(float.NaN);

        Assert.AreEqual(0, _upload.calls.Count);
        Assert.AreEqual(0, _registry.count);
    }

}

}
