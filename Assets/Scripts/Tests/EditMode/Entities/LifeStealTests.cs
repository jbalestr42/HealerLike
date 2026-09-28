using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// Builds entities holding only a health, without the full Entity.Init()
public class TestUnits
{
    readonly List<GameObject> _objects = new List<GameObject>();

    public Entity Create(float value, float max, string name = "Unit")
    {
        GameObject go = new GameObject(name);
        _objects.Add(go);
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init()), only its health is used
        TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
        // The AttributeManager required by Entity misses its Awake() in edit mode: a heal it sends reads it
        TestHelpers.InvokePrivate(go.GetComponent<AttributeManager>(), "Awake");
        // Entity already requires an AttributeManager: the health lives on its own object
        GameObject healthGo = new GameObject(name + " Health");
        _objects.Add(healthGo);
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(healthGo, AttributeType.HealthMax, max);
        health.SetValue(value);
        TestHelpers.SetPrivateField(entity, "_health", health);
        return entity;
    }

    public static List<ResourceModifier> GetPendingModifiers(Entity entity)
    {
        return TestHelpers.GetPrivateField<List<ResourceModifier>>(entity.health, "_resourceModifiers");
    }

    public static void Process(ResourceAttribute health)
    {
        TestHelpers.InvokePrivate(health, "Update");
    }

    public void DestroyAll()
    {
        foreach (GameObject go in _objects)
        {
            Object.DestroyImmediate(go);
        }
        _objects.Clear();
    }
}

public class LifeStealTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _source;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        TestHelpers.CreateAttributeManager(_source);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_source);
    }

    [Test]
    public void FindMostWounded_PicksTheLowestHealthPercent()
    {
        Entity full = _units.Create(100f, 100f);
        Entity wounded = _units.Create(30f, 100f);
        Entity halfTank = _units.Create(100f, 200f);

        Assert.AreSame(wounded, LifeSteal.FindMostWounded(new List<GameObject> { full.gameObject, wounded.gameObject, halfTank.gameObject }));
    }

    [Test]
    public void FindMostWounded_SkipsDeadAndMissingUnits()
    {
        Entity dead = _units.Create(0f, 100f);
        Entity wounded = _units.Create(80f, 100f);

        Assert.AreSame(wounded, LifeSteal.FindMostWounded(new List<GameObject> { null, dead.gameObject, wounded.gameObject }));
        Assert.IsNull(LifeSteal.FindMostWounded(new List<GameObject>()));
    }

    [Test]
    public void HealMostWounded_HealsItByTheAmount()
    {
        Entity full = _units.Create(100f, 100f);
        Entity wounded = _units.Create(30f, 100f);

        Entity healed = LifeSteal.HealMostWounded(_source, new List<GameObject> { full.gameObject, wounded.gameObject }, 12f);
        TestUnits.Process(wounded.health);

        Assert.AreSame(wounded, healed);
        Assert.AreEqual(42f, wounded.health.Value, 0.0001f);
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(full));
    }

    [Test]
    public void HealMostWounded_IgnoresArmorAndInvincibility()
    {
        Entity wounded = _units.Create(30f, 100f);
        wounded.health.preventConsumers = true;

        LifeSteal.HealMostWounded(_source, new List<GameObject> { wounded.gameObject }, 10f);
        TestUnits.Process(wounded.health);

        Assert.AreEqual(40f, wounded.health.Value, 0.0001f);
    }

    [Test]
    public void HealMostWounded_WithoutAmount_HealsNobody()
    {
        Entity wounded = _units.Create(30f, 100f);

        Assert.IsNull(LifeSteal.HealMostWounded(_source, new List<GameObject> { wounded.gameObject }, 0f));
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(wounded));
    }
}

}
