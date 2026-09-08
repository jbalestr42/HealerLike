using NUnit.Framework;
using UnityEngine;

namespace Buff
{

/// <summary>Records the last ResourceModifier it was hit with, so tests can inspect it.</summary>
public class FakeAttackable : MonoBehaviour, IAttackable
{
    [System.NonSerialized] public ResourceModifier lastResourceModifier;
    public GameObject owner => gameObject;

    public void OnHit(ResourceModifier resourceModifier) => lastResourceModifier = resourceModifier;
    public void OnHit(OnHitData onHitData) => lastResourceModifier = onHitData.resourceModifier;
}

public class FakeConsumer : AConsumer
{
    public override float GetValue() => 1f;
    public override bool ignoreDamageReduction => false;
    public override bool ignoreConsumerPrevention => false;
}

public class FakeConsumerFactory : AConsumerFactory
{
    public override AConsumer GetConsumer(GameObject source, GameObject target) => new FakeConsumer();
}

public class ApplyConsumerBuffTests
{
    GameObject _source;
    GameObject _target;
    FakeAttackable _attackable;
    ApplyConsumerBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
        _attackable = _target.AddComponent<FakeAttackable>();

        _buff = new ApplyConsumerBuff
        {
            data = new ApplyConsumerBuffData
            {
                consumerFactory = ScriptableObject.CreateInstance<FakeConsumerFactory>()
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_buff.data.consumerFactory);
    }

    [Test]
    public void Instant_DefaultStacks_AppliesMultiplierOne()
    {
        _buff.Instant(_source, _target);

        Assert.AreEqual(1f, _attackable.lastResourceModifier.multiplier);
        Assert.AreEqual(1, _attackable.lastResourceModifier.consumers.Count);
        Assert.AreEqual(_source, _attackable.lastResourceModifier.source);
    }

    [Test]
    public void Stack_IncreasesMultiplierAppliedOnInstant()
    {
        _buff.Stack(_source, _target);

        _buff.Instant(_source, _target);

        Assert.AreEqual(2f, _attackable.lastResourceModifier.multiplier);
    }

    [Test]
    public void Unstack_DecreasesMultiplierAppliedOnInstant()
    {
        _buff.Stack(_source, _target);
        _buff.Stack(_source, _target); // 3

        _buff.Unstack(_source, _target); // back to 2
        _buff.Instant(_source, _target);

        Assert.AreEqual(2f, _attackable.lastResourceModifier.multiplier);
    }
}

}
