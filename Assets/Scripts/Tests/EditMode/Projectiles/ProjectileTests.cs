using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Projectiles
{

public class ProjectileTests
{
    GameObject _source;
    GameObject _target;
    Projectile _projectile;
    readonly List<Object> _scriptableObjects = new List<Object>();
    readonly Entities.TestUnits _units = new Entities.TestUnits();
    readonly List<Object> _objects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            _target.AddComponent<Entity>();
        });
        _projectile = new GameObject("Projectile").AddComponent<Projectile>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_projectile.gameObject);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    [Test]
    public void OnHitConsumers_ReturnsTheConsumersGivenToInit()
    {
        List<AConsumerFactory> onHitConsumers = new List<AConsumerFactory> { CreateTracked<ConsumerFactory>() };

        _projectile.Init(_source, _target, new List<ABuffHandlerFactory>(), onHitConsumers);

        Assert.AreSame(onHitConsumers, _projectile.onHitConsumers);
    }

    // A lasting buff (e.g. the one of the Vulnerable status)
    BuffHandlerFactory CreateEffect()
    {
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory>(),
            tags = new List<GameplayTag>(),
        };
        return handler;
    }

    static BuffManager.BuffHandlerData FindGiven(Entity hit, ABuffHandlerFactory effect)
    {
        return hit.GetComponent<BuffManager>().GetActiveHandlers().Find(handler => handler.buffHandlerFactory == effect);
    }

    // Hits a unit with health, shot by an attacking unit carrying the on hit effects given
    Entity Hit(List<ABuffHandlerFactory> onHitEffects, out GameObject attacker, ABuffHandlerFactory attackerOnHitEffect = null)
    {
        Entity hit = _units.Create(100f, 100f, "Hit");
        hit.GetComponent<BuffManager>().isEnabled = true;
        // Set by Entity.Init(), not called here: what the effects of the hit are given through
        TestHelpers.SetPrivateField(hit, "_buffManager", hit.GetComponent<BuffManager>());
        attacker = new GameObject("Attacker");
        _objects.Add(attacker);
        GameObject attackerGo = attacker;
        Entity attackerEntity = null;
        TestHelpers.WithLoggingDisabled(() => attackerEntity = attackerGo.AddComponent<Entity>());
        if (attackerOnHitEffect != null)
        {
            attackerEntity.AddOnHitEffect(attackerOnHitEffect);
        }

        _projectile.Init(attacker, hit.gameObject, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>(), onHitEffects);
        _projectile.ApplyOnHit(hit.gameObject, attacker);
        hit.GetComponent<BuffManager>().ForceUpdate();
        return hit;
    }

    [Test]
    public void OnHit_GivesItsEffectsToTheUnitHit_FromTheShooter()
    {
        BuffHandlerFactory effect = CreateEffect();

        Entity hit = Hit(new List<ABuffHandlerFactory> { effect }, out GameObject attacker);

        BuffManager.BuffHandlerData given = FindGiven(hit, effect);
        Assert.IsNotNull(given);
        Assert.AreSame(attacker, given.source);
    }

    [Test]
    public void OnHit_GivesItsEffectsAlongWithTheOnesOfTheShooter()
    {
        BuffHandlerFactory projectileEffect = CreateEffect();
        BuffHandlerFactory shooterEffect = CreateEffect();

        Entity hit = Hit(new List<ABuffHandlerFactory> { projectileEffect }, out _, shooterEffect);

        Assert.IsNotNull(FindGiven(hit, projectileEffect));
        Assert.IsNotNull(FindGiven(hit, shooterEffect));
    }

    [Test]
    public void OnHit_WithoutEffects_GivesNone()
    {
        Entity hit = Hit(null, out _);

        CollectionAssert.IsEmpty(hit.GetComponent<BuffManager>().GetActiveHandlers());
        CollectionAssert.IsEmpty(_projectile.onHitEffects);
    }

    [Test]
    public void SetTarget_DestroyedUnit_LeavesNoTarget_AndTheProjectileIsDestroyed()
    {
        GameObject killed = new GameObject("Killed");
        Object.DestroyImmediate(killed);

        Assert.DoesNotThrow(() => _projectile.SetTarget(killed));

        Assert.IsTrue(_projectile.target == null);
        Assert.IsNull(_projectile.targetPoint);
        Assert.IsTrue(_projectile.ShouldDestroyProjectile());
    }
}

}
