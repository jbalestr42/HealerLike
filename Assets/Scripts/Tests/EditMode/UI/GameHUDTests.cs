using NUnit.Framework;
using UnityEngine;

namespace UI
{

// The game HUD: only the mana bar's tooltip, the rest relies on the singletons (PlayerBehaviour, UIManager)
public class GameHUDTests
{
    GameObject _hudGo;
    GameObject _manaBarGo;
    GameHUD _hud;

    [SetUp]
    public void SetUp()
    {
        _hudGo = new GameObject("GameHUD", typeof(RectTransform));
        _manaBarGo = new GameObject("ManaBar", typeof(RectTransform));
        _manaBarGo.transform.SetParent(_hudGo.transform, false);
        _hud = _hudGo.AddComponent<GameHUD>();
        TestHelpers.SetPrivateField(_hud, "_manaBar", _manaBarGo.AddComponent<ResourceView>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_hudGo);
    }

    [Test]
    public void AddManaTooltip_TheManaBarShowsATooltipOnHover()
    {
        TestHelpers.InvokePrivate(_hud, "AddManaTooltip");

        HoverTooltipTrigger trigger = _manaBarGo.GetComponent<HoverTooltipTrigger>();
        Assert.IsNotNull(trigger);
        Assert.IsNotNull(trigger.onEnter);
        Assert.IsNotNull(trigger.onExit);
    }

    [Test]
    public void GetManaTooltipText_NoTooltipSet_IsNull()
    {
        Assert.IsNull(_hud.GetManaTooltipText());
    }

    [Test]
    public void GetManaTooltipText_ReadsTheTextAgainOnEachHover()
    {
        int maxMana = 50;
        _hud.manaTooltip = () => $"+{maxMana} mana";

        Assert.AreEqual("+50 mana", _hud.GetManaTooltipText());
        maxMana = 80;
        Assert.AreEqual("+80 mana", _hud.GetManaTooltipText());
    }
}

}
