using NUnit.Framework;
using UnityEngine;

namespace Attributes
{

public class RecordingConsumer : AConsumer
{
    public override float GetValue() => -5f;
    public override bool ignoreDamageReduction => false;
    public override bool ignoreConsumerPrevention => false;
}

/// <summary>Counts the consumers it creates, keeping the source/target it was given.</summary>
public class RecordingConsumerFactory : AConsumerFactory
{
    [System.NonSerialized] public int createdCount = 0;

    public override AConsumer GetConsumer(GameObject source, GameObject target)
    {
        createdCount++;
        return new RecordingConsumer { source = source, target = target };
    }
}

public class ResourceModifierTests
{
    GameObject _source;
    GameObject _target;
    RecordingConsumerFactory _factory;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
        _factory = ScriptableObject.CreateInstance<RecordingConsumerFactory>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_factory);
    }

    [Test]
    public void Create_HoldsASingleConsumerFromTheFactory()
    {
        ResourceModifier resourceModifier = ResourceModifier.Create(_factory, _source, _target);

        Assert.AreEqual(1, _factory.createdCount);
        Assert.AreEqual(1, resourceModifier.consumers.Count);
        Assert.IsInstanceOf<RecordingConsumer>(resourceModifier.consumers[0]);
    }

    [Test]
    public void Create_GivesSourceAndTargetToTheConsumer()
    {
        ResourceModifier resourceModifier = ResourceModifier.Create(_factory, _source, _target);

        Assert.AreSame(_source, resourceModifier.consumers[0].source);
        Assert.AreSame(_target, resourceModifier.consumers[0].target);
    }

    [Test]
    public void Create_SetsTheModifierSource()
    {
        ResourceModifier resourceModifier = ResourceModifier.Create(_factory, _source, _target);

        Assert.AreSame(_source, resourceModifier.source);
    }

    [Test]
    public void Create_DefaultMultiplier_IsOne()
    {
        ResourceModifier resourceModifier = ResourceModifier.Create(_factory, _source, _target);

        Assert.AreEqual(1f, resourceModifier.multiplier);
    }

    [Test]
    public void Create_UsesTheGivenMultiplier()
    {
        ResourceModifier resourceModifier = ResourceModifier.Create(_factory, _source, _target, 3f);

        Assert.AreEqual(3f, resourceModifier.multiplier);
    }

    [Test]
    public void Create_ReturnsANewModifierEachTime()
    {
        ResourceModifier first = ResourceModifier.Create(_factory, _source, _target);
        ResourceModifier second = ResourceModifier.Create(_factory, _source, _target);

        Assert.AreNotSame(first, second);
        Assert.AreNotSame(first.consumers, second.consumers);
    }
}

}
