using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class ApplyConsumerOnEntitiesBuffTests
{
    GameObject _holder;
    readonly List<GameObject> _entities = new List<GameObject>();
    ApplyConsumerOnEntitiesBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _holder = new GameObject("Holder");
        _buff = new ApplyConsumerOnEntitiesBuff
        {
            data = new ApplyConsumerOnEntitiesBuffData
            {
                consumerFactory = ScriptableObject.CreateInstance<FakeConsumerFactory>()
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_holder);
        foreach (GameObject entity in _entities)
        {
            Object.DestroyImmediate(entity);
        }
        _entities.Clear();
        Object.DestroyImmediate(_buff.data.consumerFactory);
    }

    GameObject CreateEntity()
    {
        GameObject go = new GameObject("Entity");
        _entities.Add(go);
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init()), only its health is used
        TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
        // Entity already requires an AttributeManager: the health lives on its own object
        GameObject healthGo = new GameObject("Health");
        _entities.Add(healthGo);
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(entity, "_health", health);
        return go;
    }

    List<ResourceModifier> GetPendingModifiers(GameObject entity)
    {
        return TestHelpers.GetPrivateField<List<ResourceModifier>>(entity.GetComponent<Entity>().health, "_resourceModifiers");
    }

    [Test]
    public void ApplyTo_AppliesTheConsumerToEveryEntityFromTheHolder()
    {
        List<GameObject> entities = new List<GameObject> { CreateEntity(), CreateEntity() };

        _buff.ApplyTo(entities, _holder);

        foreach (GameObject entity in entities)
        {
            List<ResourceModifier> modifiers = GetPendingModifiers(entity);
            Assert.AreEqual(1, modifiers.Count);
            Assert.AreSame(_holder, modifiers[0].source);
            Assert.AreEqual(1f, modifiers[0].multiplier);
            Assert.AreEqual(1, modifiers[0].consumers.Count);
        }
    }

    [Test]
    public void Stack_IncreasesTheMultiplier()
    {
        GameObject entity = CreateEntity();

        _buff.Stack(_holder, _holder);
        _buff.ApplyTo(new List<GameObject> { entity }, _holder);

        Assert.AreEqual(2f, GetPendingModifiers(entity)[0].multiplier);
    }

    [Test]
    public void Unstack_DecreasesTheMultiplier()
    {
        GameObject entity = CreateEntity();

        _buff.Stack(_holder, _holder);
        _buff.Stack(_holder, _holder);
        _buff.Unstack(_holder, _holder);
        _buff.ApplyTo(new List<GameObject> { entity }, _holder);

        Assert.AreEqual(2f, GetPendingModifiers(entity)[0].multiplier);
    }

    [Test]
    public void ApplyTo_WithoutEntity_DoesNothing()
    {
        Assert.DoesNotThrow(() => _buff.ApplyTo(new List<GameObject>(), _holder));
    }
}

}
