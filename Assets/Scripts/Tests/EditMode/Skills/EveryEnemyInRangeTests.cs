using System.Collections.Generic;
using System.Reflection;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// Every enemy within the range of a unit, without limit (e.g. the Ravager slamming the whole team)
public class EveryEnemyInRangeTests
{
    GameObject _managersGo;
    EntityManager _entityManager;
    readonly TestUnits _units = new TestUnits();
    Entity _unit;

    [SetUp]
    public void SetUp()
    {
        _managersGo = new GameObject("Managers");
        _entityManager = _managersGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetInstance<EntityManager>(_entityManager);
        // Asked whether a target is marked: none is
        SetInstance<MarkManager>(_managersGo.AddComponent<MarkManager>());

        _unit = CreateUnit("Ravager", Entity.EntityType.Computer, Vector3.zero);
        _unit.GetComponent<AttributeManager>().GetOrAdd(AttributeType.Range).BaseValue = 10f;
        _unit.GetComponent<AttributeManager>().GetOrAdd(AttributeType.Range).Update();
    }

    [TearDown]
    public void TearDown()
    {
        SetInstance<EntityManager>(null);
        SetInstance<MarkManager>(null);
        Object.DestroyImmediate(_managersGo);
        _units.DestroyAll();
    }

    // The singletons seeded with the test's own managers: the getters neither search the open scene nor create a
    // DontDestroyOnLoad object
    static void SetInstance<T>(T instance) where T : Singleton<T>
    {
        FieldInfo field = typeof(Singleton<T>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, instance);
    }

    Entity CreateUnit(string name, Entity.EntityType entityType, Vector3 position)
    {
        Entity unit = _units.Create(100f, 100f, name);
        unit.entityType = entityType;
        unit.transform.position = position;
        _entityManager.GetEntities(entityType).Add(unit.gameObject);
        return unit;
    }

    [Test]
    public void Find_EveryEnemyInRange_NotTheAlliesNorTheOnesTooFar()
    {
        Entity near = CreateUnit("Near", Entity.EntityType.Player, new Vector3(3f, 0f, 0f));
        Entity edge = CreateUnit("Edge", Entity.EntityType.Player, new Vector3(0f, 0f, 10f));
        CreateUnit("Far", Entity.EntityType.Player, new Vector3(20f, 0f, 0f));
        CreateUnit("Ally", Entity.EntityType.Computer, new Vector3(1f, 0f, 0f));

        List<GameObject> targets = new EveryEnemyInRange(null).Find(_unit.gameObject);

        CollectionAssert.AreEquivalent(new[] { near.gameObject, edge.gameObject }, targets);
    }

    [Test]
    public void Find_WithoutLimit()
    {
        for (int i = 0; i < 5; i++)
        {
            CreateUnit("Enemy " + i, Entity.EntityType.Player, new Vector3(i, 0f, 0f));
        }

        Assert.AreEqual(5, new EveryEnemyInRange(new List<ATargetValidatorFactory>()).Find(_unit.gameObject).Count);
    }

    // A copy: changing it leaves the next search untouched
    [Test]
    public void Find_ReturnsACopy()
    {
        CreateUnit("Enemy", Entity.EntityType.Player, Vector3.one);
        EveryEnemyInRange everyEnemy = new EveryEnemyInRange(null);

        everyEnemy.Find(_unit.gameObject).Clear();

        Assert.AreEqual(1, everyEnemy.Find(_unit.gameObject).Count);
    }
}

}
