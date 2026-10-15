using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Attributes.Consumers
{

// Executioner: its hits deal twice their damage to a target under half health
public class WoundedTargetConsumerTests
{
    readonly TestUnits _units = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    static WoundedTargetConsumerData CreateData()
    {
        return new WoundedTargetConsumerData
        {
            value = new FlatValue { data = new FlatValueData { value = 10f } },
            healthThreshold = 0.5f,
            multiplier = 2f,
            ignoreDamageReduction = true,
        };
    }

    static WoundedTargetConsumer CreateConsumer(GameObject source, GameObject target)
    {
        return new WoundedTargetConsumer { data = CreateData(), source = source, target = target };
    }

    [Test]
    public void TargetUnderTheThreshold_TakesTheValueMultiplied()
    {
        Entity source = _units.Create(100f, 100f, "Executioner");
        Entity target = _units.Create(99f, 200f);

        Assert.AreEqual(-20f, CreateConsumer(source.gameObject, target.gameObject).GetValue(), 0.0001f);
    }

    [Test]
    public void TargetAtTheThreshold_TakesTheValue()
    {
        Entity source = _units.Create(100f, 100f, "Executioner");
        Entity target = _units.Create(100f, 200f);

        Assert.AreEqual(-10f, CreateConsumer(source.gameObject, target.gameObject).GetValue(), 0.0001f);
    }

    [Test]
    public void TargetAboveTheThreshold_TakesTheValue()
    {
        Entity source = _units.Create(100f, 100f, "Executioner");
        Entity target = _units.Create(180f, 200f);

        Assert.AreEqual(-10f, CreateConsumer(source.gameObject, target.gameObject).GetValue(), 0.0001f);
    }

    // The health checked is the one of the target, never the one of the source
    [Test]
    public void WoundedSource_HealthyTarget_TakesTheValue()
    {
        Entity source = _units.Create(10f, 100f, "Executioner");
        Entity target = _units.Create(200f, 200f);

        Assert.AreEqual(-10f, CreateConsumer(source.gameObject, target.gameObject).GetValue(), 0.0001f);
    }

    [Test]
    public void TargetUnderTheThreshold_LosesTheMultipliedValueOnceTheHitIsProcessed()
    {
        Entity source = _units.Create(100f, 100f, "Executioner");
        Entity target = _units.Create(80f, 200f);
        WoundedTargetConsumerFactory factory = ScriptableObject.CreateInstance<WoundedTargetConsumerFactory>();
        factory.data = CreateData();
        try
        {
            target.health.AddResourceModifier(ResourceModifier.Create(factory, source.gameObject, target.gameObject));
            TestUnits.Process(target.health);

            Assert.AreEqual(60f, target.health.Value, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(factory);
        }
    }

    // Nothing to tell it is wounded: the value of the source, never multiplied
    [Test]
    public void NoTarget_TheValueIsNotMultiplied()
    {
        Entity source = _units.Create(100f, 100f, "Executioner");

        Assert.AreEqual(-10f, CreateConsumer(source.gameObject, null).GetValue(), 0.0001f);
    }
}

}
