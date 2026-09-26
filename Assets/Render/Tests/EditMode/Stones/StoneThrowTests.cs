using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneThrowTests
{
    GameObject _owner;
    GameObject _projectile;
    GameObject _fxObject;
    ResourceAttribute _health;
    CreatureRecipe _recipe;
    Material _material;
    StoneBody _body;
    StoneThrow _throw;
    StoneEffects _fx;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Stone");
        _projectile = new GameObject("Projectile");
        _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        _fx = RenderTestAssets.CreateStoneEffects();
        _fxObject = _fx.gameObject;
        _recipe = RenderTestAssets.CreateStoneRecipe();
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        Entity entity = RenderTestAssets.CreateStoneEntity(_owner, _health);
        _body = RenderTestAssets.CreateStoneBody(_owner, entity, _recipe, _material);
        _throw = _body.gameObject.AddComponent<StoneThrow>();
        _body.Init(_health, 15, _fx);
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_throw, "OnDestroy");
        TestHelpers.InvokePrivate(_body, "OnDestroy");
        TestHelpers.InvokePrivate(_body.GetComponent<CreatureBuilder>(), "OnDestroy");
        if (_fx != null)
        {
            TestHelpers.InvokePrivate(_fx, "OnDestroy");
        }
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_projectile);
        Object.DestroyImmediate(_fxObject);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    [Test]
    public void BeginDelivery_RigWithoutArms_ClaimsWhatTheBuilderRefuses()
    {
        CreatureBuilder builder = _body.GetComponent<CreatureBuilder>();

        bool isBuilderClaiming = builder.BeginDelivery(1, DeliveryStyle.Direct, _projectile.transform, Vector3.one);
        bool isThrowClaiming = _throw.BeginDelivery(1, DeliveryStyle.Direct, _projectile.transform, Vector3.one);

        Assert.IsFalse(isBuilderClaiming);
        Assert.IsTrue(isThrowClaiming);
    }

    [TestCase(DeliveryStyle.Direct)]
    [TestCase(DeliveryStyle.Arc)]
    [TestCase(DeliveryStyle.Rigid)]
    [TestCase(DeliveryStyle.Swarm)]
    [TestCase(DeliveryStyle.Bounce)]
    [TestCase(DeliveryStyle.ChainSync)]
    [TestCase(DeliveryStyle.Thrown)]
    public void BeginDelivery_AnyStyle_ThrowsAShardFromTheHeadThatFollowsTheProjectile(DeliveryStyle style)
    {
        Vector3 head = _body.parts[3].GetComponent<Renderer>().bounds.center;

        Assert.IsTrue(_throw.BeginDelivery(1, style, _projectile.transform, Vector3.forward));
        Assert.IsFalse(_throw.BeginDelivery(1, style, _projectile.transform, Vector3.forward));
        Transform shard = _fxObject.GetComponentInChildren<MeshFilter>().transform;
        Assert.AreEqual(head, shard.position);

        _projectile.transform.position = new Vector3(4f, 3f, 2f);
        TestHelpers.InvokePrivate(_throw, "LateUpdate");
        Assert.AreEqual(_projectile.transform.position, shard.position);

        _throw.ContactDelivery(1, Vector3.one * 7f, null);
        Assert.IsFalse(shard.gameObject.activeSelf);
        Assert.That(_fx.liveCount, Is.InRange(StoneEmitters.MinThrownChips, StoneEmitters.MinThrownChips + 2));
    }

    [Test]
    public void BeginDelivery_CollapsedStone_Refuses()
    {
        _body.Collapse(null);

        bool isClaimed = _throw.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        Assert.IsFalse(isClaimed);
    }

    [Test]
    public void Enable_False_EndsTheLiveDeliveriesUntilInitAgain()
    {
        _throw.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        Transform shard = _fxObject.GetComponentInChildren<MeshFilter>().transform;

        _throw.Enable(false);

        Assert.IsFalse(shard.gameObject.activeSelf);
        Assert.IsFalse(_throw.BeginDelivery(2, DeliveryStyle.Thrown, _projectile.transform, Vector3.one));
        _body.Init(_health, 15, _fx);
        Assert.IsTrue(_throw.BeginDelivery(2, DeliveryStyle.Thrown, _projectile.transform, Vector3.one));
    }

    [Test]
    public void OnDisable_LiveDeliveries_LeaksNothing()
    {
        _throw.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        _throw.BeginDelivery(2, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        _throw.enabled = false;
        TestHelpers.InvokePrivate(_throw, "OnDisable");

        Assert.AreEqual(0, _fxObject.GetComponentsInChildren<MeshFilter>().Length); // both shards went back
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void LateUpdate_EffectsOwnerDestroyedBeforeThrower_ReleasesTheRevokedDelivery()
    {
        _throw.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        Object.DestroyImmediate(_fxObject);

        Assert.DoesNotThrow(() => TestHelpers.InvokePrivate(_throw, "LateUpdate"));
        Assert.DoesNotThrow(() => _throw.EndDelivery(1));
    }

    [Test]
    public void LateUpdate_EffectsDisabledAndReused_OldDeliveryCannotMoveTheNewShard()
    {
        _throw.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        _fx.enabled = false;
        TestHelpers.InvokePrivate(_fx, "OnDisable");
        _fx.enabled = true;
        StoneFragmentPool.ShardLease current = _fx.BorrowShard(RenderTestAssets.LoadMeshes().pyramid, Color.red);
        current.shard.position = Vector3.up * 7f;
        _projectile.transform.position = Vector3.right * 5f;

        TestHelpers.InvokePrivate(_throw, "LateUpdate");

        Assert.IsNotNull(current.shard);
        Assert.AreEqual(Vector3.up * 7f, current.shard.position);
        current.Dispose();
    }

    [Test]
    public void LateUpdate_DestroyedProjectile_ReleasesTheDeliveryWithoutContact()
    {
        _throw.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        Object.DestroyImmediate(_projectile);
        TestHelpers.InvokePrivate(_throw, "LateUpdate");

        Assert.AreEqual(0, _fxObject.GetComponentsInChildren<MeshFilter>().Length); // the shard went back
        Assert.AreEqual(0, _fx.liveCount);
    }
}

}
