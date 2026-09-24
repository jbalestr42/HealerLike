using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Deliveries
{

public class ProjectileVisualObserverTests
{
    GameObject _source;
    GameObject _first;
    GameObject _second;
    GameObject _model;
    GameObject _projectileObject;
    Projectile _projectile;
    ProjectileVisualObserver _observer;
    DeliveryProbe _probe;
    GameObject _managerGo;
    RenderManager _manager;
    readonly List<Object> _scriptableObjects = new List<Object>();

    static Entity EntityFixture(GameObject go)
    {
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
        TestHelpers.SetPrivateField(entity, "_targetPoint", go);
        return entity;
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    // A positive flat value is damage, a negative one heals
    ConsumerFactory CreateConsumer(float value)
    {
        ConsumerFactory consumer = CreateTracked<ConsumerFactory>();
        FlatValue flat = new FlatValue();
        flat.data = new FlatValueData { value = value };
        consumer.data = new ConsumerData { value = flat };
        return consumer;
    }

    // An unclaimed shot flies as its own tip
    bool IsFree()
    {
        FreeShot shot = _projectileObject.GetComponent<FreeShot>();
        return shot && shot.enabled;
    }

    // Shoots the projectile again carrying the consumers, the observer reads them in its Init
    void Shoot(params AConsumerFactory[] consumers)
    {
        _projectile.Init(_source, _first, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>(consumers));
    }

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        Entity entity = EntityFixture(_source);
        _first = new GameObject("First");
        EntityFixture(_first);
        _first.transform.position = Vector3.one;
        _second = new GameObject("Second");
        EntityFixture(_second);
        _second.transform.position = Vector3.right * 2f;
        _model = new GameObject("Model");
        _model.transform.SetParent(_source.transform, false);
        EntityModel entityModel = _model.AddComponent<EntityModel>();
        TestHelpers.SetPrivateField(entity, "_model", entityModel);
        _probe = _model.AddComponent<DeliveryProbe>();
        _projectileObject = new GameObject("Projectile", typeof(LineRenderer));
        _projectile = _projectileObject.AddComponent<Projectile>();
        _observer = _projectileObject.AddComponent<ProjectileVisualObserver>();
        _managerGo = new GameObject("RenderManager");
        _manager = _managerGo.AddComponent<RenderManager>();
        TestHelpers.SetPrivateField(_manager, "_deliveryVocabulary", RenderTestAssets.LoadDeliveryVocabulary());
        TestHelpers.SetPrivateField(_manager, "_meshes", RenderTestAssets.LoadMeshes());
        _observer.Init(_manager, null);
        Shoot();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_observer, "OnDestroy");
        foreach (FreeShot shot in _projectileObject.GetComponents<FreeShot>())
        {
            TestHelpers.InvokePrivate(shot, "OnDestroy");
        }

        Object.DestroyImmediate(_projectileObject);
        Object.DestroyImmediate(_managerGo);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_first);
        Object.DestroyImmediate(_second);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }

        _scriptableObjects.Clear();
    }

    [Test]
    public void Init_ClaimingSource_BeginsHidesAndEndsThroughTheInterface()
    {
        _observer.Init(_manager, new ProjectileLook { style = DeliveryStyle.Arc });

        _observer.Init(_source);
        _projectile.OnHit.Invoke(new OnHitData { target = _first });
        _observer.enabled = false;
        TestHelpers.InvokePrivate(_observer, "OnDisable");

        Assert.AreEqual(DeliveryStyle.Arc, _probe.style);
        Assert.AreEqual(1, _probe.contacts);
        Assert.AreEqual(2, _probe.ends); // the first shot of SetUp, then this one
        Assert.IsTrue(_projectileObject.GetComponent<LineRenderer>().enabled);
    }

    [Test]
    public void Init_ClaimingSource_TintsTheTipThroughTheAccentInterface()
    {
        Shoot(CreateConsumer(10f));

        TestHelpers.InvokePrivate(_observer, "LateUpdate");

        Assert.AreEqual(_manager.NextDeliveryToken() - 1, _observer.gestureToken); // the manager's last token
        Assert.AreEqual(_observer.gestureToken, _probe.token);
        Assert.AreEqual(RenderTestAssets.LoadDeliveryVocabulary().palette.damage, _probe.accent);
        Assert.IsFalse(IsFree());
        Assert.IsFalse(_projectileObject.GetComponent<LineRenderer>().enabled);
    }

    [Test]
    public void LateUpdate_HealingConsumer_TintsTheTipLime()
    {
        Shoot(CreateConsumer(-5f));

        TestHelpers.InvokePrivate(_observer, "LateUpdate");

        Assert.AreEqual(RenderTestAssets.LoadDeliveryVocabulary().palette.heal, _probe.accent);
    }

    [Test]
    public void LateUpdate_NoConsumer_LeavesTheTipAtRest()
    {
        int accents = _probe.accents;

        TestHelpers.InvokePrivate(_observer, "LateUpdate");

        Assert.AreEqual(accents, _probe.accents);
    }

    [Test]
    public void Init_MissingOrDecliningSource_FliesAsItsOwnTipAndRestoresRenderersAfter()
    {
        LineRenderer visible = _projectileObject.GetComponent<LineRenderer>();
        GameObject child = new GameObject("HiddenRenderer", typeof(MeshRenderer));
        child.transform.SetParent(_projectileObject.transform);
        Renderer hidden = child.GetComponent<Renderer>();
        hidden.enabled = false;
        _probe.accepts = false;

        _observer.Init(_source);
        TestHelpers.InvokePrivate(_observer, "LateUpdate");
        _projectile.OnHit.Invoke(new OnHitData { target = _first });

        Assert.AreEqual(0, _observer.gestureToken);
        Assert.IsTrue(IsFree());
        Assert.IsFalse(visible.enabled);
        Assert.IsFalse(hidden.enabled);
        Assert.AreEqual(0, _probe.contacts);
        _probe.accepts = true;
        _observer.Init(_source);
        Assert.IsFalse(IsFree());
        Assert.IsFalse(visible.enabled);
        int ends = _probe.ends;
        _probe.enabled = false;
        TestHelpers.InvokePrivate(_observer, "LateUpdate");
        Assert.AreEqual(ends + 1, _probe.ends);
        Assert.IsTrue(IsFree()); // the claimer left, the shot keeps its tip
        Assert.IsFalse(visible.enabled);
        _observer.enabled = false;
        TestHelpers.InvokePrivate(_observer, "OnDisable");
        Assert.IsTrue(visible.enabled);
        Assert.IsFalse(hidden.enabled);
    }

    [Test]
    public void Init_UnclaimedShot_HidesItsRenderersAndDrawsTheTipAlongTheFlight()
    {
        Object.DestroyImmediate(_probe);

        _observer.Init(_source);
        TestHelpers.InvokePrivate(_observer, "LateUpdate");

        Assert.IsTrue(IsFree());
        Assert.IsFalse(_projectileObject.GetComponent<LineRenderer>().enabled);
        Assert.AreEqual(1, _projectileObject.GetComponent<FreeShot>().tip.partCount);
    }

    [Test]
    public void Init_BounceGrantedByItem_DeliversAsBounce()
    {
        BounceProjectileBehaviourFactory bounce = CreateTracked<BounceProjectileBehaviourFactory>();
        bounce.data = new BounceProjectileBehaviourData();
        ProjectileBehaviourBuff buff = new ProjectileBehaviourBuff
        {
            data = new ProjectileBehaviourBuffData { projectileBehaviour = bounce }
        };
        buff.Add(_source, _projectileObject);

        _observer.Init(_source);

        Assert.AreEqual(DeliveryStyle.Bounce, _observer.deliveryStyle);
        Assert.AreEqual(DeliveryStyle.Bounce, _probe.style);
    }

    [Test]
    public void OnHit_SynchronousHitsAfterInit_RecordsOrderedContacts()
    {
        Assert.AreSame(_first, _observer.capturedTargetPoint);
        _observer.Init(_manager, new ProjectileLook { preserveContactPath = true });
        _observer.Init(_source);

        _projectile.OnHit.Invoke(new OnHitData { source = _source, target = _first });
        _projectile.OnHit.Invoke(new OnHitData { source = _source, target = _second });
        _projectile.SetTarget(null);

        Assert.AreEqual(DeliveryStyle.ChainSync, _probe.style);
        Assert.AreEqual(2, _observer.contacts.Count);
        Assert.AreSame(_first, _observer.contacts[0].target);
        Assert.AreSame(_second, _observer.contacts[1].target);
        Assert.AreEqual(_first.transform.position, _observer.contacts[0].position);
        Assert.AreEqual(2, _probe.contacts);
        Assert.IsTrue(_projectile.enabled);
    }

    [Test]
    public void OnHit_LateRetargetThenDisable_KeepsContactAndUnsubscribes()
    {
        _projectile.OnHit.AddListener(_ => _projectile.SetTarget(_second));

        _projectile.OnHit.Invoke(new OnHitData { target = _first });
        TestHelpers.InvokePrivate(_observer, "LateUpdate");

        Assert.AreSame(_first, _observer.contacts[0].target);
        Assert.AreSame(_second, _projectile.target);
        _observer.enabled = false;
        TestHelpers.InvokePrivate(_observer, "OnDisable");
        _projectile.OnHit.Invoke(new OnHitData { target = _second });
        Assert.AreEqual(1, _observer.contacts.Count);
        Assert.AreEqual(0, _observer.gestureToken);
    }

    [Test]
    public void Init_CalledTwice_DoesNotDuplicateListenerOrMoveProjectile()
    {
        Vector3 position = _projectile.transform.position;

        _observer.Init(_source);
        _observer.Init(_source);
        _projectile.OnHit.Invoke(new OnHitData { target = _first });

        Assert.AreEqual(1, _observer.contacts.Count);
        Assert.AreEqual(position, _projectile.transform.position);
        Assert.AreSame(_first, _projectile.target);
    }

    [Test]
    public void Init_UnclaimedThrownShot_KeepsItsOwnVisualWithoutMovingIt()
    {
        _probe.accepts = false;
        _observer.Init(_manager, new ProjectileLook { style = DeliveryStyle.Thrown });

        _observer.Init(_source);

        Assert.AreEqual(0, _observer.gestureToken);
        Assert.AreEqual(DeliveryStyle.Thrown, _observer.deliveryStyle);
        Assert.IsTrue(_projectileObject.GetComponent<LineRenderer>().enabled);
        Assert.AreEqual(Vector3.zero, _projectile.transform.position);
    }
}

}
