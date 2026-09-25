using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Projectiles
{

public class AreaOfEffectProjectileBehaviourTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    GameObject _areaOfEffectPrefab;
    GameObject _source;
    GameObject _target;
    Projectile _projectile;
    AreaOfEffectProjectileBehaviour _behaviour;
    Buff.FakeConsumerFactory _projectileConsumer;
    AreaOfEffect _spawnedArea;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);
        _entityManager.OnProjectileSpawned.AddListener(go => _spawnedArea = go.GetComponent<AreaOfEffect>());

        _areaOfEffectPrefab = new GameObject("AreaOfEffect");
        _areaOfEffectPrefab.AddComponent<AreaOfEffect>();

        _source = new GameObject("Source");
        _target = new GameObject("Target");
        _target.transform.position = new Vector3(3f, 0f, 1f);

        _projectileConsumer = ScriptableObject.CreateInstance<Buff.FakeConsumerFactory>();
        _projectile = new GameObject("Projectile").AddComponent<Projectile>();
        TestHelpers.SetPrivateField(_projectile, "_onHitConsumers", new List<AConsumerFactory> { _projectileConsumer });

        _behaviour = _projectile.gameObject.AddComponent<AreaOfEffectProjectileBehaviour>();
        _behaviour.data = new AreaOfEffectProjectileBehaviourData { areaOfEffectPrefab = _areaOfEffectPrefab.GetComponent<AreaOfEffect>(), radius = 1.5f };
        _behaviour.projectile = _projectile;
        _behaviour.Init(_source);
    }

    [TearDown]
    public void TearDown()
    {
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
        Object.DestroyImmediate(_areaOfEffectPrefab);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_projectile.gameObject);
        Object.DestroyImmediate(_projectileConsumer);
    }

    // The behaviour spawns the area through EntityManager.instance. Seeding the singleton with the
    // test's own manager keeps the getter from searching the open scene or creating a
    // DontDestroyOnLoad object.
    static void SetEntityManagerInstance(EntityManager entityManager)
    {
        FieldInfo field = typeof(Singleton<EntityManager>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, entityManager);
    }

    void Hit()
    {
        _projectile.OnHit.Invoke(new OnHitData { source = _source, target = _target });
    }

    [Test]
    public void OnHit_SpawnsTheAreaOnTheHitTarget()
    {
        Hit();

        Assert.IsNotNull(_spawnedArea);
        Assert.AreEqual(_target.transform.position, _spawnedArea.transform.position);
        Assert.AreSame(_source, _spawnedArea.source);
        Assert.AreSame(_target, _spawnedArea.target);
        Assert.AreEqual(1.5f, _spawnedArea.radius);
    }

    [Test]
    public void OnHit_GivesTheProjectileOnHitConsumersToTheArea()
    {
        Hit();

        CollectionAssert.AreEqual(new List<AConsumerFactory> { _projectileConsumer }, _spawnedArea.extraOnHitConsumers);
    }

    [Test]
    public void OnHit_GivesTheAreaItsOwnCopyOfTheConsumers()
    {
        Hit();

        Assert.AreNotSame(_projectile.onHitConsumers, _spawnedArea.extraOnHitConsumers);
    }

    [Test]
    public void OnHit_Twice_SpawnsASingleArea()
    {
        int spawnCount = 0;
        _entityManager.OnProjectileSpawned.AddListener(go => spawnCount++);

        Hit();
        Hit();

        Assert.AreEqual(1, spawnCount);
    }
}

}
