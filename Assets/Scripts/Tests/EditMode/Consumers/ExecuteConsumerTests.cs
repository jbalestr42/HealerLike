using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Consumers
{

// Executioner: a hit kills the target outright below the health threshold
public class ExecuteConsumerTests
{
    readonly TestUnits _units = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    static ExecuteConsumer CreateConsumer(GameObject target)
    {
        return new ExecuteConsumer { data = new ExecuteConsumerData { healthThreshold = 0.1f, ignoreDamageReduction = true }, target = target };
    }

    [Test]
    public void TargetBelowThreshold_TakesItsWholeMaxHealth()
    {
        Entity target = _units.Create(5f, 200f);

        Assert.AreEqual(-200f, CreateConsumer(target.gameObject).GetValue(), 0.0001f);
    }

    [Test]
    public void TargetAtThreshold_TakesNothing()
    {
        Entity target = _units.Create(20f, 200f);

        Assert.AreEqual(0f, CreateConsumer(target.gameObject).GetValue(), 0.0001f);
    }

    [Test]
    public void TargetAboveThreshold_TakesNothing()
    {
        Entity target = _units.Create(150f, 200f);

        Assert.AreEqual(0f, CreateConsumer(target.gameObject).GetValue(), 0.0001f);
    }

    [Test]
    public void TargetBelowThreshold_IsKilledOnceTheHitIsProcessed()
    {
        Entity target = _units.Create(5f, 200f);
        ExecuteConsumerFactory factory = ScriptableObject.CreateInstance<ExecuteConsumerFactory>();
        factory.data = new ExecuteConsumerData { healthThreshold = 0.1f, ignoreDamageReduction = true };
        try
        {
            target.health.AddResourceModifier(ResourceModifier.Create(factory, target.gameObject, target.gameObject));
            TestUnits.Process(target.health);

            Assert.AreEqual(0f, target.health.Value, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(factory);
        }
    }

    [Test]
    public void NoTargetOrANonEntity_TakesNothing()
    {
        GameObject notAnEntity = new GameObject("Not an entity");
        try
        {
            Assert.AreEqual(0f, CreateConsumer(null).GetValue(), 0.0001f);
            Assert.AreEqual(0f, CreateConsumer(notAnEntity).GetValue(), 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(notAnEntity);
        }
    }
}

}
