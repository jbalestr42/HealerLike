using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The sandbox tools: a button per tool and per entry of the sandbox data, opened from the toolbar
    public class ToolkitSandboxPanelTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        ToolkitGameContext _context;
        ToolkitSandboxPanel _sandboxPanel;
        GameObject _host;
        SandboxGameType _sandbox;
        readonly List<Object> _objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Sandbox");
            _host.SetActive(false);
            GameManager game = _host.AddComponent<GameManager>();
            game.screens.Open(GameScreen.Game);
            _sandbox = _host.AddComponent<SandboxGameType>();
            SandboxData data = Track(ScriptableObject.CreateInstance<SandboxData>());
            data.waves.Add(Track(ScriptableObject.CreateInstance<WavePatternData>()));
            data.waves[0].name = "Recruits";
            EntityData unit = Track(ScriptableObject.CreateInstance<EntityData>());
            unit.title = "Zealot";
            data.entities.Add(unit);
            StatusData status = Track(ScriptableObject.CreateInstance<StatusData>());
            status.title = "Poisoned";
            data.statuses.Add(status);
            TestHelpers.SetPrivateField(_sandbox, "_data", data);
            _context = new ToolkitGameContext { game = game, sandbox = _sandbox };

            _panel = new ToolkitTestPanel();
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _panel.root.Add(root);
            _view = new ToolkitGameView(root);
            _sandboxPanel = new ToolkitSandboxPanel();
            _sandboxPanel.Init(_context, _view, null);
        }

        [TearDown]
        public void TearDown()
        {
            _sandboxPanel.Dispose();
            _view.Release();
            _panel.Dispose();
            Object.DestroyImmediate(_host);
            foreach (Object obj in _objects)
            {
                Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            _objects.Add(obj);
            return obj;
        }

        void Initialize()
        {
            TestHelpers.SetPrivateField(_sandbox, "_isInitialized", true);
        }

        List<string> Labels(string listName)
        {
            return _view.root.Q(listName).Query<Button>().ToList().ConvertAll(button => button.text);
        }

        bool IsShown()
        {
            return !_view.root.Q(ToolkitSandboxPanel.PanelName).ClassListContains("is-hidden");
        }

        [Test]
        public void Refresh_SandboxNotInitialized_BuildsNothing()
        {
            _sandboxPanel.Refresh();

            Assert.IsFalse(_sandboxPanel.isBuilt);
        }

        [Test]
        public void Refresh_Initialized_OneButtonPerEntryOfTheData()
        {
            Initialize();

            _sandboxPanel.Refresh();

            CollectionAssert.AreEqual(new[] { "Recruits" }, Labels(ToolkitSandboxPanel.WavesName));
            CollectionAssert.AreEqual(new[] { "Zealot" }, Labels(ToolkitSandboxPanel.UnitsName));
            CollectionAssert.AreEqual(new[] { "Poisoned" }, Labels(ToolkitSandboxPanel.StatusesName));
            CollectionAssert.AreEqual(new[] { ToolkitSandboxPanel.GetCharacterText(null), "Side: Ally", "Remove units", "Heal all", "Clear all" },
                Labels(ToolkitSandboxPanel.ControlsName));
        }

        [Test]
        public void Refresh_Twice_BuildsOnce()
        {
            Initialize();
            _sandboxPanel.Refresh();

            _sandboxPanel.Refresh();

            Assert.AreEqual(1, Labels(ToolkitSandboxPanel.UnitsName).Count);
        }

        [Test]
        public void Refresh_Opened_ShowsThePanel()
        {
            Initialize();
            _context.isSandboxOpen = true;

            _sandboxPanel.Refresh();

            Assert.IsTrue(IsShown());
        }

        [Test]
        public void Refresh_OutsideTheSandbox_ClosesIt()
        {
            Initialize();
            _context.isSandboxOpen = true;
            _context.sandbox = null;

            _sandboxPanel.Refresh();

            Assert.IsFalse(_context.isSandboxOpen);
            Assert.IsFalse(IsShown());
        }

        [Test]
        public void Close_HidesThePanel()
        {
            Initialize();
            _context.isSandboxOpen = true;
            _sandboxPanel.Refresh();

            _sandboxPanel.Close();

            Assert.IsFalse(_context.isSandboxOpen);
            Assert.IsFalse(IsShown());
        }

        [Test]
        public void SideButton_SwitchesTheSideTheUnitsArePlacedOn()
        {
            Initialize();
            _sandboxPanel.Refresh();
            Button side = _view.root.Q<Button>(ToolkitSandboxPanel.SideName);

            using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = side;
                side.SendEvent(evt);
            }

            Assert.AreEqual(Entity.EntityType.Computer, _sandbox.placementSide);
            Assert.AreEqual("Side: Enemy", side.text);
        }

        [Test]
        public void GetCharacterText_NamesThePlayedCharacter()
        {
            CharacterData cleric = Track(ScriptableObject.CreateInstance<CharacterData>());
            cleric.title = "Cleric";

            Assert.AreEqual("Character: Cleric", ToolkitSandboxPanel.GetCharacterText(cleric));
            Assert.AreEqual("Character: All skills", ToolkitSandboxPanel.GetCharacterText(null));
        }
    }
}
