using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Projectiles.Effects
{

public class AreaOfEffectTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    GameObject _source;
    AreaOfEffect _areaOfEffect;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        _source = new GameObject("Source");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            _source.AddComponent<Entity>();
        });

        _areaOfEffect = new GameObject("AreaOfEffect").AddComponent<AreaOfEffect>();
        _areaOfEffect.source = _source;
    }

    [TearDown]
    public void TearDown()
    {
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_areaOfEffect.gameObject);
    }

    // AreaOfEffect.Start reaches the manager through EntityManager.instance. Seeding the singleton
    // with the test's own manager keeps the getter from searching the open scene or creating a
    // DontDestroyOnLoad object.
    static void SetEntityManagerInstance(EntityManager entityManager)
    {
        FieldInfo field = typeof(Singleton<EntityManager>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, entityManager);
    }

    [Test]
    public void Start_InvokesOnAreaOfEffectStarted_WithTheAreaOfEffect()
    {
        AreaOfEffect startedAreaOfEffect = null;
        _entityManager.OnAreaOfEffectStarted.AddListener(areaOfEffect => startedAreaOfEffect = areaOfEffect);

        TestHelpers.InvokePrivate(_areaOfEffect, "Start");

        Assert.AreSame(_areaOfEffect, startedAreaOfEffect);
    }
}

}
