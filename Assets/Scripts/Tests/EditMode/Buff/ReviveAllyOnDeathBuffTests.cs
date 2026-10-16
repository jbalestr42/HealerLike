using System.Collections.Generic;
using System.Reflection;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The raised allies are spawned by the EntityManager singleton in the game: here a unit with health is made in their
// place
public class TestReviveAllyOnDeathBuff : ReviveAllyOnDeathBuff
{
    public readonly TestUnits units = new TestUnits();
    public readonly List<(EntityData data, Vector3 position)> spawns = new List<(EntityData, Vector3)>();
    public readonly List<Entity> raised = new List<Entity>();

    protected override Entity Spawn(EntityData entityData, Vector3 position)
    {
        spawns.Add((entityData, position));
        Entity entity = units.Create(1f, 200f, "Raised");
        raised.Add(entity);
        return entity;
    }
}

// Twin Blades: when its twin falls, the holder raises it, the twin rising again once the holder is done unless the
// holder dies first, as many times as it falls
public class ReviveAllyOnDeathBuffTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();
    EntityData _twinData;
    Entity _owner;
    Entity _twin;
    BuffManager _ownerBuffs;
    BuffHandlerFactory _raising;
    TestReviveAllyOnDeathBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        _twinData = Track(ScriptableObject.CreateInstance<EntityData>());
        _owner = CreateUnit("Owner", _twinData);
        _twin = CreateUnit("Twin", _twinData);
        _twin.transform.position = new Vector3(3f, 0f, 2f);
        _ownerBuffs = _owner.GetComponent<BuffManager>();
        _ownerBuffs.isEnabled = true;

        _raising = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _raising.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 6f,
            maxStacks = 1,
            buffFactoryList = new List<ABuffFactory>(),
        };
        _buff = new TestReviveAllyOnDeathBuff { data = new ReviveAllyOnDeathBuffData { raisingBuffHandlerFactory = _raising, healthRatio = 0.5f } };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        if (_owner != null)
        {
            _buff.Remove(_owner.gameObject, _owner.gameObject);
        }
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
        _buff.units.DestroyAll();
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    // The buff reaches the manager through EntityManager.instance: seeding the singleton with the test's own
    // manager keeps the getter from searching the open scene or creating a DontDestroyOnLoad object
    static void SetEntityManagerInstance(EntityManager entityManager)
    {
        FieldInfo field = typeof(Singleton<EntityManager>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, entityManager);
    }

    T Track<T>(T obj) where T : Object
    {
        _objects.Add(obj);
        return obj;
    }

    Entity CreateUnit(string name, EntityData data, Entity.EntityType entityType = Entity.EntityType.Computer)
    {
        Entity unit = _units.Create(100f, 100f, name);
        unit.data = data;
        unit.entityType = entityType;
        return unit;
    }

    void Kill(Entity entity)
    {
        _entityManager.OnEntityKilled.Invoke(entity);
        _ownerBuffs.ForceUpdate();
    }

    bool IsRaising() => _ownerBuffs.HasHandler(_raising);

    // The raising lasts its whole duration
    void FinishRaising()
    {
        BuffManager.BuffHandlerData raising = _ownerBuffs.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _raising);
        ((BuffHandler)raising.buffHandler).durationTimer = _raising.data.duration;
        _ownerBuffs.ForceUpdate();
    }

    [Test]
    public void TwinKilled_TheHolderStartsRaisingIt()
    {
        Kill(_twin);

        Assert.IsTrue(IsRaising());
        CollectionAssert.IsEmpty(_buff.spawns, "not before the raising is over");
    }

    [Test]
    public void RaisingOver_TheTwinRisesWhereItDied_WithHalfItsHealth()
    {
        Kill(_twin);

        FinishRaising();

        Assert.IsFalse(IsRaising());
        Assert.AreEqual(1, _buff.spawns.Count);
        Assert.AreSame(_twinData, _buff.spawns[0].data);
        Assert.AreEqual(new Vector3(3f, 0f, 2f), _buff.spawns[0].position);
        Assert.AreEqual(100f, _buff.raised[0].health.Value, 0.0001f);
    }

    [Test]
    public void RaisingOver_TheTwinRisesOnlyOnce()
    {
        Kill(_twin);
        FinishRaising();

        _ownerBuffs.ForceUpdate();

        Assert.AreEqual(1, _buff.spawns.Count);
    }

    [Test]
    public void HolderDeadDuringTheRaising_TheTwinStaysDead()
    {
        Kill(_twin);

        _owner.health.SetValue(0f);
        FinishRaising();

        Assert.AreEqual(0, _buff.spawns.Count);
    }

    [Test]
    public void AnotherKindOfAllyKilled_IsNotRaised()
    {
        Entity other = CreateUnit("Archer", Track(ScriptableObject.CreateInstance<EntityData>()));

        Kill(other);

        Assert.IsFalse(IsRaising());
    }

    [Test]
    public void OpponentOfTheSameKindKilled_IsNotRaised()
    {
        Entity opponent = CreateUnit("Opponent", _twinData, Entity.EntityType.Player);

        Kill(opponent);

        Assert.IsFalse(IsRaising());
    }

    [Test]
    public void HolderKilled_RaisesNothing()
    {
        Kill(_owner);

        Assert.IsFalse(IsRaising());
    }

    [Test]
    public void RaisedTwinKilledAgain_IsRaisedAgain()
    {
        Kill(_twin);
        FinishRaising();
        Entity raised = _buff.raised[0];
        raised.data = _twinData;
        raised.entityType = Entity.EntityType.Computer;
        raised.transform.position = new Vector3(5f, 0f, 1f);

        Kill(raised);
        Assert.IsTrue(IsRaising());
        FinishRaising();

        Assert.AreEqual(2, _buff.spawns.Count);
        Assert.AreEqual(new Vector3(5f, 0f, 1f), _buff.spawns[1].position);
    }

    [Test]
    public void Removed_StopsRaising()
    {
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Kill(_twin);

        Assert.IsFalse(IsRaising());
    }
}

}
