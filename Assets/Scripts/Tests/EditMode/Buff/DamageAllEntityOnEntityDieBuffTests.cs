using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class DamageAllEntityOnEntityDieBuffTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    GameObject _owner;
    GameObject _dead;
    readonly List<GameObject> _victims = new List<GameObject>();
    readonly List<Object> _objects = new List<Object>();
    Attributes.RecordingConsumerFactory _damage;
    DamageAllEntityOnEntityDieBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        // The owner is the damage source: the resolver reads its attributes
        _owner = new GameObject("Owner");
        TestHelpers.CreateAttributeManager(_owner);
        _dead = CreateEntity("Dead", Entity.EntityType.Player);

        _damage = ScriptableObject.CreateInstance<Attributes.RecordingConsumerFactory>(); // -5 per consumer
        _objects.Add(_damage);
        _buff = new DamageAllEntityOnEntityDieBuff
        {
            data = new DamageAllEntityOnEntityDieBuffData { damageToAllEntity = _damage, entityType = Entity.EntityType.Player },
        };
    }

    [TearDown]
    public void TearDown()
    {
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
        // Some tests destroy them already
        if (_owner != null)
        {
            Object.DestroyImmediate(_owner);
        }
        if (_dead != null)
        {
            Object.DestroyImmediate(_dead);
        }
        foreach (GameObject victim in _victims)
        {
            Object.DestroyImmediate(victim);
        }
        _victims.Clear();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    // The buff reaches the manager through EntityManager.instance. Seeding the singleton with the
    // test's own manager keeps the getter from searching the open scene or creating a
    // DontDestroyOnLoad object.
    static void SetEntityManagerInstance(EntityManager entityManager)
    {
        FieldInfo field = typeof(Singleton<EntityManager>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, entityManager);
    }

    GameObject CreateEntity(string name, Entity.EntityType entityType)
    {
        GameObject go = new GameObject(name);
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            entity = go.AddComponent<Entity>();
        });
        // Initialized like in game, where every entity has its attributes
        TestHelpers.InvokePrivate(go.GetComponent<AttributeManager>(), "Awake");
        // On its own child, with its own AttributeManager holding the max health
        GameObject healthGo = new GameObject("Health");
        healthGo.transform.SetParent(go.transform);
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(entity, "_health", health);
        _entityManager.GetEntities(entityType).Add(go);
        return go;
    }

    GameObject CreateVictim(Entity.EntityType entityType = Entity.EntityType.Player)
    {
        GameObject victim = CreateEntity("Victim", entityType);
        _victims.Add(victim);
        return victim;
    }

    static ResourceAttribute Drain(GameObject entity)
    {
        ResourceAttribute health = entity.GetComponent<Entity>().health;
        TestHelpers.InvokePrivate(health, "Update");
        return health;
    }

    void Kill(GameObject entity)
    {
        _entityManager.OnEntityKilled.Invoke(entity.GetComponent<Entity>());
    }

    [Test]
    public void EntityKilled_DamagesEveryEntityOfTheTypeExceptTheDeadOne()
    {
        GameObject first = CreateVictim();
        GameObject second = CreateVictim();
        _buff.Add(_owner, _owner);

        Kill(_dead);

        Assert.AreEqual(95f, Drain(first).Value);
        Assert.AreEqual(95f, Drain(second).Value);
        Assert.AreEqual(100f, Drain(_dead).Value);
    }

    [Test]
    public void EntityKilled_DoesNotDamageTheOtherType()
    {
        GameObject computer = CreateVictim(Entity.EntityType.Computer);
        _buff.Add(_owner, _owner);

        Kill(_dead);

        Assert.AreEqual(100f, Drain(computer).Value);
    }

    [Test]
    public void EntityKilled_TheOwnerIsTheDamageSource()
    {
        GameObject victim = CreateVictim();
        _buff.Add(_owner, _owner);
        ResourceModifier processed = null;
        victim.GetComponent<Entity>().health.OnAllConsumerProcessed.AddListener((target, modifier, result) => processed = modifier);

        Kill(_dead);
        Drain(victim);

        Assert.AreSame(_owner, processed.source);
    }

    [Test]
    public void EntityKilled_DeadEntityDestroyedBeforeTheDamageIsApplied_StillDealsIt()
    {
        // The dead entity is destroyed at the end of the frame, before the victims process the damage
        GameObject victim = CreateVictim();
        _buff.Add(_owner, _owner);
        Kill(_dead);
        Object.DestroyImmediate(_dead);

        Assert.DoesNotThrow(() => Drain(victim));
        Assert.AreEqual(95f, victim.GetComponent<Entity>().health.Value);
    }

    [Test]
    public void EntityKilled_AfterTheOwnerIsDestroyed_DealsNothing()
    {
        GameObject victim = CreateVictim();
        _buff.Add(_owner, _owner);
        Object.DestroyImmediate(_owner);

        Kill(_dead);

        Assert.AreEqual(100f, Drain(victim).Value);
        Assert.AreEqual(0, _damage.createdCount);
    }

    [Test]
    public void EntityKilled_AfterTheOwnerIsDestroyed_StopsListening()
    {
        GameObject victim = CreateVictim();
        _buff.Add(_owner, _owner);
        Object.DestroyImmediate(_owner);
        Kill(_dead); // unsubscribes

        // Were the buff still listening, a live owner would make the next death deal damage
        _owner = new GameObject("NewOwner");
        TestHelpers.CreateAttributeManager(_owner);
        TestHelpers.SetPrivateField(_buff, "_owner", _owner);
        Kill(_dead);

        Assert.AreEqual(100f, Drain(victim).Value);
    }

    [Test]
    public void Trigger_ByDefault_IsAnyEntity()
    {
        // Assets saved before the trigger existed must keep reacting to every death
        Assert.AreEqual(DeathTrigger.AnyEntity, new DamageAllEntityOnEntityDieBuffData().trigger);
    }

    [Test]
    public void OwnerTrigger_AnotherEntityDies_DealsNothing()
    {
        GameObject victim = CreateVictim();
        _buff.data.trigger = DeathTrigger.Owner;
        _buff.Add(_owner, _owner);

        Kill(_dead);

        Assert.AreEqual(100f, Drain(victim).Value);
    }

    [Test]
    public void OwnerTrigger_TheOwnerDies_DamagesEveryEntityOfTheType()
    {
        GameObject victim = CreateVictim();
        GameObject kamikaze = CreateVictim(Entity.EntityType.Computer);
        _buff.data.trigger = DeathTrigger.Owner;
        _buff.Add(kamikaze, kamikaze);

        Kill(kamikaze);

        Assert.AreEqual(95f, Drain(victim).Value);
    }

    [Test]
    public void OwnerTrigger_TheOwnerDies_IsTheDamageSource()
    {
        GameObject victim = CreateVictim();
        GameObject kamikaze = CreateVictim(Entity.EntityType.Computer);
        _buff.data.trigger = DeathTrigger.Owner;
        _buff.Add(kamikaze, kamikaze);
        ResourceModifier processed = null;
        victim.GetComponent<Entity>().health.OnAllConsumerProcessed.AddListener((target, modifier, result) => processed = modifier);

        Kill(kamikaze);
        Drain(victim);

        Assert.AreSame(kamikaze, processed.source);
    }

    [Test]
    public void TargetOwnerOpponents_ComputerOwnerDies_DamagesPlayersOnly()
    {
        GameObject player = CreateVictim(Entity.EntityType.Player);
        GameObject ally = CreateVictim(Entity.EntityType.Computer);
        GameObject kamikaze = CreateVictim(Entity.EntityType.Computer);
        kamikaze.GetComponent<Entity>().entityType = Entity.EntityType.Computer;
        _buff.data.trigger = DeathTrigger.Owner;
        _buff.data.targetOwnerOpponents = true;
        _buff.Add(kamikaze, kamikaze);

        Kill(kamikaze);

        Assert.AreEqual(95f, Drain(player).Value);
        Assert.AreEqual(100f, Drain(ally).Value);
    }

    [Test]
    public void TargetOwnerOpponents_PlayerOwnerDies_DamagesComputersOnly()
    {
        // entityType says Player, but the owner's side decides: it never hits its own team
        GameObject ally = CreateVictim(Entity.EntityType.Player);
        GameObject computer = CreateVictim(Entity.EntityType.Computer);
        GameObject kamikaze = CreateVictim(Entity.EntityType.Player);
        kamikaze.GetComponent<Entity>().entityType = Entity.EntityType.Player;
        _buff.data.trigger = DeathTrigger.Owner;
        _buff.data.targetOwnerOpponents = true;
        _buff.Add(kamikaze, kamikaze);

        Kill(kamikaze);

        Assert.AreEqual(100f, Drain(ally).Value);
        Assert.AreEqual(95f, Drain(computer).Value);
    }

    [Test]
    public void TargetOwnerOpponents_ByDefault_IsFalse()
    {
        // Assets saved before the option existed keep using entityType
        Assert.IsFalse(new DamageAllEntityOnEntityDieBuffData().targetOwnerOpponents);
    }

    [Test]
    public void Remove_StopsDealingDamage()
    {
        GameObject victim = CreateVictim();
        _buff.Add(_owner, _owner);

        _buff.Remove(_owner, _owner);
        Kill(_dead);

        Assert.AreEqual(100f, Drain(victim).Value);
    }

    [Test]
    public void Stack_MultipliesTheDamage()
    {
        GameObject victim = CreateVictim();
        _buff.Add(_owner, _owner);

        _buff.Stack(_owner, _owner);
        Kill(_dead);

        Assert.AreEqual(90f, Drain(victim).Value); // 5 x 2 stacks
    }

    [Test]
    public void Unstack_ReducesTheDamageBack()
    {
        GameObject victim = CreateVictim();
        _buff.Add(_owner, _owner);
        _buff.Stack(_owner, _owner);

        _buff.Unstack(_owner, _owner);
        Kill(_dead);

        Assert.AreEqual(95f, Drain(victim).Value);
    }
}

}
