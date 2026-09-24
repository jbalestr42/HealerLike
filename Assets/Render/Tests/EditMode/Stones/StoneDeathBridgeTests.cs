using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneDeathBridgeTests
{
    GameObject _target;
    GameObject _bridgeObject;
    GameObject _effectsObject;
    StoneEffects _fx;
    CreatureRecipe _recipe;
    Material _material;
    StoneBody _body;

    [SetUp]
    public void SetUp()
    {
        _fx = StoneEffectsTests.CreateEffects();
        _effectsObject = _fx.gameObject;
        _target = new GameObject("Target");
        _bridgeObject = new GameObject("Bridge");
        _recipe = StoneBodyTests.Recipe();
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(
            "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"));
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
        Object.DestroyImmediate(_bridgeObject);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_effectsObject);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    [Test]
    public void HandleDeparture_LivingThenLethal_CollapsesOnlyOnceOnTheLethalDeparture()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
        Entity entity = StoneBodyTests.CreateEntity(_target, health);
        _body = StoneBodyTests.CreateBody(_target, entity, _recipe, _material);
        _body.Init(health, 1, _fx);
        StoneDeathBridge bridge = _bridgeObject.AddComponent<StoneDeathBridge>();
        bridge.Bind(null, _fx);

        bridge.HandleDeparture(entity);
        Assert.AreEqual(0, _fx.liveCount);

        TestHelpers.SetPrivateField(health, "_value", 0f);
        bridge.HandleDeparture(entity);
        int collapse = StoneEffects.CollapseDebris + StoneEffects.DustPuffs;
        Assert.AreEqual(collapse, _fx.liveCount); // 12 debris and 5 dust
        Assert.IsTrue(_body.isCollapsed);

        bridge.HandleDeparture(entity);
        Assert.AreEqual(collapse, _fx.liveCount);

        _body.Init(health, 1, _fx);
        bridge.enabled = false;
        bridge.HandleDeparture(entity);
        Assert.AreEqual(collapse, _fx.liveCount);
        Assert.IsFalse(_body.isCollapsed);
    }

    [Test]
    public void HandleDeparture_BodyWithoutEffects_CollapsesWithTheBridgeEffects()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100);
        Entity entity = StoneBodyTests.CreateEntity(_target, health);
        _body = StoneBodyTests.CreateBody(_target, entity, _recipe, _material);
        _body.Init(health, 1, null);
        StoneDeathBridge bridge = _bridgeObject.AddComponent<StoneDeathBridge>();
        bridge.Bind(null, _fx);
        TestHelpers.SetPrivateField(health, "_value", 0f);

        bridge.HandleDeparture(entity);

        Assert.AreEqual(StoneEffects.CollapseDebris + StoneEffects.DustPuffs, _fx.liveCount);
    }
}

}
