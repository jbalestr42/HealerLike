using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
public class ToolkitMapPanelTests
{
    GameObject _host;
    ToolkitGameContext _context;
    ToolkitGameView _view;
    ToolkitMapPanel _panel;
    MapView _legacyMap;
    RunState _run;
    int _selections;

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("Map adapter fixture");
        _host.SetActive(false);
        _run = ToolkitRunMapPresentationTests.CreateRun();
        _context = new ToolkitGameContext();
        _context.ui = _host.AddComponent<UIManager>();
        _context.ascension = _host.AddComponent<AscensionGameType>();
        _context.legacy = _host.AddComponent<GameView>();
        _host.AddComponent<CanvasGroup>();
        GameHUD hud = _host.AddComponent<GameHUD>();
        var hudButton = _host.AddComponent<UnityEngine.UI.Button>();
        TestHelpers.SetPrivateField(hud, "_mapButton", hudButton);
        TestHelpers.SetPrivateField(hud, "_inventoryButton", hudButton);
        TestHelpers.SetPrivateField(_context.legacy, "_gameHUD", hud);
        GameObject mapHost = new GameObject("Legacy map", typeof(RectTransform), typeof(CanvasGroup));
        mapHost.SetActive(false);
        mapHost.transform.SetParent(_host.transform);
        _legacyMap = mapHost.AddComponent<MapView>();
        _legacyMap.OnNodeSelected.AddListener(node => _selections++);
        TestHelpers.SetPrivateField(_context.ui, "_views", new Dictionary<ViewType, AView>
            { { ViewType.Game, _context.legacy }, { ViewType.Map, _legacyMap } });
        TestHelpers.SetPrivateField(_context.ui, "_viewStack", new List<ViewType> { ViewType.Game, ViewType.Map });
        SetView(ViewType.Map, AscensionGameType.State.SelectRoom);
        TestHelpers.SetPrivateField(_context.ascension, "_run", _run);
        _view = new ToolkitGameView(Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree());
        _panel = new ToolkitMapPanel();
        _panel.Init(_context, _view);
        _selections = 0;
    }

    [TearDown]
    public void TearDown()
    {
        _panel.Dispose();
        _view.Release();
        Object.DestroyImmediate(_host);
    }

    void SetView(ViewType view, AscensionGameType.State state)
    {
        TestHelpers.SetPrivateField(_context.ui, "_currentView", view);
        TestHelpers.SetPrivateField(_context.ascension, "_state", state);
    }

    void Select(MapNode node)
    {
        typeof(ToolkitMapPanel).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_panel, new object[] { node });
    }

    [Test]
    public void Select_AvailableRoomForwardsEventWithoutMutatingRun()
    {
        Select(_run.map.startNodes[0]);
        Assert.AreEqual(1, _selections);
        Assert.IsNull(_run.currentNode, "Only Julien's listener is allowed to advance the run.");
    }

    [Test]
    public void Select_LockedOrOldRunNodeDoesNotForward()
    {
        Select(_run.map.boss);
        Select(ToolkitRunMapPresentationTests.CreateRun().map.startNodes[0]);
        Assert.AreEqual(0, _selections);
    }

    [TestCase(ViewType.Map, AscensionGameType.State.WaitForRoundToStart)]
    [TestCase(ViewType.Game, AscensionGameType.State.SelectRoom)]
    [TestCase(ViewType.Map, AscensionGameType.State.InitializeRound)]
    public void Select_OutsideLiveSelectionStateDoesNotForward(ViewType view, AscensionGameType.State state)
    {
        SetView(view, state);
        Select(_run.map.startNodes[0]);
        Assert.AreEqual(0, _selections);
    }

    [Test]
    public void Select_QueuedSecondClickAfterGameplayTravelCannotAdvanceAgain()
    {
        _legacyMap.OnNodeSelected.AddListener(node =>
        {
            _run.TravelTo(node);
            SetView(ViewType.Game, AscensionGameType.State.InitializeRound);
        });
        Select(_run.map.startNodes[0]);
        Select(_run.GetAvailableNodes()[0]);
        Assert.AreEqual(1, _selections);
        Assert.AreEqual(0, _run.currentFloor);
    }

    [Test]
    public void Close_SelectionIsMandatoryButInspectReturnsToGame()
    {
        Assert.IsFalse(_panel.Close());
        SetView(ViewType.Map, AscensionGameType.State.WaitForRoundToStart);
        Assert.IsTrue(_panel.Close());
        Assert.AreEqual(ViewType.Game, LegacyUiReader.CurrentView(_context.ui));
        Assert.IsNull(_run.currentNode);
    }

    [Test]
    public void Open_ForwardsOnlyTheExistingPlanningButton()
    {
        SetView(ViewType.Game, AscensionGameType.State.WaitForRoundToStart);
        int opens = 0;
        _context.legacy.gameHUD.mapButton.onClick.AddListener(() => opens++);
        TestHelpers.InvokePrivate(_panel, "Open");
        Assert.AreEqual(1, opens);
        _context.isInventoryOpen = true;
        TestHelpers.InvokePrivate(_panel, "Open");
        Assert.AreEqual(1, opens);
        _context.isInventoryOpen = false;
        _context.isPaused = true;
        TestHelpers.InvokePrivate(_panel, "Open");
        Assert.AreEqual(1, opens);
    }

    [Test]
    public void Reinitialize_ReleasesPreviousVisualTreeBeforeRebinding()
    {
        _panel.Refresh();
        ToolkitGameView replacement = new ToolkitGameView(Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree());
        try
        {
            _panel.Init(_context, replacement);
            _panel.Refresh();
            Assert.IsNull(_view.root.Q("map-connections"));
            Assert.AreEqual(5, replacement.root.Query<Button>(className: "map-node").ToList().Count);
        }
        finally { _panel.Dispose(); replacement.Release(); }
    }
}
}
