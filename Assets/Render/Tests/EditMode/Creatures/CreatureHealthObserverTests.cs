using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class CreatureHealthObserverTests : CreatureRigFixture
{
    CreatureHealthObserver _observer;
    ResourceAttribute _health;

    [SetUp]
    public void SetUpObserver()
    {
        _observer = new CreatureHealthObserver();
        _health = TestHelpers.CreateResourceAttribute(_parent, AttributeType.HealthMax, 100f);
    }

    [TearDown]
    public void TearDownObserver()
    {
        _observer.Dispose();
    }

    [Test]
    public void Init_DamageSubscription_DetachesAndCanObserveAgain()
    {
        _observer.Init(_health, _rig);
        _observer.Init(_health, _rig);
        _health.OnAllConsumerProcessed.Invoke(_parent, new ResourceModifier(), -1f, false);
        _rig.Tick(0f, 0.01f, ground);
        Assert.Greater(Mathf.Abs(_rig.armRotation.z), 0.001f);
        _rig.Tick(0f, 1f, ground);
        _observer.Dispose();
        _observer.Dispose();
        _health.OnAllConsumerProcessed.Invoke(_parent, new ResourceModifier(), -1f, false);
        _rig.Tick(0f, 0f, ground);
        Assert.AreEqual(Quaternion.identity, _rig.armRotation);
        _observer.Init(_health, _rig);
        foreach (float value in new[] { 0f, 1f, float.NaN, float.NegativeInfinity })
        {
            _health.OnAllConsumerProcessed.Invoke(_parent, new ResourceModifier(), value, false);
            _rig.Tick(0f, 0f, ground);
            Assert.AreEqual(Quaternion.identity, _rig.armRotation);
        }

        _health.OnAllConsumerProcessed.Invoke(_parent, new ResourceModifier(), -1f, false);
        _rig.Tick(0f, 0.01f, ground);
        Assert.Greater(Mathf.Abs(_rig.armRotation.z), 0.001f);
    }
}
}
