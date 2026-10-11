using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// Lit fuse: after its delay of battle, the holder hurts every opponent and dies
public class FuseSkillTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    GameObject _kamikaze;
    FuseSkill _skill;
    Attributes.RecordingConsumerFactory _explosion;
    readonly List<GameObject> _entities = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        _kamikaze = CreateEntity("Kamikaze", Entity.EntityType.Computer);
        _explosion = ScriptableObject.CreateInstance<Attributes.RecordingConsumerFactory>(); // -5 per consumer
        _skill = _kamikaze.AddComponent<FuseSkill>();
        _skill.data = new FuseSkillData { onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(), minDelay = 10f, maxDelay = 10f, explosion = _explosion };
    }

    [TearDown]
    public void TearDown()
    {
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
        foreach (GameObject entity in _entities)
        {
            Object.DestroyImmediate(entity);
        }
        _entities.Clear();
        Object.DestroyImmediate(_explosion);
    }

    // The skill reaches the manager through EntityManager.instance: seeding the singleton with the test's own
    // manager keeps the getter from searching the open scene or creating a DontDestroyOnLoad object
    static void SetEntityManagerInstance(EntityManager entityManager)
    {
        FieldInfo field = typeof(Singleton<EntityManager>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, entityManager);
    }

    GameObject CreateEntity(string name, Entity.EntityType entityType)
    {
        GameObject go = new GameObject(name);
        _entities.Add(go);
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            entity = go.AddComponent<Entity>();
        });
        entity.entityType = entityType;
        // On its own child, with its own AttributeManager holding the max health
        GameObject healthGo = new GameObject("Health");
        healthGo.transform.SetParent(go.transform);
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(entity, "_health", health);
        _entityManager.GetEntities(entityType).Add(go);
        return go;
    }

    static float Health(GameObject entity)
    {
        ResourceAttribute health = entity.GetComponent<Entity>().health;
        TestHelpers.InvokePrivate(health, "Update");
        return health.Value;
    }

    [Test]
    public void BeforeTheDelay_NothingHappens()
    {
        GameObject opponent = CreateEntity("Opponent", Entity.EntityType.Player);

        _skill.Tick(_kamikaze, 9.9f);

        Assert.IsFalse(_skill.hasExploded);
        Assert.AreEqual(100f, Health(opponent));
        Assert.AreEqual(100f, Health(_kamikaze));
    }

    [Test]
    public void AfterTheDelay_HurtsEveryOpponent()
    {
        GameObject first = CreateEntity("First", Entity.EntityType.Player);
        GameObject second = CreateEntity("Second", Entity.EntityType.Player);

        _skill.Tick(_kamikaze, 4f);
        _skill.Tick(_kamikaze, 6f);

        Assert.IsTrue(_skill.hasExploded);
        Assert.AreEqual(95f, Health(first));
        Assert.AreEqual(95f, Health(second));
    }

    [Test]
    public void AfterTheDelay_SparesTheHolderAllies()
    {
        GameObject ally = CreateEntity("Ally", Entity.EntityType.Computer);

        _skill.Tick(_kamikaze, 10f);

        Assert.AreEqual(100f, Health(ally));
    }

    [Test]
    public void AfterTheDelay_TheHolderDies()
    {
        _skill.Tick(_kamikaze, 10f);

        Assert.AreEqual(0f, Health(_kamikaze));
    }

    [Test]
    public void AfterTheDelay_TheHolderDiesEvenWhenInvincible()
    {
        _kamikaze.GetComponent<Entity>().health.preventConsumers = true;

        _skill.Tick(_kamikaze, 10f);

        Assert.AreEqual(0f, Health(_kamikaze));
    }

    [Test]
    public void AfterTheDelay_TheHolderIsTheSourceOfTheExplosion()
    {
        GameObject opponent = CreateEntity("Opponent", Entity.EntityType.Player);
        ResourceModifier processed = null;
        opponent.GetComponent<Entity>().health.OnAllConsumerProcessed.AddListener((target, modifier, result) => processed = modifier);

        _skill.Tick(_kamikaze, 10f);
        Health(opponent);

        Assert.AreSame(_kamikaze, processed.source);
    }

    [Test]
    public void Explodes_OnlyOnce()
    {
        GameObject opponent = CreateEntity("Opponent", Entity.EntityType.Player);

        _skill.Tick(_kamikaze, 10f);
        _skill.Tick(_kamikaze, 10f);

        Assert.AreEqual(95f, Health(opponent));
        Assert.AreEqual(1, _explosion.createdCount);
    }

    [Test]
    public void Reset_LightsTheFuseAgain()
    {
        _skill.Tick(_kamikaze, 8f);

        _skill.Reset();
        _skill.Tick(_kamikaze, 8f);

        Assert.IsFalse(_skill.hasExploded);
    }

    [Test]
    public void Delay_IsDrawnBetweenTheMinAndTheMax()
    {
        _skill.data.minDelay = 10f;
        _skill.data.maxDelay = 13f;
        for (int i = 0; i < 50; i++)
        {
            _skill.Reset();

            Assert.That(_skill.delay, Is.InRange(10f, 13f));
        }
    }

    [Test]
    public void Delay_StaysTheSameForTheWholeFuse()
    {
        _skill.data.minDelay = 10f;
        _skill.data.maxDelay = 13f;
        float delay = _skill.delay;

        _skill.Tick(_kamikaze, 1f);

        Assert.AreEqual(delay, _skill.delay);
    }

    [Test]
    public void LongerDelay_WaitsForIt()
    {
        _skill.data.minDelay = 13f;
        _skill.data.maxDelay = 13f;

        _skill.Tick(_kamikaze, 12.9f);
        Assert.IsFalse(_skill.hasExploded);

        // 12.9 + 0.1 falls just short of 13 in floats
        _skill.Tick(_kamikaze, 0.2f);
        Assert.IsTrue(_skill.hasExploded);
    }

    [Test]
    public void CooldownProgress_IsThePartOfTheFuseLeftToBurn()
    {
        Assert.AreEqual(1f, _skill.cooldownProgress, 0.0001f);

        _skill.Tick(_kamikaze, 2.5f);

        Assert.AreEqual(0.75f, _skill.cooldownProgress, 0.0001f);
        Assert.AreEqual(10f, _skill.cooldownDuration);
    }
}

}
