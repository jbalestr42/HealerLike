using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Deliveries
{

public class ProjectileVisualObserverTests
{
    readonly ProjectileScene _scene = new ProjectileScene();

    [SetUp]
    public void SetUp()
    {
        _scene.Create();
    }

    [TearDown]
    public void TearDown()
    {
        _scene.Destroy();
    }

    [Test]
    public void Init_ClaimingSource_BeginsHidesAndEndsThroughTheInterface()
    {
        _scene.observer.Init(_scene.manager, DeliveryStyle.Arc);

        _scene.observer.Init(_scene.source);
        _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });
        _scene.observer.enabled = false;
        TestHelpers.InvokePrivate(_scene.observer, "OnDisable");

        Assert.AreEqual(DeliveryStyle.Arc, _scene.probe.style);
        Assert.AreEqual(1, _scene.probe.contacts);
        Assert.AreEqual(2, _scene.probe.ends); // the first shot of SetUp, then this one
        Assert.IsTrue(_scene.projectileObject.GetComponent<LineRenderer>().enabled);
    }

    [Test]
    public void Init_ClaimingSource_TintsTheTipThroughTheAccentInterface()
    {
        _scene.Shoot(_scene.CreateConsumer(10f));

        TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");

        // The manager's last token
        Assert.AreEqual(_scene.manager.NextDeliveryToken() - 1, _scene.observer.gestureToken);
        Assert.AreEqual(_scene.observer.gestureToken, _scene.probe.token);
        Assert.AreEqual(RenderTestAssets.LoadDeliveryVocabulary().palette.damage, _scene.probe.accent);
        Assert.IsFalse(_scene.IsFree());
        Assert.IsFalse(_scene.projectileObject.GetComponent<LineRenderer>().enabled);
    }

    [Test]
    public void LateUpdate_HealingConsumer_TintsTheTipLime()
    {
        _scene.Shoot(_scene.CreateConsumer(-5f));

        TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");

        Assert.AreEqual(RenderTestAssets.LoadDeliveryVocabulary().palette.heal, _scene.probe.accent);
    }

    [Test]
    public void LateUpdate_NoConsumer_LeavesTheTipAtRest()
    {
        int accents = _scene.probe.accents;

        TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");

        Assert.AreEqual(accents, _scene.probe.accents);
    }

    [Test]
    public void Init_MissingOrDecliningSource_FliesAsItsOwnTipAndRestoresRenderersAfter()
    {
        LineRenderer visible = _scene.projectileObject.GetComponent<LineRenderer>();
        GameObject child = new GameObject("HiddenRenderer", typeof(MeshRenderer));
        child.transform.SetParent(_scene.projectileObject.transform);
        Renderer hidden = child.GetComponent<Renderer>();
        hidden.enabled = false;
        _scene.probe.accepts = false;

        _scene.observer.Init(_scene.source);
        TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");
        _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });

        Assert.AreEqual(0, _scene.observer.gestureToken);
        Assert.IsTrue(_scene.IsFree());
        Assert.IsFalse(visible.enabled);
        Assert.IsFalse(hidden.enabled);
        Assert.AreEqual(0, _scene.probe.contacts);
        _scene.probe.accepts = true;
        _scene.observer.Init(_scene.source);
        Assert.IsFalse(_scene.IsFree());
        Assert.IsFalse(visible.enabled);
        int ends = _scene.probe.ends;
        _scene.probe.enabled = false;
        TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");
        Assert.AreEqual(ends + 1, _scene.probe.ends);
        Assert.IsTrue(_scene.IsFree()); // the claimer left, the shot keeps its tip
        Assert.IsFalse(visible.enabled);
        _scene.observer.enabled = false;
        TestHelpers.InvokePrivate(_scene.observer, "OnDisable");
        Assert.IsTrue(visible.enabled);
        Assert.IsFalse(hidden.enabled);
    }

    [Test]
    public void Init_UnclaimedShot_HidesItsRenderersAndDrawsTheTipAlongTheFlight()
    {
        Object.DestroyImmediate(_scene.probe);

        _scene.observer.Init(_scene.source);
        TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");

        Assert.IsTrue(_scene.IsFree());
        Assert.IsFalse(_scene.projectileObject.GetComponent<LineRenderer>().enabled);
        Assert.AreEqual(1, _scene.projectileObject.GetComponent<FreeShot>().tip.partCount);
    }

    [Test]
    public void Init_BounceGrantedByItem_DeliversAsBounce()
    {
        BounceProjectileBehaviourFactory bounce = _scene.CreateTracked<BounceProjectileBehaviourFactory>();
        bounce.data = new BounceProjectileBehaviourData();
        ProjectileBehaviourBuff buff = new ProjectileBehaviourBuff
        {
            data = new ProjectileBehaviourBuffData { projectileBehaviour = bounce }
        };
        buff.Add(_scene.source, _scene.projectileObject);

        _scene.observer.Init(_scene.source);

        Assert.AreEqual(DeliveryStyle.Bounce, _scene.observer.deliveryStyle);
        Assert.AreEqual(DeliveryStyle.Bounce, _scene.probe.style);
    }

    [Test]
    public void OnHit_SynchronousHitsAfterInit_RecordsOrderedContacts()
    {
        Assert.AreSame(_scene.first, _scene.observer.capturedTargetPoint);
        _scene.observer.Init(_scene.manager, DeliveryStyle.ChainSync);
        _scene.observer.Init(_scene.source);

        _scene.projectile.OnHit.Invoke(new OnHitData { source = _scene.source, target = _scene.first });
        _scene.projectile.OnHit.Invoke(new OnHitData { source = _scene.source, target = _scene.second });
        _scene.projectile.SetTarget(null);

        Assert.AreEqual(DeliveryStyle.ChainSync, _scene.probe.style);
        Assert.AreEqual(2, _scene.observer.contacts.Count);
        Assert.AreSame(_scene.first, _scene.observer.contacts[0].target);
        Assert.AreSame(_scene.second, _scene.observer.contacts[1].target);
        Assert.AreEqual(_scene.first.transform.position, _scene.observer.contacts[0].position);
        Assert.AreEqual(2, _scene.probe.contacts);
        Assert.IsTrue(_scene.projectile.enabled);
    }

    [Test]
    public void OnHit_LateRetargetThenDisable_KeepsContactAndUnsubscribes()
    {
        _scene.projectile.OnHit.AddListener(_ => _scene.projectile.SetTarget(_scene.second));

        _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });
        TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");

        Assert.AreSame(_scene.first, _scene.observer.contacts[0].target);
        Assert.AreSame(_scene.second, _scene.projectile.target);
        _scene.observer.enabled = false;
        TestHelpers.InvokePrivate(_scene.observer, "OnDisable");
        _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.second });
        Assert.AreEqual(1, _scene.observer.contacts.Count);
        Assert.AreEqual(0, _scene.observer.gestureToken);
    }

    [Test]
    public void Init_CalledTwice_DoesNotDuplicateListenerOrMoveProjectile()
    {
        Vector3 position = _scene.projectile.transform.position;

        _scene.observer.Init(_scene.source);
        _scene.observer.Init(_scene.source);
        _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });

        Assert.AreEqual(1, _scene.observer.contacts.Count);
        Assert.AreEqual(position, _scene.projectile.transform.position);
        Assert.AreSame(_scene.first, _scene.projectile.target);
    }

    [Test]
    public void Init_UnclaimedThrownShot_KeepsItsOwnVisualWithoutMovingIt()
    {
        _scene.probe.accepts = false;
        _scene.observer.Init(_scene.manager, DeliveryStyle.Thrown);

        _scene.observer.Init(_scene.source);

        Assert.AreEqual(0, _scene.observer.gestureToken);
        Assert.AreEqual(DeliveryStyle.Thrown, _scene.observer.deliveryStyle);
        Assert.IsTrue(_scene.projectileObject.GetComponent<LineRenderer>().enabled);
        Assert.AreEqual(Vector3.zero, _scene.projectile.transform.position);
    }

    [Test]
    public void OnHit_ChainProjectile_DrawsAThreadBetweenItsTargets()
    {
        GameObject chainGo = new GameObject("Chain");
        ChainLightningProjectile chain = chainGo.AddComponent<ChainLightningProjectile>();
        ProjectileVisualObserver observer = chainGo.AddComponent<ProjectileVisualObserver>();
        SpellVisualSink sink = SpellSinkFixture.Add(_scene.managerGo);
        TestHelpers.SetPrivateField(_scene.manager, "_spellSink", sink);
        observer.Init(_scene.manager, DeliveryStyle.ChainSync);
        observer.projectile = chain;
        observer.Init(_scene.source);

        chain.OnHit.Invoke(new OnHitData { target = _scene.first });
        chain.OnHit.Invoke(new OnHitData { target = _scene.second });

        Assert.AreEqual(1, sink.impactCount);
        TestHelpers.InvokePrivate(observer, "OnDestroy");
        TestHelpers.InvokePrivate(sink, "OnDestroy");
        Object.DestroyImmediate(chainGo);
    }

    [Test]
    public void OnHit_AreaShot_DropsOnePodAtTheFirstContact()
    {
        _scene.projectileObject.AddComponent<AreaOfEffectProjectileBehaviour>();
        _scene.observer.Init(_scene.source);

        _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });
        _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.second });

        TipDrop[] drops = Object.FindObjectsByType<TipDrop>();
        foreach (TipDrop drop in drops)
        {
            Object.DestroyImmediate(drop.gameObject);
        }
        Assert.AreEqual(1, drops.Length);
    }
}

}
