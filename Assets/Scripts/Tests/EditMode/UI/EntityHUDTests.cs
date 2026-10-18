using Entities;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// The health bar above a unit
public class EntityHUDTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _hudGo;
    EntityHUD _hud;
    Slider _slider;
    TMP_Text _text;
    BuffIconView _iconPrefab;

    [SetUp]
    public void SetUp()
    {
        _hudGo = new GameObject("HUD");
        _slider = new GameObject("Slider").AddComponent<Slider>();
        _slider.transform.SetParent(_hudGo.transform);
        _text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        _text.transform.SetParent(_hudGo.transform);

        ResourceView resourceView = _hudGo.AddComponent<ResourceView>();
        TestHelpers.SetPrivateField(resourceView, "_slider", _slider);
        TestHelpers.SetPrivateField(resourceView, "_text", _text);

        _hud = _hudGo.AddComponent<EntityHUD>();
        TestHelpers.SetPrivateField(_hud, "_resourceView", resourceView);
        TestHelpers.SetPrivateField(_hud, "_mark", new GameObject("Mark"));
        BuffIconBar buffIconBar = _hudGo.AddComponent<BuffIconBar>();
        _iconPrefab = BuffIconBarTests.CreateIconPrefab();
        TestHelpers.SetPrivateField(buffIconBar, "_iconPrefab", _iconPrefab);
        TestHelpers.SetPrivateField(_hud, "_buffIconBar", buffIconBar);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(TestHelpers.GetPrivateField<GameObject>(_hud, "_mark"));
        Object.DestroyImmediate(_hudGo);
        Object.DestroyImmediate(_iconPrefab.gameObject);
    }

    Entity CreateUnit(float value, float max)
    {
        Entity entity = _units.Create(value, max);
        TestHelpers.SetPrivateField(entity, "_buffManager", entity.gameObject.AddComponent<BuffManager>());
        // As Entity.Init
        entity.attributeManager = entity.GetComponent<AttributeManager>();
        return entity;
    }

    [Test]
    public void Init_ShowsTheHealthOfTheUnitRightAway()
    {
        // e.g. a Treant before the battle: no hit yet, its 300 HP must be shown, not the prefab default
        Entity treant = CreateUnit(300f, 300f);

        _hud.Init(treant);

        Assert.AreEqual("300 / 300", _text.text);
        Assert.AreEqual(1f, _slider.value, 0.0001f);
    }

    [Test]
    public void Init_ThenHealthChanges_ShowsTheNewHealth()
    {
        Entity unit = CreateUnit(100f, 100f);
        _hud.Init(unit);

        unit.health.SetValue(40f);
        TestUnits.Process(unit.health);

        Assert.AreEqual("40 / 100", _text.text);
    }

    // A shield on the unit, holding the given value
    ResourceAttribute GiveShield(Entity unit, float value)
    {
        GameObject shieldGo = new GameObject("Shield");
        shieldGo.transform.SetParent(unit.transform);
        ResourceAttribute shield = TestHelpers.CreateResourceAttribute(shieldGo, ResourceType.Shield, AttributeType.HealthMax, 100f);
        shield.fillWithMax = false;
        shield.SetValue(value);
        TestHelpers.SetPrivateField(unit, "_shield", shield);
        return shield;
    }

    RectTransform AddShieldBar()
    {
        RectTransform bar = new GameObject("ShieldBar", typeof(RectTransform)).GetComponent<RectTransform>();
        bar.SetParent(_hudGo.transform);
        TestHelpers.SetPrivateField(_hud, "_shieldBar", bar);
        return bar;
    }

    [Test]
    public void Shield_ShownAfterTheHealth()
    {
        Entity unit = CreateUnit(60f, 100f);
        GiveShield(unit, 12f);
        RectTransform bar = AddShieldBar();

        _hud.Init(unit);

        Assert.AreEqual("60 / 100 +12", _text.text);
        Assert.IsTrue(bar.gameObject.activeSelf);
        Assert.AreEqual(0.6f, bar.anchorMin.x, 0.0001f);
        Assert.AreEqual(0.72f, bar.anchorMax.x, 0.0001f);
    }

    [Test]
    public void Shield_Gained_ShownOnTheNextUpdate()
    {
        Entity unit = CreateUnit(100f, 100f);
        ResourceAttribute shield = GiveShield(unit, 0f);
        RectTransform bar = AddShieldBar();
        _hud.Init(unit);
        Assert.IsFalse(bar.gameObject.activeSelf);

        shield.SetValue(10f);
        TestUnits.Process(shield);

        Assert.AreEqual("100 / 100 +10", _text.text);
        Assert.IsTrue(bar.gameObject.activeSelf);
    }

    [Test]
    public void NoShield_TheHealthOnly()
    {
        Entity unit = CreateUnit(60f, 100f);
        GiveShield(unit, 0f);
        RectTransform bar = AddShieldBar();

        _hud.Init(unit);

        Assert.AreEqual("60 / 100", _text.text);
        Assert.IsFalse(bar.gameObject.activeSelf);
    }

    [Test]
    public void GetShieldSpan_RightAfterTheHealth()
    {
        (float start, float end) = EntityHUD.GetShieldSpan(50f, 20f, 100f);

        Assert.AreEqual(0.5f, start, 0.0001f);
        Assert.AreEqual(0.7f, end, 0.0001f);
    }

    // Full health: the shield covers the end of the bar
    [Test]
    public void GetShieldSpan_AboveTheMax_AtTheEndOfTheBar()
    {
        (float start, float end) = EntityHUD.GetShieldSpan(95f, 20f, 100f);

        Assert.AreEqual(0.8f, start, 0.0001f);
        Assert.AreEqual(1f, end, 0.0001f);
    }

    [Test]
    public void GetShieldSpan_NoShield_Empty()
    {
        (float start, float end) = EntityHUD.GetShieldSpan(50f, 0f, 100f);

        Assert.AreEqual(start, end);
    }

    // e.g. the Armored Footman raising its guard
    [Test]
    public void Init_UnitBlockingHits_ShowsHowMany()
    {
        Entity unit = CreateUnit(100f, 100f);
        unit.attributeManager.GetOrAdd(AttributeType.HitArmor).BaseValue = 2f;
        unit.attributeManager.ForceUpdate();

        _hud.Init(unit);

        BuffIconBar buffIconBar = TestHelpers.GetPrivateField<BuffIconBar>(_hud, "_buffIconBar");
        Assert.IsNotNull(buffIconBar.hitArmorView);
        Assert.AreEqual("2", buffIconBar.hitArmorView.stacksText);
    }
}

}
