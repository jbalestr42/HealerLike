using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class TrampleZoneTests
{
    GameObject _root;
    GameObject _obstacle;
    ZoneRegistry _registry;
    TrampleZone _zone;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("zones");
        _obstacle = new GameObject("obstacle");
        _registry = _root.AddComponent<ZoneRegistry>();
        _registry.Init(new ZoneFakeUpload());
        _zone = _obstacle.AddComponent<TrampleZone>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_obstacle);
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void Refresh_ObstacleMovesOrTurnsInvalid_UpdatesOneHandleAndRemovesIt()
    {
        _zone.Init(_registry);
        _zone.Refresh();
        _registry.PublishFrame(1f);
        Assert.AreEqual((int)ZoneKind.Trample, _registry.snapshot[0].kind);

        _obstacle.transform.position = Vector3.forward;
        _zone.radius = 2f;
        for (int i = 0; i < 50; i++)
        {
            _zone.Refresh();
        }

        _registry.PublishFrame(1f);
        Assert.AreEqual(1, _registry.count);
        Assert.AreEqual(Vector3.forward, _registry.snapshot[0].position);
        Assert.AreEqual(2, _registry.snapshot[0].radius);

        _zone.radius = float.NaN;
        _zone.Refresh();
        Assert.AreEqual(0, _registry.liveCount);

        _zone.radius = 1f;
        _zone.Refresh();
        Assert.AreEqual(1, _registry.liveCount);

        _zone.enabled = false;
        TestHelpers.InvokePrivate(_zone, "OnDisable");
        Assert.AreEqual(0, _registry.liveCount);

        _zone.enabled = true;
        _zone.Refresh();
        Assert.AreEqual(1, _registry.liveCount);

        TestHelpers.InvokePrivate(_zone, "OnDestroy");
        Assert.AreEqual(0, _registry.liveCount);
    }

    [Test]
    public void Refresh_Steady_AllocatesNothing()
    {
        _zone.Init(_registry);
        for (int i = 0; i < 100; i++)
        {
            _zone.Refresh();
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            _zone.Refresh();
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
    }

    [Test]
    public void Init_RecreatedRegistry_MovesTheFootprintThere()
    {
        _zone.Init(_registry);
        _zone.Refresh();
        Object.DestroyImmediate(_root);
        _zone.Refresh();
        _root = new GameObject("replacement");
        _registry = _root.AddComponent<ZoneRegistry>();
        _registry.Init(new ZoneFakeUpload());

        _zone.Init(_registry);
        _zone.Refresh();

        Assert.AreEqual(1, _registry.liveCount);
    }

    [Test]
    public void CreatureFootprint_ScaledRoot_IsHalfTheLargerHorizontalScale()
    {
        _obstacle.transform.localScale = new Vector3(0.9f, 3f, -1.2f);

        float footprint = TrampleZone.CreatureFootprint(_obstacle.transform);

        Assert.AreEqual(0.6f, footprint, 0.0001f); // 1 cell * 0.5 * |-1.2|
        Assert.AreEqual(0.75f, TrampleZone.TrampleRadius(footprint), 0.0001f); // + 0.15 margin
        Assert.AreEqual(TrampleZone.Margin, TrampleZone.TrampleRadius(-1f), 0.0001f);
    }

    [Test]
    public void Refresh_InitWithZones_AddsOneFootprintThere()
    {
        _zone.Init(_registry);
        _zone.Refresh();
        _zone.Refresh();

        Assert.AreEqual(1, _registry.liveCount);
    }

    [Test]
    public void Refresh_InitWithoutZones_AddsNothing()
    {
        _zone.Init(null);
        _zone.Refresh();

        Assert.AreEqual(0, _registry.liveCount);
    }
}

}
