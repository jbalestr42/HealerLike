using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class AreaPulseTests
{
    GameObject _ownerGo;
    GameObject _areaGo;
    ZoneRegistry _owner;
    AreaPulse _pulse;

    [SetUp]
    public void SetUp()
    {
        _ownerGo = new GameObject("zones");
        _areaGo = new GameObject("area");
        _owner = _ownerGo.AddComponent<ZoneRegistry>();
        _owner.Init(new ZoneFakeUpload());
        _pulse = _areaGo.AddComponent<AreaPulse>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_areaGo);
        Object.DestroyImmediate(_ownerGo);
    }

    [Test]
    public void Start_ScaledArea_PulsesAtTheAreaRadiusAndDisableRemovesIt()
    {
        AreaOfEffect area = _areaGo.GetComponent<AreaOfEffect>();
        area.radius = 3.25f;
        _areaGo.transform.position = new Vector3(2f, 3f, 4f);
        _areaGo.transform.localScale = Vector3.one * 99f;
        _pulse.Init(_owner);

        TestHelpers.InvokePrivate(_pulse, "Start");
        _owner.PublishFrame(0f);

        Assert.AreEqual((int)ZoneKind.Hostile, _owner.snapshot[0].kind);
        Assert.AreEqual(3.25f, _owner.snapshot[0].radius);
        Assert.AreEqual(_areaGo.transform.position, _owner.snapshot[0].position);

        _owner.PublishFrame(0.4f);
        Assert.AreEqual(0.5f, _owner.snapshot[0].strength);

        TestHelpers.InvokePrivate(_pulse, "OnDisable");
        _owner.PublishFrame(0f);
        Assert.AreEqual(0, _owner.count);
    }

    [Test]
    public void Start_AuthoredKind_PulsesThatKindForEightTenths()
    {
        _pulse.kind = ZoneKind.Heal;
        _pulse.Init(_owner);

        TestHelpers.InvokePrivate(_pulse, "Start");
        _owner.PublishFrame(0f);

        Assert.AreEqual((int)ZoneKind.Heal, _owner.snapshot[0].kind);

        _owner.PublishFrame(0.8f);
        Assert.AreEqual(0, _owner.liveCount);
    }

    [Test]
    public void Start_InitWithZones_PulsesOnTheGivenRegistry()
    {
        _areaGo.GetComponent<AreaOfEffect>().radius = 2f;

        _pulse.Init(_owner);
        TestHelpers.InvokePrivate(_pulse, "Start");

        Assert.AreEqual(1, _owner.liveCount);
    }

    [Test]
    public void Start_InitWithoutZones_PulsesNothing()
    {
        _areaGo.GetComponent<AreaOfEffect>().radius = 2f;

        _pulse.Init(null);
        TestHelpers.InvokePrivate(_pulse, "Start");

        Assert.AreEqual(0, _owner.liveCount);
    }
}

}
