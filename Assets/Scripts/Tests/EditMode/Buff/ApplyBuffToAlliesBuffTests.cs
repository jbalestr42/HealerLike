using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Aura: every ally of the holder has the buff while the holder has it
public class ApplyBuffToAlliesBuffTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    GameObject _owner;
    readonly List<GameObject> _entities = new List<GameObject>();
    readonly List<Object> _objects = new List<Object>();
    ApplyBuffToAlliesBuff _buff;
    FakeBuffData _data;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        _owner = CreateEntity("BloodWarden", Entity.EntityType.Computer);

        _data = new FakeBuffData();
        FakeBuffFactory buffFactory = ScriptableObject.CreateInstance<FakeBuffFactory>();
        buffFactory.data = _data;
        BuffHandlerFactory handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory> { buffFactory },
        };
        _objects.Add(buffFactory);
        _objects.Add(handlerFactory);

        _buff = new ApplyBuffToAlliesBuff { data = new ApplyBuffToAlliesBuffData { buffHandlerFactory = handlerFactory } };
    }

    [TearDown]
    public void TearDown()
    {
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
        foreach (GameObject entity in _entities)
        {
            if (entity != null)
            {
                Object.DestroyImmediate(entity);
            }
        }
        _entities.Clear();
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

    GameObject CreateEntity(string name, Entity.EntityType entityType, bool isListed = true)
    {
        GameObject go = new GameObject(name);
        _entities.Add(go);
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            entity = go.AddComponent<Entity>();
        });
        entity.entityType = entityType;
        // Required by Entity, already added with it
        go.GetComponent<BuffManager>().isEnabled = true;
        if (isListed)
        {
            _entityManager.GetEntities(entityType).Add(go);
        }
        return go;
    }

    static int ActiveHandlers(GameObject entity)
    {
        BuffManager buffManager = entity.GetComponent<BuffManager>();
        buffManager.ForceUpdate();
        return buffManager.GetActiveHandlers().Count;
    }

    [Test]
    public void Added_GivesTheBuffToEveryAlly()
    {
        GameObject first = CreateEntity("Berserker", Entity.EntityType.Computer);
        GameObject second = CreateEntity("Hexer", Entity.EntityType.Computer);

        _buff.Add(_owner, _owner);

        Assert.AreEqual(1, ActiveHandlers(first));
        Assert.AreEqual(1, ActiveHandlers(second));
    }

    [Test]
    public void Added_SparesTheHolderAndItsOpponents()
    {
        GameObject opponent = CreateEntity("Opponent", Entity.EntityType.Player);

        _buff.Add(_owner, _owner);

        Assert.AreEqual(0, ActiveHandlers(_owner));
        Assert.AreEqual(0, ActiveHandlers(opponent));
    }

    [Test]
    public void AllySpawnedLater_GetsTheBuffToo()
    {
        _buff.Add(_owner, _owner);
        GameObject summon = CreateEntity("Skeleton", Entity.EntityType.Computer, isListed: false);

        _entityManager.OnEntitySpawned.Invoke(summon.GetComponent<Entity>());

        Assert.AreEqual(1, ActiveHandlers(summon));
    }

    [Test]
    public void AllySpawnedTwice_GetsTheBuffOnce()
    {
        GameObject ally = CreateEntity("Berserker", Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);

        _entityManager.OnEntitySpawned.Invoke(ally.GetComponent<Entity>());

        Assert.AreEqual(1, ActiveHandlers(ally));
        CollectionAssert.AreEqual(new[] { "Add" }, _data.log);
    }

    [Test]
    public void Removed_TakesTheBuffBackFromEveryAlly()
    {
        GameObject ally = CreateEntity("Berserker", Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);
        ActiveHandlers(ally);

        _buff.Remove(_owner, _owner);

        Assert.AreEqual(0, ActiveHandlers(ally));
        CollectionAssert.AreEqual(new[] { "Add", "Remove" }, _data.log);
    }

    [Test]
    public void Removed_StopsGivingItToTheAlliesSpawnedLater()
    {
        _buff.Add(_owner, _owner);
        _buff.Remove(_owner, _owner);
        GameObject summon = CreateEntity("Skeleton", Entity.EntityType.Computer, isListed: false);

        _entityManager.OnEntitySpawned.Invoke(summon.GetComponent<Entity>());

        Assert.AreEqual(0, ActiveHandlers(summon));
    }

    [Test]
    public void Removed_AfterAnAllyDied_DoesNotThrow()
    {
        GameObject ally = CreateEntity("Berserker", Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);
        Object.DestroyImmediate(ally);

        Assert.DoesNotThrow(() => _buff.Remove(_owner, _owner));
    }
}

}
