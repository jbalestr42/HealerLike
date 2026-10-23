using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

    // The run map: a room picked on it moves the run on, a map looked at while preparing goes back to the units
    public class ToolkitMapPanelTests
    {
        ToolkitTestRun _testRun;
        ToolkitGameContext _context;
        ToolkitGameView _view;
        ToolkitMapPanel _panel;
        RunState _run;

        [SetUp]
        public void SetUp()
        {
            _testRun = new ToolkitTestRun();
            _context = _testRun.context;
            _run = ToolkitRunMapPresentationTests.CreateRun();
            TestHelpers.SetPrivateField(_context.ascension, "_run", _run);
            OpenMap(AscensionGameType.State.SelectRoom);
            _view = new ToolkitGameView(Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree());
            _panel = new ToolkitMapPanel();
            _panel.Init(_context, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _panel.Dispose();
            _view.Release();
            _testRun.Dispose();
        }

        void OpenMap(AscensionGameType.State state)
        {
            _testRun.SetState(state);
            _testRun.game.screens.Open(GameScreen.Map);
        }

        void Select(MapNode node)
        {
            typeof(ToolkitMapPanel)
                .GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_panel, new object[] { node });
        }

        [Test]
        public void Select_AvailableRoom_TravelsAndClosesTheMap()
        {
            MapNode room = _run.map.startNodes[0];

            Select(room);

            Assert.AreSame(room, _run.currentNode);
            Assert.AreEqual(GameScreen.Game, _context.currentScreen);
            Assert.AreEqual(AscensionGameType.State.EnterRoom, _context.ascension.state);
        }

        [Test]
        public void Select_LockedOrOldRunNode_DoesNotTravel()
        {
            Select(_run.map.boss);
            Select(ToolkitRunMapPresentationTests.CreateRun().map.startNodes[0]);

            Assert.IsNull(_run.currentNode);
            Assert.AreEqual(GameScreen.Map, _context.currentScreen);
        }

        [Test]
        public void Select_MapOnlyLookedAt_DoesNotTravel()
        {
            _testRun.SetState(AscensionGameType.State.WaitForRoundToStart);

            Select(_run.map.startNodes[0]);

            Assert.IsNull(_run.currentNode);
        }

        [Test]
        public void Select_QueuedSecondClickAfterTravel_CannotAdvanceAgain()
        {
            Select(_run.map.startNodes[0]);
            Select(_run.GetAvailableNodes()[0]);

            Assert.AreEqual(0, _run.currentFloor);
        }

        [Test]
        public void Close_SelectionIsMandatoryButALookedAtMapReturnsToTheUnits()
        {
            Assert.IsFalse(_panel.Close());

            _testRun.SetState(AscensionGameType.State.WaitForRoundToStart);

            Assert.IsTrue(_panel.Close());
            Assert.AreEqual(GameScreen.Game, _context.currentScreen);
            Assert.IsNull(_run.currentNode);
        }

        [Test]
        public void Open_WhilePreparing_LooksAtTheMap_NotWithTheInventoryOpenNorPaused()
        {
            _testRun.game.screens.Close();
            _testRun.SetState(AscensionGameType.State.WaitForRoundToStart);

            _context.isInventoryOpen = true;
            TestHelpers.InvokePrivate(_panel, "Open");
            Assert.AreEqual(GameScreen.Game, _context.currentScreen);

            _context.isInventoryOpen = false;
            _context.isPaused = true;
            TestHelpers.InvokePrivate(_panel, "Open");
            Assert.AreEqual(GameScreen.Game, _context.currentScreen);

            _context.isPaused = false;
            TestHelpers.InvokePrivate(_panel, "Open");
            Assert.AreEqual(GameScreen.Map, _context.currentScreen);
        }

        [Test]
        public void Reinitialize_ReleasesPreviousVisualTreeBeforeRebinding()
        {
            _panel.Refresh();
            ToolkitGameView replacement = new ToolkitGameView(
                Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree()
            );
            try
            {
                _panel.Init(_context, replacement);
                _panel.Refresh();
                VisualElement oldSurface = _view.root.Q("map-connections");
                Assert.IsNotNull(oldSurface, "The drawing surface belongs to the authored template.");
                Assert.IsNull(oldSurface.userData, "The old painter must release its binding.");
                Assert.IsEmpty(_view.root.Query<Button>(className: "map-node").ToList());
                Assert.IsNotNull(replacement.root.Q("map-connections").userData);
                Assert.AreEqual(5, replacement.root.Query<Button>(className: "map-node").ToList().Count);
            }
            finally
            {
                _panel.Dispose();
                replacement.Release();
            }
        }
    }
}
