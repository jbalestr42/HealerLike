using Entities;
using NUnit.Framework;

namespace Attributes.Consumers
{

// A consumer computes its value from the source applying it, or from the target receiving it
public class ConsumerTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _source;
    Entity _target;

    [SetUp]
    public void SetUp()
    {
        _source = _units.Create(60f, 100f, "Source");
        _target = _units.Create(1000f, 1000f, "Target");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    Consumer CreateConsumer(ConsumerValueOwner valueOwner, Entity target)
    {
        ConsumerData data = new ConsumerData
        {
            value = new CurrentHealthValue { data = new CurrentHealthValueData { multiplier = 0.1f } },
            valueOwner = valueOwner,
        };
        return new Consumer { data = data, source = _source.gameObject, target = target != null ? target.gameObject : null };
    }

    [Test]
    public void ValueOwner_DefaultsToSource()
    {
        Assert.AreEqual(ConsumerValueOwner.Source, new ConsumerData().valueOwner);
    }

    [Test]
    public void GetValue_ValueOwnerSource_UsesTheSourceHealth()
    {
        Assert.AreEqual(-6f, CreateConsumer(ConsumerValueOwner.Source, _target).GetValue(), 0.0001f);
    }

    [Test]
    public void GetValue_ValueOwnerTarget_UsesTheTargetHealth()
    {
        Assert.AreEqual(-100f, CreateConsumer(ConsumerValueOwner.Target, _target).GetValue(), 0.0001f);
    }

    [Test]
    public void CanBeCritical_DefaultsToTrue()
    {
        Assert.IsTrue(new ConsumerData().canBeCritical);
        Assert.IsTrue(CreateConsumer(ConsumerValueOwner.Source, _target).canBeCritical);
    }

    [Test]
    public void CanBeCritical_FollowsTheData()
    {
        Consumer consumer = CreateConsumer(ConsumerValueOwner.Source, _target);
        consumer.data.canBeCritical = false;

        Assert.IsFalse(consumer.canBeCritical);
    }

    [Test]
    public void GetValue_ValueOwnerTarget_WithoutTarget_IsZero()
    {
        Assert.AreEqual(0f, CreateConsumer(ConsumerValueOwner.Target, null).GetValue(), 0.0001f);
    }
}

}
