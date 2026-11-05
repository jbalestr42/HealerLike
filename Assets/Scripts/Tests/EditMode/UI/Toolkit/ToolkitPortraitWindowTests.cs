using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitPortraitWindowTests
    {
        static readonly Vector2Int Display1080 = new Vector2Int(1920, 1080);

        ToolkitGameView _view;
        ToolkitPortraitWindow _window;
        ToolkitPortraitWindow.WindowState _current;
        List<ToolkitPortraitWindow.WindowState> _applied;

        [SetUp]
        public void SetUp()
        {
            VisualElement root = new VisualElement();
            Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree(root);
            _view = new ToolkitGameView(root);
            _current = new ToolkitPortraitWindow.WindowState(1920, 1080, FullScreenMode.FullScreenWindow);
            _applied = new List<ToolkitPortraitWindow.WindowState>();
            _window = new ToolkitPortraitWindow();
            _window.Init(_view, true, () => Display1080, () => _current, Apply);
        }

        // The window takes the size it is given, as the player's does
        void Apply(ToolkitPortraitWindow.WindowState state)
        {
            _applied.Add(state);
            _current = state;
        }

        [TearDown]
        public void TearDown()
        {
            // The size to come back to is process-wide: leave no portrait state to the next test
            if (_window.isPortrait)
            {
                _window.Toggle();
            }

            _window.Dispose();
            _view.Release();
        }

        [Test]
        public void IsAvailable_OnlyOnDesktopPlayers()
        {
            Assert.IsTrue(ToolkitPortraitWindow.IsAvailable(RuntimePlatform.WindowsPlayer));
            Assert.IsTrue(ToolkitPortraitWindow.IsAvailable(RuntimePlatform.OSXPlayer));
            Assert.IsTrue(ToolkitPortraitWindow.IsAvailable(RuntimePlatform.LinuxPlayer));
            Assert.IsFalse(ToolkitPortraitWindow.IsAvailable(RuntimePlatform.WindowsEditor));
            Assert.IsFalse(ToolkitPortraitWindow.IsAvailable(RuntimePlatform.Android));
            Assert.IsFalse(ToolkitPortraitWindow.IsAvailable(RuntimePlatform.IPhonePlayer));
        }

        [Test]
        public void GetPortraitSize_LandscapeDisplay_IsTallAtPhoneAspect()
        {
            Vector2Int size = ToolkitPortraitWindow.GetPortraitSize(Display1080);
            Assert.AreEqual(972, size.y, "90% of the display height");
            Assert.AreEqual(449, size.x, "972 * 390 / 844");
            Assert.Greater(size.y, size.x);
        }

        [Test]
        public void GetPortraitSize_NarrowDisplay_FitsTheWidth()
        {
            Vector2Int size = ToolkitPortraitWindow.GetPortraitSize(new Vector2Int(300, 1080));
            Assert.AreEqual(300, size.x);
            Assert.AreEqual(Mathf.RoundToInt(300 / ToolkitPortraitWindow.PhoneAspect), size.y);
        }

        [Test]
        public void Init_Available_ShowsButtonLabelledPhoneView()
        {
            Button button = _view.root.Q<Button>(ToolkitPortraitWindow.ButtonName);
            Assert.IsFalse(button.ClassListContains("is-hidden"));
            Assert.AreEqual("Phone view", button.text);
        }

        [Test]
        public void Init_Unavailable_HidesButton()
        {
            _window.Init(_view, false, () => Display1080, () => _current, Apply);
            Assert.IsTrue(_view.root.Q(ToolkitPortraitWindow.ButtonName).ClassListContains("is-hidden"));
        }

        [Test]
        public void Toggle_FromDesktop_AppliesWindowedPortrait()
        {
            _window.Toggle();
            Assert.IsTrue(_window.isPortrait);
            Assert.AreEqual(1, _applied.Count);
            Assert.AreEqual(449, _applied[0].width);
            Assert.AreEqual(972, _applied[0].height);
            Assert.AreEqual(FullScreenMode.Windowed, _applied[0].mode);
            Assert.AreEqual("Desktop view", _view.root.Q<Button>(ToolkitPortraitWindow.ButtonName).text);
        }

        [Test]
        public void Toggle_Twice_RestoresThePreviousWindow()
        {
            _window.Toggle();
            _window.Toggle();
            Assert.IsFalse(_window.isPortrait);
            Assert.AreEqual(2, _applied.Count);
            Assert.AreEqual(1920, _applied[1].width);
            Assert.AreEqual(1080, _applied[1].height);
            Assert.AreEqual(FullScreenMode.FullScreenWindow, _applied[1].mode);
            Assert.AreEqual("Phone view", _view.root.Q<Button>(ToolkitPortraitWindow.ButtonName).text);
        }

        [Test]
        public void Toggle_AfterSceneChange_StillRestoresTheFirstWindow()
        {
            _window.Toggle();
            // The next scene's HUD builds its own instance while the window is still portrait
            _window.Dispose();
            _window = new ToolkitPortraitWindow();
            _window.Init(_view, true, () => Display1080, () => _current, Apply);
            Assert.AreEqual("Desktop view", _view.root.Q<Button>(ToolkitPortraitWindow.ButtonName).text);
            _window.Toggle();
            Assert.AreEqual(1920, _applied[_applied.Count - 1].width);
            Assert.AreEqual(FullScreenMode.FullScreenWindow, _applied[_applied.Count - 1].mode);
        }

        [Test]
        public void Init_LaunchedInPortraitWindow_IsPortraitLabelledDesktopView()
        {
            _current = new ToolkitPortraitWindow.WindowState(449, 972, FullScreenMode.Windowed);
            _window.Init(_view, true, () => Display1080, () => _current, Apply);
            Assert.IsTrue(_window.isPortrait);
            Assert.AreEqual("Desktop view", _view.root.Q<Button>(ToolkitPortraitWindow.ButtonName).text);
        }

        [Test]
        public void Toggle_LaunchedInPortraitWindow_OpensADesktopWindow()
        {
            _current = new ToolkitPortraitWindow.WindowState(449, 972, FullScreenMode.Windowed);
            _window.Toggle();
            Assert.AreEqual(1, _applied.Count);
            Assert.AreEqual(1536, _applied[0].width, "80% of the display width");
            Assert.AreEqual(864, _applied[0].height, "80% of the display height");
            Assert.AreEqual(FullScreenMode.Windowed, _applied[0].mode);
            Assert.IsFalse(_window.isPortrait);
        }
    }
}
