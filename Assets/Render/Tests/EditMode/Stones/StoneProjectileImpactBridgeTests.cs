using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneProjectileImpactBridgeTests
{
    GameObject _target;
    GameObject _projectileObject;
    StoneEnemyVisual _visual;

    [SetUp]
    public void SetUp()
    {
        _target = new GameObject("Target");
        _projectileObject = new GameObject("Projectile");
    }

    [TearDown]
    public void TearDown()
    {
        if (_visual != null)
        {
            TestHelpers.InvokePrivate(_visual, "OnDestroy");
            _visual = null;
        }
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_projectileObject);
    }

    [Test]
    public void OnHit_ProjectileTargetCleared_UsesTheCallbackTargetUntilDisabled()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
        _visual = StoneEnemyVisualTests.CreateVisual(_target);
        _visual.Init(health, 1, null);
        Projectile projectile = _projectileObject.AddComponent<Projectile>();
        StoneProjectileImpactBridge bridge = _projectileObject.AddComponent<StoneProjectileImpactBridge>();
        TestHelpers.InvokePrivate(bridge, "OnEnable");
        ResourceModifier modifier = new ResourceModifier();
        Assert.IsNull(projectile.target);

        projectile.OnHit.Invoke(new OnHitData { target = _target, resourceModifier = modifier });

        Assert.AreEqual(1, _visual.pendingImpactCount);

        bridge.enabled = false;
        TestHelpers.InvokePrivate(bridge, "OnDisable");
        projectile.OnHit.Invoke(new OnHitData { target = _target, resourceModifier = new ResourceModifier() });
        Assert.AreEqual(1, _visual.pendingImpactCount);
    }
}

}
