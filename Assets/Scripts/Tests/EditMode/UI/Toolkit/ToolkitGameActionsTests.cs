using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The menu's two routes through the host's loader: Start opens the class choice then the expedition, Sandbox opens
    // Julien's sandbox directly
    public class ToolkitGameActionsTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        GameObject _owner;
        ToolkitGameUI _host;
        ToolkitGameContext _context;
        PanelSettings _settings;
        ToolkitMobileLayout _mobile;
        ToolkitTimeControls _time;
        ToolkitMapPanel _map;
        ToolkitGameActions _actions;
        float _speed;
        readonly List<string> _loads = new List<string>();
        readonly List<Object> _created = new List<Object>();
        GameData _data;

        [SetUp]
        public void SetUp()
        {
            CharacterSelection.selected = null;
            _loads.Clear();
            _speed = Time.timeScale;
            _panel = new ToolkitTestPanel();
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _panel.root.Add(root);
            _view = new ToolkitGameView(root);
            _owner = new GameObject("Toolkit menu actions");
            _owner.SetActive(false);
            _host = _owner.AddComponent<ToolkitGameUI>();
            _host.sceneLoader = scene =>
            {
                _loads.Add(scene);
                return true;
            };
            _settings = ToolkitMobileLayout.CreatePanelSettings(null);
            UIDocument document = _owner.GetComponent<UIDocument>();
            document.panelSettings = _settings;
            _context = new ToolkitGameContext { isMenu = true };
            _mobile = new ToolkitMobileLayout();
            _time = new ToolkitTimeControls();
            _map = new ToolkitMapPanel();
            _mobile.Init(_view, _context, document);
            _time.Init(null, _context, _view);
            _actions = new ToolkitGameActions(_host, _context, _view, _time, _mobile, _map);
        }

        [TearDown]
        public void TearDown()
        {
            _actions.Dispose();
            _map.Dispose();
            _time.Dispose();
            _mobile.Dispose();
            _view.Release();
            _panel.Dispose();
            _owner.GetComponent<UIDocument>().panelSettings = null;
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_settings);
            Time.timeScale = _speed;
            CharacterSelection.selected = null;
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        // Three classes on the host, as MenuToolkit gets Main's game data
        void GiveClasses()
        {
            _data = ScriptableObject.CreateInstance<GameData>();
            _created.Add(_data);
            foreach (string title in new[] { "Cleric", "Druid", "Warlock" })
            {
                CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
                character.name = title + "Character";
                character.title = title;
                character.text = title + " description";
                _created.Add(character);
                _data.characters.Add(character);
            }

            _host.gameData = _data;
        }

        VisualElement ClassPanel()
        {
            return _view.root.Q(ToolkitClassSelect.PanelName);
        }

        bool IsShown(VisualElement element)
        {
            return !element.ClassListContains("is-hidden");
        }

        List<Button> ClassCards()
        {
            return _view.root.Q(ToolkitClassSelect.ListName).Query<Button>("data-card").ToList();
        }

        Button ClassCard(string title)
        {
            foreach (Button card in ClassCards())
            {
                if (card.userData is ToolkitCardModel model && model.title == title)
                {
                    return card;
                }
            }

            return null;
        }

        [Test]
        public void SandboxButton_Menu_LoadsTheSandboxScene()
        {
            ToolkitTestPanel.Submit(_view.root.Q<Button>("sandbox-button"));

            CollectionAssert.AreEqual(new[] { "Sandbox" }, _loads);
        }

        [Test]
        public void SandboxButton_Menu_LoadsTheHostsSandboxScene()
        {
            _host.sandboxScene = "OtherSandbox";

            ToolkitTestPanel.Submit(_view.root.Q<Button>("sandbox-button"));

            CollectionAssert.AreEqual(new[] { "OtherSandbox" }, _loads);
        }

        [Test]
        public void StartButton_MenuWithoutGameData_LoadsTheGameplaySceneDirectly()
        {
            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            CollectionAssert.AreEqual(new[] { "Main" }, _loads);
            Assert.IsFalse(IsShown(ClassPanel()));
        }

        [Test]
        public void StartButton_Menu_OpensTheClassScreenWithoutLoading()
        {
            GiveClasses();

            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            CollectionAssert.IsEmpty(_loads);
            Assert.IsTrue(IsShown(ClassPanel()));
            CollectionAssert.AreEqual(new[] { "Cleric", "Druid", "Warlock", "Random" },
                ClassCards().ConvertAll(card => ((ToolkitCardModel)card.userData).title));
            Assert.IsNull(CharacterSelection.selected);
        }

        [Test]
        public void ClassCard_Picked_SelectsTheClassAndLoadsTheGameplayScene()
        {
            GiveClasses();
            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            ToolkitTestPanel.Submit(ClassCard("Druid"));

            Assert.AreSame(_data.characters[1], CharacterSelection.selected);
            CollectionAssert.AreEqual(new[] { "Main" }, _loads);
            Assert.IsFalse(IsShown(ClassPanel()));
        }

        [Test]
        public void ClassCard_Picked_LoadsTheHostsGameplayScene()
        {
            GiveClasses();
            _host.gameplayScene = "MainToolkit";
            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            ToolkitTestPanel.Submit(ClassCard("Warlock"));

            Assert.AreSame(_data.characters[2], CharacterSelection.selected);
            CollectionAssert.AreEqual(new[] { "MainToolkit" }, _loads);
        }

        [Test]
        public void RandomCard_Picked_SelectsAnOfferedClassAndLoadsTheGameplayScene()
        {
            GiveClasses();
            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            ToolkitTestPanel.Submit(ClassCard("Random"));

            CollectionAssert.Contains(_data.characters, CharacterSelection.selected);
            CollectionAssert.AreEqual(new[] { "Main" }, _loads);
        }

        [Test]
        public void BackButton_ClassScreen_ReturnsToTheMenuWithoutLoading()
        {
            GiveClasses();
            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            ToolkitTestPanel.Submit(_view.root.Q<Button>(ToolkitClassSelect.BackButtonName));

            Assert.IsFalse(IsShown(ClassPanel()));
            CollectionAssert.IsEmpty(_loads);
            Assert.IsNull(CharacterSelection.selected);
        }

        [Test]
        public void StartButton_AfterBack_OpensTheClassScreenAgain()
        {
            GiveClasses();
            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));
            ToolkitTestPanel.Submit(_view.root.Q<Button>(ToolkitClassSelect.BackButtonName));

            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            Assert.IsTrue(IsShown(ClassPanel()));
            CollectionAssert.IsEmpty(_loads);
        }

        [Test]
        public void SandboxButton_MenuWithClasses_LoadsTheSandboxWithoutTheClassScreen()
        {
            GiveClasses();

            ToolkitTestPanel.Submit(_view.root.Q<Button>("sandbox-button"));

            CollectionAssert.AreEqual(new[] { "Sandbox" }, _loads);
            Assert.IsFalse(IsShown(ClassPanel()));
            Assert.IsNull(CharacterSelection.selected);
        }

        [Test]
        public void StartButton_DuringARun_NeverOpensTheClassScreen()
        {
            GiveClasses();
            _context.isMenu = false;

            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            Assert.IsFalse(IsShown(ClassPanel()));
            CollectionAssert.IsEmpty(_loads);
        }

        [Test]
        public void SandboxButton_DuringARun_LoadsNothing()
        {
            _context.isMenu = false;

            ToolkitTestPanel.Submit(_view.root.Q<Button>("sandbox-button"));

            CollectionAssert.IsEmpty(_loads);
        }

        [Test]
        public void Dispose_SandboxButton_NoLongerLoads()
        {
            _actions.Dispose();

            ToolkitTestPanel.Submit(_view.root.Q<Button>("sandbox-button"));

            CollectionAssert.IsEmpty(_loads);
        }
    }
}
