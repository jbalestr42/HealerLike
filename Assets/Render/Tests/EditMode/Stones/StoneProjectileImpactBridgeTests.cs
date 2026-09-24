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

    [SetUp]
    public void SetUp()
    {
        _target = new GameObject("Target");
        _projectileObject = new GameObject("Projectile");
        _recipe = RenderTestAssets.CreateStoneRecipe();
        _material = new Material(RenderTestAssets.LoadLookMaterial());
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
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_projectileObject);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    [Test]
    public void OnHit_ProjectileTargetCleared_UsesTheCallbackTargetUntilDisabled()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
        _body = RenderTestAssets.CreateStoneBody(_target, RenderTestAssets.CreateStoneEntity(_target, health), _recipe, _material);
        _body.Init(health, 1, null);
        Projectile projectile = _projectileObject.AddComponent<Projectile>();
        StoneProjectileImpactBridge bridge = _projectileObject.AddComponent<StoneProjectileImpactBridge>();
        TestHelpers.InvokePrivate(bridge, "OnEnable");
        ResourceModifier modifier = new ResourceModifier();
        Assert.IsNull(projectile.target);

        projectile.OnHit.Invoke(new OnHitData { target = _target, resourceModifier = modifier });

        Assert.AreEqual(1, _body.pendingImpactCount);

        bridge.enabled = false;
        TestHelpers.InvokePrivate(bridge, "OnDisable");
        projectile.OnHit.Invoke(new OnHitData { target = _target, resourceModifier = new ResourceModifier() });
        Assert.AreEqual(1, _body.pendingImpactCount);
    }
}

}
