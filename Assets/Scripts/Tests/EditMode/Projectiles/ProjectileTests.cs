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
}

}
