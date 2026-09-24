using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneProjectileImpactBridgeTests
{
    GameObject _target;
    GameObject _projectileObject;
    CreatureRecipe _recipe;
    Material _material;
    StoneBody _body;
    StoneEffects _fx;

    [SetUp]
    public void SetUp()
    {
        _target = new GameObject("Target");
        _projectileObject = new GameObject("Projectile");
        _recipe = RenderTestAssets.CreateStoneRecipe();
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        _fx = RenderTestAssets.CreateStoneEffects();
    }

    [TearDown]
    public void TearDown()
    {
        if (_body != null)
        {
            TestHelpers.InvokePrivate(_body, "OnDestroy");
            TestHelpers.InvokePrivate(_body.GetComponent<CreatureBuilder>(), "OnDestroy");
            _body = null;
        }
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_fx.gameObject);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_projectileObject);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    [Test]
    public void OnHit_ProjectileTargetCleared_UsesTheCallbackTargetUntilDisabled()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
        Entity entity = RenderTestAssets.CreateStoneEntity(_target, health);
        _body = RenderTestAssets.CreateStoneBody(_target, entity, _recipe, _material);
        _body.Init(health, 1, _fx);
        Projectile projectile = _projectileObject.AddComponent<Projectile>();
        StoneProjectileImpactBridge bridge = _projectileObject.AddComponent<StoneProjectileImpactBridge>();
        TestHelpers.InvokePrivate(bridge, "OnEnable");
        ResourceModifier modifier = new ResourceModifier();
        Assert.IsNull(projectile.target);

        projectile.OnHit.Invoke(new OnHitData { target = _target, resourceModifier = modifier });

        Assert.AreEqual(StoneEffects.DustPuffs, _fx.liveCount); // the recorded contact raises dust

        bridge.enabled = false;
        TestHelpers.InvokePrivate(bridge, "OnDisable");
        projectile.OnHit.Invoke(new OnHitData { target = _target, resourceModifier = new ResourceModifier() });
        Assert.AreEqual(StoneEffects.DustPuffs, _fx.liveCount);
    }
}

}
