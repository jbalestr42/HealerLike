using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class AttributeShieldViewTests
{
    GameObject _go;
    GameObject _sinkGo;
    SpellVisualSink _sink;
    BuffHandlerFactory _factory;
    FlatModifierFactory _modifier;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ShieldRecipient");
        _sinkGo = new GameObject("Sink");
        _sink = SpellSinkFixture.Add(_sinkGo);
        _factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        TestHelpers.InvokePrivate(_sink, "OnDestroy");
        Object.DestroyImmediate(_sinkGo);
        Object.DestroyImmediate(_factory);
        Object.DestroyImmediate(_modifier);
    }

    [Test]
    public void Refresh_InstantHitArmorGrant_ShowsOnePlatePerChargeWithoutAStartedHandler()
    {
        AttributeManager attributes = TestHelpers.CreateAttributeManager(_go, AttributeType.HitArmor, 0);
        BuffManager manager = _go.AddComponent<BuffManager>();
        manager.isEnabled = true;
        int starts = 0;
        manager.OnBuffHandlerStarted.AddListener(_ => starts++);
        _modifier.data = new FlatModifierData
        {
            type = AttributeType.HitArmor,
            modifierType = AttributeModifierType.Add,
            value = 2f
        };
        _factory.uniqueID = "InstantShieldFixture";
        _factory.data = new BuffHandlerData
        {
            durationType = DurationType.Instant,
            buffFactoryList = new List<ABuffFactory> { _modifier }
        };
        AttributeShieldView view = _go.AddComponent<AttributeShieldView>();
        view.Init(attributes, _go, _sink);
        Assert.IsNull(view.effect);

        TestHelpers.WithLoggingDisabled(() =>
        {
            manager.AddHandler(_factory, _go, _go);
            TestHelpers.InvokePrivate(manager, "Update");
            TestHelpers.InvokePrivate(attributes, "Update");
        });
        view.Refresh();

        Assert.AreEqual(0, starts, "an instant HitArmor grant raised a buff start event");
        Assert.AreEqual(2, attributes.Get(AttributeType.HitArmor).Value);
        Assert.NotNull(view.effect);
        Assert.AreEqual(EffectElement.Plates, view.effect.element);
        Assert.AreEqual(2, view.effect.count);

        attributes.Get(AttributeType.HitArmor).BaseValue = 0f;
        attributes.Get(AttributeType.HitArmor).Update();
        view.Refresh();
        Assert.IsNull(view.effect);
    }
}

}
