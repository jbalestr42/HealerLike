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
        TestHelpers.SetPrivateField(_hud, "_buffIconBar", _hudGo.AddComponent<BuffIconBar>());
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(TestHelpers.GetPrivateField<GameObject>(_hud, "_mark"));
        Object.DestroyImmediate(_hudGo);
    }

    Entity CreateUnit(float value, float max)
    {
        Entity entity = _units.Create(value, max);
        TestHelpers.SetPrivateField(entity, "_buffManager", entity.gameObject.AddComponent<BuffManager>());
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
}

}
