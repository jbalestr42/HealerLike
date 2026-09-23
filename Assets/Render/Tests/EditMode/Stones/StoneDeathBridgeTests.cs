using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneDeathBridgeTests
{
    GameObject _target;
    GameObject _bridgeObject;
    GameObject _effectsObject;
    StoneEffects _fx;
    StoneEnemyVisual _visual;

    [SetUp]
    public void SetUp()
    {
        _fx = StoneEffectsTests.CreateEffects();
        _effectsObject = _fx.gameObject;
        _target = new GameObject("Target");
        _bridgeObject = new GameObject("Bridge");
    }

    [TearDown]
    public void TearDown()
    {
        if (_visual != null)
        {
            TestHelpers.InvokePrivate(_visual, "OnDestroy");
            _visual = null;
        }
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_bridgeObject);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_effectsObject);
    }

    [Test]
    public void HandleDeparture_LivingThenLethal_CollapsesOnlyOnceOnTheLethalDeparture()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = _target.AddComponent<Entity>());
        TestHelpers.SetPrivateField(entity, "_health", health);
        _visual = StoneEnemyVisualTests.CreateVisual(_target);
        _visual.Init(health, 1, _fx);
        StoneDeathBridge bridge = _bridgeObject.AddComponent<StoneDeathBridge>();
        bridge.Bind(null, _fx);

        bridge.HandleDeparture(entity);
        Assert.AreEqual(0, _fx.liveCount);

        TestHelpers.SetPrivateField(health, "_value", 0f);
        bridge.HandleDeparture(entity);
        Assert.AreEqual(17, _fx.liveCount);

        bridge.HandleDeparture(entity);
        Assert.AreEqual(17, _fx.liveCount);

        _visual.Init(health, 1, _fx);
        bridge.enabled = false;
        bridge.HandleDeparture(entity);
        Assert.AreEqual(17, _fx.liveCount);
    }
}

}
