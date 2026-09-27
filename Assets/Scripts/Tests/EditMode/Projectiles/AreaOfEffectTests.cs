using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Projectiles
{

/// <summary>An attacker whose on hit consumers are set by the test.</summary>
public class FakeAttacker : MonoBehaviour, IAttacker
{
    [System.NonSerialized] public List<AConsumerFactory> onHitConsumers = new List<AConsumerFactory>();
    [System.NonSerialized] public List<ABuffHandlerFactory> onHitEffects = new List<ABuffHandlerFactory>();

    public void AddOnHitConsumer(AConsumerFactory consumerFactory) => onHitConsumers.Add(consumerFactory);
    public List<AConsumerFactory> GetOnHitConsumers() => onHitConsumers;
    public void RemoveOnHitConsumer(AConsumerFactory onHitConsumer) => onHitConsumers.Remove(onHitConsumer);

    public void AddOnHitEffect(ABuffHandlerFactory onHitEffect) => onHitEffects.Add(onHitEffect);
    public List<ABuffHandlerFactory> GetOnHitEffects() => onHitEffects;
    public void RemoveOnHitEffect(ABuffHandlerFactory onHitEffect) => onHitEffects.Remove(onHitEffect);
}

/// <summary>Keeps the last OnHitData it was hit with.</summary>
public class RecordingAttackable : MonoBehaviour, IAttackable
{
    [System.NonSerialized] public OnHitData lastOnHitData;
    public GameObject owner => gameObject;

    public void OnHit(ResourceModifier resourceModifier) { }
    public void OnHit(OnHitData onHitData) => lastOnHitData = onHitData;
}

public class AreaOfEffectTests
{
    GameObject _source;
    GameObject _target;
    FakeAttacker _attacker;
    RecordingAttackable _attackable;
    AreaOfEffect _areaOfEffect;
    Attributes.RecordingConsumerFactory _sourceConsumer;
    Attributes.RecordingConsumerFactory _extraConsumer;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _attacker = _source.AddComponent<FakeAttacker>();
        _target = new GameObject("Target");
        _attackable = _target.AddComponent<RecordingAttackable>();

        _sourceConsumer = ScriptableObject.CreateInstance<Attributes.RecordingConsumerFactory>();
        _extraConsumer = ScriptableObject.CreateInstance<Attributes.RecordingConsumerFactory>();

        // Start() is not called in EditMode, so the area never looks for targets by itself
        _areaOfEffect = new GameObject("AreaOfEffect").AddComponent<AreaOfEffect>();
        _areaOfEffect.source = _source;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_areaOfEffect.gameObject);
        Object.DestroyImmediate(_sourceConsumer);
        Object.DestroyImmediate(_extraConsumer);
    }

    [Test]
    public void ExtraOnHitConsumers_AreEmptyByDefault()
    {
        Assert.IsNotNull(_areaOfEffect.extraOnHitConsumers);
        Assert.IsEmpty(_areaOfEffect.extraOnHitConsumers);
    }

    [Test]
    public void HitTarget_AppliesTheSourceOnHitConsumers()
    {
        _attacker.onHitConsumers.Add(_sourceConsumer);

        _areaOfEffect.HitTarget(_target);

        Assert.AreEqual(1, _attackable.lastOnHitData.resourceModifier.consumers.Count);
        Assert.AreEqual(1, _sourceConsumer.createdCount);
    }

    [Test]
    public void HitTarget_AppliesTheExtraOnHitConsumersToo()
    {
        _attacker.onHitConsumers.Add(_sourceConsumer);
        _areaOfEffect.extraOnHitConsumers = new List<AConsumerFactory> { _extraConsumer };

        _areaOfEffect.HitTarget(_target);

        Assert.AreEqual(2, _attackable.lastOnHitData.resourceModifier.consumers.Count);
        Assert.AreEqual(1, _extraConsumer.createdCount);
    }

    [Test]
    public void HitTarget_WithOnlyExtraOnHitConsumers_StillDealsThem()
    {
        _areaOfEffect.extraOnHitConsumers = new List<AConsumerFactory> { _extraConsumer };

        _areaOfEffect.HitTarget(_target);

        Assert.AreEqual(1, _attackable.lastOnHitData.resourceModifier.consumers.Count);
    }

    [Test]
    public void HitTarget_CreatesTheConsumersForTheSourceAndTheHitTarget()
    {
        _areaOfEffect.extraOnHitConsumers = new List<AConsumerFactory> { _extraConsumer };

        _areaOfEffect.HitTarget(_target);

        AConsumer consumer = _attackable.lastOnHitData.resourceModifier.consumers[0];
        Assert.AreSame(_source, consumer.source);
        Assert.AreSame(_target, consumer.target);
    }

    [Test]
    public void HitTarget_FillsTheOnHitData()
    {
        _areaOfEffect.HitTarget(_target);

        OnHitData onHitData = _attackable.lastOnHitData;
        Assert.AreSame(_source, onHitData.source);
        Assert.AreSame(_source, onHitData.resourceModifier.source);
        Assert.AreSame(_attacker, onHitData.attacker);
        Assert.AreSame(_attackable, onHitData.attackable);
        Assert.AreSame(_target, onHitData.target);
    }

    [Test]
    public void HitTarget_OnSomethingNotAttackable_DoesNothing()
    {
        GameObject wall = new GameObject("Wall");
        _areaOfEffect.extraOnHitConsumers = new List<AConsumerFactory> { _extraConsumer };

        Assert.DoesNotThrow(() => _areaOfEffect.HitTarget(wall));
        Assert.AreEqual(0, _extraConsumer.createdCount);

        Object.DestroyImmediate(wall);
    }
}

}
