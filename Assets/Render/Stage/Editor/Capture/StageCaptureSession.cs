using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // One UI proof owns its input override, viewport size and temporary map settings across scene changes.
    public class StageCaptureSession : IDisposable
    {
        readonly RenderManager _manager;
        readonly StageInterfaceOutput _output;
        readonly StageInterfaceActions _actions = new StageInterfaceActions();
        readonly float _timeScale = Time.timeScale;
        readonly ThemeStyleSheet _selectedTheme;
        StageCaptureTheme _theme;
        StageGameViewSize _size;
        ToolkitGameUI _attachedUi;
        Func<Rect> _safeArea;
        public RenderManager manager
        {
            get
            {
                return _manager;
            }
        }

        public StageInterfaceOutput output
        {
            get
            {
                return _output;
            }
        }

        public StageInterfaceActions actions
        {
            get
            {
                return _actions;
            }
        }

        public InteractionManager interaction { get; private set; }
        public StageMapFixture mapFixture { get; set; }

        public StageCaptureSession(RenderManager manager, StageInterfaceOutput output, ThemeStyleSheet theme = null)
        {
            _manager = manager;
            _output = output;
            _selectedTheme = theme;
        }

        public void AttachInput()
        {
            RestoreSafeArea();
            _actions.ui = UnityEngine.Object.FindAnyObjectByType<ToolkitGameUI>();
            _output.Check(_actions.ui != null, "RenderStage attached Toolkit interface");
            _output.Check(UnityEngine.Object.FindObjectsByType<ToolkitGameUI>().Length == 1,
                "Exactly one Toolkit UI host");
            _attachedUi = _actions.ui;
            _safeArea = _attachedUi.safeAreaProvider;
            if (_selectedTheme != null)
            {
                UIDocument document = _attachedUi.GetComponent<UIDocument>();
                _theme = new StageCaptureTheme(document.panelSettings, document.rootVisualElement, _selectedTheme);
            }

            _actions.ConfigureLegacyInput();
            interaction = UnityEngine.Object.FindAnyObjectByType<InteractionManager>();
        }

        public IEnumerator Resize(int width, int height)
        {
            if (_size != null)
            {
                _size.Dispose();
            }

            _size = new StageGameViewSize(width, height);
            yield return AStageRun.Wait(0.8f);
            _output.Check(Screen.width == width && Screen.height == height, "Game frame is " + width + "x" + height);
            _output.Check(Mathf.Abs(_manager.gameCamera.aspect - (float)width / height) < 0.01f,
                "Camera agrees with Game frame aspect");
        }

        public IEnumerator Capture(string name)
        {
            yield return _output.Capture(_actions.ui, name);
        }

        public virtual void Dispose()
        {
            RestoreSafeArea();
            _actions.Dispose();
            if (mapFixture != null)
            {
                mapFixture.Dispose();
                mapFixture = null;
            }

            if (_size != null)
            {
                _size.Dispose();
                _size = null;
            }

            Time.timeScale = _timeScale;
        }

        void RestoreSafeArea()
        {
            if (_theme != null)
            {
                _theme.Dispose();
                _theme = null;
            }

            if (_attachedUi != null)
            {
                _attachedUi.safeAreaProvider = _safeArea;
            }

            _attachedUi = null;
        }
    }
}
