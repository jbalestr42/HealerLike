using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The menu's two routes: Start opens the expedition, Sandbox opens Julien's sandbox, both through the host's loader
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

        [SetUp]
        public void SetUp()
        {
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
        public void StartButton_Menu_StillLoadsTheGameplayScene()
        {
            ToolkitTestPanel.Submit(_view.root.Q<Button>("start-button"));

            CollectionAssert.AreEqual(new[] { "Main" }, _loads);
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
