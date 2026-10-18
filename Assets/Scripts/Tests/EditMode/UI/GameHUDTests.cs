using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// The game HUD: only the mana bar, the Devotion and their tooltips, the rest relies on the singletons
// (PlayerBehaviour, UIManager)
public class GameHUDTests
{
    GameObject _hudGo;
    GameObject _manaBarGo;
    GameObject _devotionGo;
    DevotionView _devotionView;
    GameObject _characterGo;
    GameHUD _hud;

    [SetUp]
    public void SetUp()
    {
        _hudGo = new GameObject("GameHUD", typeof(RectTransform));
        _manaBarGo = new GameObject("ManaBar", typeof(RectTransform));
        _manaBarGo.transform.SetParent(_hudGo.transform, false);
        _hud = _hudGo.AddComponent<GameHUD>();
        TestHelpers.SetPrivateField(_hud, "_manaBar", _manaBarGo.AddComponent<ResourceView>());

        _devotionGo = new GameObject("Devotion", typeof(RectTransform));
        _devotionGo.transform.SetParent(_hudGo.transform, false);
        Image icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
        icon.transform.SetParent(_devotionGo.transform, false);
        TMPro.TMP_Text text = new GameObject("Text", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
        text.transform.SetParent(_devotionGo.transform, false);
        _devotionView = _devotionGo.AddComponent<DevotionView>();
        TestHelpers.SetPrivateField(_devotionView, "_icon", icon);
        TestHelpers.SetPrivateField(_devotionView, "_text", text);
        TestHelpers.SetPrivateField(_hud, "_devotionBar", _devotionView);

        _characterGo = new GameObject("Character");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_hudGo);
        Object.DestroyImmediate(_characterGo);
    }

    // The Devotion of a character holding up to 5, empty
    ResourceAttribute CreateDevotion()
    {
        ResourceAttribute devotion = TestHelpers.CreateResourceAttribute(_characterGo, ResourceType.Devotion, AttributeType.DevotionMax, 5f);
        devotion.SetValue(0f);
        TestHelpers.InvokePrivate(devotion, "Update");
        return devotion;
    }

    [Test]
    public void AddTooltips_TheManaBarAndTheDevotionShowATooltipOnHover()
    {
        TestHelpers.InvokePrivate(_hud, "AddTooltips");

        foreach (GameObject target in new[] { _manaBarGo, _devotionGo })
        {
            HoverTooltipTrigger trigger = target.GetComponent<HoverTooltipTrigger>();
            Assert.IsNotNull(trigger, target.name);
            Assert.IsNotNull(trigger.onEnter, target.name);
            Assert.IsNotNull(trigger.onExit, target.name);
        }
    }

    [Test]
    public void ShowDevotion_FollowsTheChargesOfTheCharacter()
    {
        ResourceAttribute devotion = CreateDevotion();

        _hud.ShowDevotion(devotion);
        devotion.SetValue(3f);
        TestHelpers.InvokePrivate(devotion, "Update");

        Assert.IsTrue(_devotionGo.activeSelf);
        Assert.AreEqual("3/5", _devotionView.text);
        Assert.IsTrue(_devotionView.isLit);
        StringAssert.Contains("3 / 5", _hud.GetDevotionTooltipText());
    }

    [Test]
    public void ShowDevotion_ACharacterBuildingNone_HidesIt_WithoutTooltip()
    {
        _hud.ShowDevotion(null);

        Assert.IsFalse(_devotionGo.activeSelf);
        Assert.IsNull(_hud.GetDevotionTooltipText());
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

    // The sandbox: the HUD is hidden in its scene and it has its own buttons
    [Test]
    public void ShowResourcesOnly_ShowsTheHUDAndTheManaBar_NotTheButtonsOfARun()
    {
        Button inventory = new GameObject("Inventory", typeof(RectTransform)).AddComponent<Button>();
        inventory.transform.SetParent(_hudGo.transform, false);
        Button nextWave = new GameObject("StartBattle", typeof(RectTransform)).AddComponent<Button>();
        nextWave.transform.SetParent(_hudGo.transform, false);
        TestHelpers.SetPrivateField(_hud, "_inventoryButton", inventory);
        TestHelpers.SetPrivateField(_hud, "_nextWaveButton", nextWave);
        _hudGo.SetActive(false);
        _manaBarGo.SetActive(false);

        _hud.ShowResourcesOnly();

        Assert.IsTrue(_hudGo.activeSelf);
        Assert.IsTrue(_manaBarGo.activeSelf);
        Assert.IsFalse(inventory.gameObject.activeSelf);
        Assert.IsFalse(nextWave.gameObject.activeSelf);
    }

    // The Cleric in the sandbox: its HUD starts after its init
    [Test]
    public void ShowCharacter_ShowsItsDevotion()
    {
        // The mana bar shows the mana too
        ResourceView manaBar = _manaBarGo.GetComponent<ResourceView>();
        TestHelpers.SetPrivateField(manaBar, "_slider", _manaBarGo.AddComponent<Slider>());
        TMPro.TMP_Text manaText = new GameObject("ManaText", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
        manaText.transform.SetParent(_manaBarGo.transform, false);
        TestHelpers.SetPrivateField(manaBar, "_text", manaText);
        Character character = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            _characterGo.AddComponent<AttributeManager>();
            character = _characterGo.AddComponent<Character>();
        });
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
        try
        {
            data.attributes = new System.Collections.Generic.Dictionary<AttributeType, float>
            {
                { AttributeType.ManaMax, 100f },
                { AttributeType.DevotionMax, 5f },
            };
            character.data = data;
            TestHelpers.WithLoggingDisabled(character.InitAttributes);
            TestHelpers.InvokePrivate(character.devotion, "Update");

            _hud.ShowCharacter(character);

            Assert.IsTrue(_devotionGo.activeSelf);
            Assert.AreEqual("0/5", _devotionView.text);
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }
}

}
