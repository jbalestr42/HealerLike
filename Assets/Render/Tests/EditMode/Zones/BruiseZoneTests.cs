using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class BruiseZoneTests
{
    GameObject _root;
    GameObject _actor;
    ZoneRegistry _owner;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("zones");
        _actor = new GameObject("enemy");
        _owner = _root.AddComponent<ZoneRegistry>();
        _owner.Init(new ZoneFakeUpload());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_actor);
        Object.DestroyImmediate(_root);
    }

    static Entity CreateEnemy(GameObject go, float range)
    {
        AttributeManager attributes = TestHelpers.CreateAttributeManager(go, AttributeType.Range, range);
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
        entity.attributeManager = attributes;
        entity.entityType = Entity.EntityType.Computer;
        return entity;
    }

    [Test]
    public void Refresh_EnemyMovesAndRangeChanges_FollowsThemAndCleansUp()
    {
        Entity entity = CreateEnemy(_actor, 3f);
        AttributeManager attributes = entity.attributeManager;
        BruiseZone bruise = _actor.AddComponent<BruiseZone>();
        bruise.Init(entity, _owner);
        _owner.PublishFrame(0f);

        Assert.AreEqual((int)ZoneKind.Bruise, _owner.snapshot[0].kind);
        Assert.AreEqual(3, _owner.snapshot[0].radius);

        _actor.transform.position = Vector3.forward;
        attributes.Get(AttributeType.Range).BaseValue = 5;
        attributes.Get(AttributeType.Range).Update();
        bruise.Refresh();
        _owner.PublishFrame(0f);

        Assert.AreEqual(1, _owner.count);
        Assert.AreEqual(5, _owner.snapshot[0].radius);
        Assert.AreEqual(Vector3.forward, _owner.snapshot[0].position);

        entity.entityType = Entity.EntityType.Player;
        bruise.Refresh();
        Assert.AreEqual(0, _owner.liveCount);

        entity.entityType = Entity.EntityType.Computer;
        bruise.Refresh();
        Assert.AreEqual(1, _owner.liveCount);

        bruise.enabled = false;
        TestHelpers.InvokePrivate(bruise, "OnDisable");
        Assert.AreEqual(0, _owner.liveCount);
    }

    [Test]
    public void Init_WithZones_AddsTheBruiseThere()
    {
        Entity entity = CreateEnemy(_actor, 3f);
        BruiseZone bruise = _actor.AddComponent<BruiseZone>();

        bruise.Init(entity, _owner);
        _owner.PublishFrame(0f);

        Assert.AreEqual((int)ZoneKind.Bruise, _owner.snapshot[0].kind);
        Assert.AreEqual(3f, _owner.snapshot[0].radius);
    }

    [Test]
    public void Init_WithoutZones_IgnoresTheStaticRegistry()
    {
        Entity entity = CreateEnemy(_actor, 3f);
        BruiseZone bruise = _actor.AddComponent<BruiseZone>();

        bruise.Init(entity, (ZoneRegistry)null);
        bruise.Refresh();

        Assert.AreEqual(0, _owner.liveCount);
    }

    [TestCase(3f, true)]
    [TestCase(15.9f, true)]
    [TestCase(16f, false)]
    [TestCase(100f, false)]
    [TestCase(0f, false)]
    public void Bruises_Range_OnlyUnderTheBoardWidth(float range, bool expected)
    {
        Assert.AreEqual(expected, BruiseZone.Bruises(range));
    }

    [Test]
    public void Refresh_BoardWideRange_AddsNoBruise()
    {
        Entity entity = CreateEnemy(_actor, 100f);
        BruiseZone bruise = _actor.AddComponent<BruiseZone>();

        bruise.Init(entity, _owner);

        Assert.AreEqual(0, _owner.liveCount); // the Soldier's 100 would cover every cell
    }
}

}
