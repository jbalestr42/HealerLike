using System.Collections.Generic;
using System.Reflection;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// When the holder dies, its allies with a tag get a buff (e.g. each Acolyte handing its power down to the Archon)
public class ApplyBuffToAlliesOnDeathBuffTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();
    GameplayTag _tag;
    BuffHandlerFactory _legacy;
    Entity _holder;
    Entity _master;
    ApplyBuffToAlliesOnDeathBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        _tag = Track(ScriptableObject.CreateInstance<GameplayTag>());
        _tag.name = "Archon";
        _legacy = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _legacy.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            maxStacks = 1,
            buffFactoryList = new List<ABuffFactory>(),
        };

        _holder = CreateUnit("Acolyte", Entity.EntityType.Computer);
        _master = CreateUnit("Archon", Entity.EntityType.Computer);
        _master.AddTag(_tag);

        _buff = new ApplyBuffToAlliesOnDeathBuff { data = new ApplyBuffToAlliesOnDeathBuffData { buffHandlerFactory = _legacy, tag = _tag } };
        _buff.Add(_holder.gameObject, _holder.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        if (_holder != null)
        {
            _buff.Remove(_holder.gameObject, _holder.gameObject);
        }
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
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

    // An entity on the board of its side
    Entity CreateUnit(string name, Entity.EntityType entityType)
    {
        Entity unit = _units.Create(100f, 100f, name);
        unit.entityType = entityType;
        unit.GetComponent<BuffManager>().isEnabled = true;
        _entityManager.GetEntities(entityType).Add(unit.gameObject);
        return unit;
    }

    void Kill(Entity entity)
    {
        _entityManager.OnEntityKilled.Invoke(entity);
        foreach (GameObject unit in _entityManager.GetEntities(Entity.EntityType.Computer))
        {
            unit.GetComponent<BuffManager>().ForceUpdate();
        }
        foreach (GameObject unit in _entityManager.GetEntities(Entity.EntityType.Player))
        {
            unit.GetComponent<BuffManager>().ForceUpdate();
        }
    }

    static bool HasLegacy(Entity entity, BuffHandlerFactory legacy) => entity.GetComponent<BuffManager>().HasHandler(legacy);

    [Test]
    public void HolderKilled_TheAllyWithTheTagGetsTheBuff()
    {
        Kill(_holder);

        Assert.IsTrue(HasLegacy(_master, _legacy));
    }

    [Test]
    public void HolderKilled_AnAllyWithoutTheTag_GetsNothing()
    {
        Entity other = CreateUnit("Other Acolyte", Entity.EntityType.Computer);

        Kill(_holder);

        Assert.IsFalse(HasLegacy(other, _legacy));
    }

    [Test]
    public void HolderKilled_AnOpponentWithTheTag_GetsNothing()
    {
        Entity opponent = CreateUnit("Opponent", Entity.EntityType.Player);
        opponent.AddTag(_tag);

        Kill(_holder);

        Assert.IsFalse(HasLegacy(opponent, _legacy));
    }

    [Test]
    public void HolderKilled_WithoutTag_EveryAllyGetsTheBuff()
    {
        _buff.data.tag = null;
        Entity other = CreateUnit("Other Acolyte", Entity.EntityType.Computer);

        Kill(_holder);

        Assert.IsTrue(HasLegacy(_master, _legacy));
        Assert.IsTrue(HasLegacy(other, _legacy));
    }

    [Test]
    public void HolderKilled_NeverGivesTheBuffToItself()
    {
        _holder.AddTag(_tag);

        Kill(_holder);

        Assert.IsFalse(HasLegacy(_holder, _legacy));
    }

    [Test]
    public void AnotherAllyKilled_GivesNothing()
    {
        Entity other = CreateUnit("Other Acolyte", Entity.EntityType.Computer);

        Kill(other);

        Assert.IsFalse(HasLegacy(_master, _legacy));
    }

    // Given from the ally itself, the buff stays once the dead holder is gone
    [Test]
    public void HolderKilledThenDestroyed_TheAllyKeepsTheBuff()
    {
        Kill(_holder);
        _buff.Remove(_holder.gameObject, _holder.gameObject);
        _entityManager.GetEntities(Entity.EntityType.Computer).Remove(_holder.gameObject);
        Object.DestroyImmediate(_holder.gameObject);
        _master.GetComponent<BuffManager>().ForceUpdate();

        Assert.IsTrue(HasLegacy(_master, _legacy));
    }

    [Test]
    public void Removed_StopsListening()
    {
        _buff.Remove(_holder.gameObject, _holder.gameObject);

        Kill(_holder);

        Assert.IsFalse(HasLegacy(_master, _legacy));
    }
}

}
