using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Each death of an ally of the holder gives it a buff
public class ApplyBuffOnAllyDeathBuffTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    GameObject _owner;
    BuffManager _ownerBuffs;
    FakeBuffData _data;
    readonly List<GameObject> _entities = new List<GameObject>();
    readonly List<Object> _objects = new List<Object>();
    ApplyBuffOnAllyDeathBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        _owner = CreateEntity("Sniper", Entity.EntityType.Computer);
        // Required by Entity, already added with it
        _ownerBuffs = _owner.GetComponent<BuffManager>();
        _ownerBuffs.isEnabled = true;

        _data = new FakeBuffData();
        FakeBuffFactory buffFactory = ScriptableObject.CreateInstance<FakeBuffFactory>();
        buffFactory.data = _data;
        BuffHandlerFactory handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 6f,
            maxStacks = 1,
            buffFactoryList = new List<ABuffFactory> { buffFactory },
        };
        _objects.Add(buffFactory);
        _objects.Add(handlerFactory);

        _buff = new ApplyBuffOnAllyDeathBuff { data = new ApplyBuffOnAllyDeathBuffData { buffHandlerFactory = handlerFactory } };
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

    GameObject CreateEntity(string name, Entity.EntityType entityType)
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
        return go;
    }

    void Kill(GameObject entity)
    {
        _entityManager.OnEntityKilled.Invoke(entity.GetComponent<Entity>());
        _ownerBuffs.ForceUpdate();
    }

    [Test]
    public void AllyKilled_GivesTheBuffToTheHolder()
    {
        GameObject ally = CreateEntity("Kamikaze", Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);

        Kill(ally);

        CollectionAssert.AreEqual(new[] { "Add" }, _data.log);
        Assert.AreEqual(1, _ownerBuffs.GetActiveHandlers().Count);
    }

    [Test]
    public void TwoAlliesKilled_RefreshTheBuffWithoutStackingOverItsMax()
    {
        GameObject first = CreateEntity("First", Entity.EntityType.Computer);
        GameObject second = CreateEntity("Second", Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);

        Kill(first);
        Kill(second);

        CollectionAssert.AreEqual(new[] { "Add" }, _data.log);
        Assert.AreEqual(1, _ownerBuffs.GetActiveHandlers().Count);
    }

    [Test]
    public void OpponentKilled_GivesNothing()
    {
        GameObject opponent = CreateEntity("Opponent", Entity.EntityType.Player);
        _buff.Add(_owner, _owner);

        Kill(opponent);

        CollectionAssert.IsEmpty(_data.log);
    }

    [Test]
    public void HolderKilled_GivesNothing()
    {
        _buff.Add(_owner, _owner);

        Kill(_owner);

        CollectionAssert.IsEmpty(_data.log);
    }

    [Test]
    public void Removed_StopsListening()
    {
        GameObject ally = CreateEntity("Kamikaze", Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);

        _buff.Remove(_owner, _owner);
        Kill(ally);

        CollectionAssert.IsEmpty(_data.log);
    }

    [Test]
    public void HolderDestroyed_AllyKilled_DoesNotThrow()
    {
        GameObject ally = CreateEntity("Kamikaze", Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);
        Object.DestroyImmediate(_owner);

        Assert.DoesNotThrow(() => _entityManager.OnEntityKilled.Invoke(ally.GetComponent<Entity>()));
    }
}

}
