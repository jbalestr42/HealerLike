using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

public class ToolkitMobileLayoutTests
{
    GameObject _host;
    PanelSettings _panel;
    ToolkitGameView _view;
    ToolkitGameContext _context;
    ToolkitMobileLayout _layout;
    VisualElement _root;

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("Toolkit layout test");
        UIDocument document = _host.AddComponent<UIDocument>();
        _panel = ToolkitMobileLayout.CreatePanelSettings(null);
        document.panelSettings = _panel;
        _root = new VisualElement();
        Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree(_root);
        _view = new ToolkitGameView(_root);
        _context = new ToolkitGameContext();
        _layout = new ToolkitMobileLayout();
        _layout.Init(_view, _context, document);
    }

    [TearDown]
    public void TearDown()
    {
        _view.Release();
        Object.DestroyImmediate(_host);
        Object.DestroyImmediate(_panel);
    }

    [Test]
    public void Resize_PhoneThenDesktop_MovesSameSpeedControlsBetweenPauseAndDock()
    {
        VisualElement speed = _root.Q("speed-controls");
        VisualElement markers = _root.Q("mark-entity-toggle");
        _layout.Resize(390f, 844f);

        Assert.IsTrue(_root.Q("hud-root").ClassListContains("is-mobile"));
        Assert.AreEqual("pause-settings", speed.parent.name);
        Assert.AreEqual("pause-settings", markers.parent.name);

        _layout.Resize(1440f, 900f);

        Assert.IsFalse(_root.Q("hud-root").ClassListContains("is-mobile"));
        Assert.AreEqual("command-section", speed.parent.name);
        Assert.AreSame(speed, _root.Q("speed-controls"));
    }

    [Test]
    public void InspectRequested_Phone_OpensDetailsWithoutActivatingCard()
    {
        _layout.Resize(390f, 844f);
        int activations = 0;
        ToolkitCardModel model = new ToolkitCardModel();
        model.title = "Healing";
        model.activate = delegate { activations++; };

        _view.OnInspectRequested.Invoke(model);

        Assert.IsTrue(_root.Q("hud-root").ClassListContains("detail-open"));
        Assert.AreEqual(0, activations);
    }

    [Test]
    public void Refresh_InventoryOpened_ClosesInspectionDrawer()
    {
        _layout.Resize(390f, 844f);
        _view.OnInspectRequested.Invoke(new ToolkitCardModel());
        _context.isInventoryOpen = true;

        _layout.Refresh();

        Assert.IsFalse(_root.Q("hud-root").ClassListContains("detail-open"));
    }

    [Test]
    public void CardActivated_Phone_ClosesDrawerForBattlefieldTargeting()
    {
        _layout.Resize(390f, 844f);
        _view.OnInspectRequested.Invoke(new ToolkitCardModel());

        _view.OnCardActivated.Invoke(new ToolkitCardModel());

        Assert.IsFalse(_root.Q("hud-root").ClassListContains("detail-open"));
    }

    [Test]
    public void SetBattleFocus_ExistingControl_ReflectsFocusAndCanBeRemoved()
    {
        _layout.SetBattleFocus(true, delegate { });

        Assert.AreEqual("Overview", _root.Q<Button>("focus-button").text);
        Assert.IsFalse(_root.Q("focus-button").ClassListContains("is-hidden"));

        _layout.SetBattleFocus(false, null);

        Assert.IsTrue(_root.Q("focus-button").ClassListContains("is-hidden"));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void SetBattleFocus_OverlayOpen_DoesNotEnableControlDuringLateRefresh(bool paused, bool inventory)
    {
        _context.isPaused = paused;
        _context.isInventoryOpen = inventory;
        _layout.Refresh();

        _layout.SetBattleFocus(true, delegate { });

        Assert.IsFalse(_root.Q<Button>("focus-button").enabledSelf);
    }

    [Test]
    public void Update_HostSafeArea_InsetsHudAndModalByTheSamePanelDistance()
    {
        _layout.Update(delegate { return new Rect(0.05f, 0.1f, 0.9f, 0.75f); });
        VisualElement hud = _root.Q("hud-root");
        VisualElement inventory = _root.Q("inventory-panel");
        float scale = _panel.scale;

        Assert.AreEqual(Screen.width * 0.05f / scale + 8f, hud.style.paddingLeft.value.value, 0.01f);
        Assert.AreEqual(Screen.height * 0.15f / scale + 8f, hud.style.paddingTop.value.value, 0.01f);
        Assert.AreEqual(Screen.height * 0.1f / scale + 8f, hud.style.paddingBottom.value.value, 0.01f);
        Assert.AreEqual(hud.style.paddingLeft.value.value, inventory.style.left.value.value);
        Assert.AreEqual(hud.style.paddingTop.value.value, inventory.style.top.value.value);
        Assert.AreEqual(hud.style.paddingBottom.value.value, inventory.style.bottom.value.value);

        _layout.Update();

        Rect safe = ToolkitScreenLayout.GetSafePanelRect(Screen.width, Screen.height, Screen.safeArea, scale);
        Assert.AreEqual(safe.yMin + 8f, inventory.style.top.value.value, 0.01f);
    }

    [Test]
    public void SetCards_UnavailableSpell_KeepsSeparateInfoControlEnabled()
    {
        ToolkitCardModel model = new ToolkitCardModel();
        model.title = "Cooling down";
        model.isEnabled = false;
        _view.SetCards("spell-list", new ToolkitCardModel[] { model });
        VisualElement card = _root.Q("spell-list").Q(className: "card-shell");

        Assert.IsFalse(card.Q<Button>("data-card").enabledInHierarchy);
        Assert.IsTrue(card.Q<Button>("card-info").enabledInHierarchy);
        Assert.IsFalse(card.Q("card-info").ClassListContains("is-hidden"));
    }
}
}
