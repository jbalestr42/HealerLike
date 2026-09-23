using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class AttributeShieldViewTests
{
    GameObject _go;
    BuffHandlerFactory _factory;
    FlatModifierFactory _modifier;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ShieldRecipient");
        _factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_factory);
        Object.DestroyImmediate(_modifier);
    }

    [Test]
    public void Refresh_InstantHitArmorGrant_ShowsPlatesWithoutAStartedHandler()
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
        view.Bind(attributes, _go.transform, AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset"));
        Assert.IsNull(view.effect);

        TestHelpers.WithLoggingDisabled(() =>
        {
            manager.AddHandler(_factory, _go, _go);
            TestHelpers.InvokePrivate(manager, "Update");
            TestHelpers.InvokePrivate(attributes, "Update");
        });
        view.Refresh();

        Assert.AreEqual(0, starts, "The real instant gameplay path must not be replaced with a fabricated start event.");
        Assert.AreEqual(2, attributes.Get(AttributeType.HitArmor).Value);
        Assert.NotNull(view.effect);
        int plates = 0;
        foreach (Transform plate in view.effect.parts)
        {
            if (plate.gameObject.activeSelf)
            {
                plates++;
            }
        }
        Assert.AreEqual(2, plates);

        attributes.Get(AttributeType.HitArmor).BaseValue = 0f;
        attributes.Get(AttributeType.HitArmor).Update();
        view.Refresh();
        Assert.IsNull(view.effect);
    }
}

}
