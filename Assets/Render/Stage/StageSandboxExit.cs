using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // The sandbox panel has no way out, so the stage adds one: a Toolkit "Menu" button in the top corner of the
    // safe area, away from the panel along the bottom, and the Android back gesture (legacy Escape). Both leave for the
    // menu through the stage's own loader, the route the run's "Return to menu" takes, and restore normal time first,
    // since the sandbox pause and slow motion set Time.timeScale and it survives the scene load.
    // Runs before InteractionManager so Escape still cancels an armed placement first, as the sandbox hint says.
    [DefaultExecutionOrder(-3000)]
    public class StageSandboxExit : MonoBehaviour
    {
        public static readonly string HostName = "Render Sandbox Exit";
        public static readonly string ButtonName = "sandbox-menu-button";
        public static readonly string ButtonText = "Menu";
        public static readonly float Margin = 8f;
        public static readonly string StyleSheetName = "RenderSandboxExit";

        Func<string, bool> _loader;
        string _menuScene;
        InteractionManager _interaction;
        VisualElement _overlay;
        Button _button;
        UIDocument _document;
        PanelSettings _panel;
        Vector2Int _screen;
        Rect _safeArea;

        public VisualElement overlay { get { return _overlay; } }
        public Button button { get { return _button; } }
        public string menuScene { get { return _menuScene; } }

        // One exit per sandbox scene, living in it, so it unloads with the sandbox and never reaches a Main run
        public static StageSandboxExit Attach(Scene scene, Func<string, bool> loader, string menuScene)
        {
            StageSandboxExit exit = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == HostName)
                {
                    exit = root.GetComponent<StageSandboxExit>();
                }
            }

            if (exit == null)
            {
                GameObject host = new GameObject(HostName);
                SceneManager.MoveGameObjectToScene(host, scene);
                exit = host.AddComponent<StageSandboxExit>();
            }

            exit.Init(loader, menuScene, StageSceneObjects.Find<InteractionManager>(scene));
            return exit;
        }

        public void Init(Func<string, bool> loader, string menuScene, InteractionManager interaction)
        {
            _loader = loader;
            _menuScene = menuScene;
            _interaction = interaction;
            if (_overlay == null)
            {
                _overlay = CreateOverlay(out _button);
                _button.clicked += OnClicked;
            }
        }

        // A full-screen layer that never takes a touch itself, holding one button in the Toolkit theme and the menu's
        // pill look
        public static VisualElement CreateOverlay(out Button button)
        {
            VisualElement overlay = new VisualElement { name = "sandbox-exit-overlay", pickingMode = PickingMode.Ignore };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.right = 0f;
            overlay.style.bottom = 0f;
            StyleSheet style = Resources.Load<StyleSheet>(StyleSheetName);
            if (style != null)
            {
                overlay.styleSheets.Add(style);
            }

            button = new Button { name = ButtonName, text = ButtonText };
            button.AddToClassList("button");
            button.style.position = Position.Absolute;
            button.style.left = Margin;
            button.style.top = Margin;
            overlay.Add(button);
            return overlay;
        }

        // The button's top-left corner in panel units: inside the safe area, a margin from its top-left edge
        public static Vector2 ButtonPosition(int width, int height, Rect safeArea, float scale)
        {
            Rect safe = ToolkitScreenLayout.GetSafePanelRect(width, height, safeArea, scale);
            return new Vector2(safe.xMin + Margin, safe.yMin + Margin);
        }

        void OnClicked()
        {
            Leave();
        }

        // Normal time, then the menu through the stage's loader. False when there is nothing to load it with.
        public bool Leave()
        {
            Time.timeScale = 1f;
            return _loader != null && !string.IsNullOrEmpty(_menuScene) && _loader.Invoke(_menuScene);
        }

        // Back or Escape: an armed placement or removal is cancelled by the sandbox itself, the next press leaves
        public bool Back()
        {
            if (_interaction != null && _interaction.GetInteraction() != null)
            {
                return false;
            }

            return Leave();
        }

        void Start()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                _document = gameObject.AddComponent<UIDocument>();
            }

            _panel = ToolkitTheme.CreatePanelSettings(null, null);
            _panel.name = "Sandbox exit panel";
            _panel.sortingOrder = 1000;
            _document.panelSettings = _panel;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Back();
            }

            Present();
        }

        void Present()
        {
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root == null || _overlay == null)
            {
                return;
            }

            if (_overlay.parent != root)
            {
                root.pickingMode = PickingMode.Ignore;
                ToolkitTheme.Apply(root, null);
                root.Add(_overlay);
                _screen = Vector2Int.zero;
            }

            Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
            if (screen == _screen && Screen.safeArea == _safeArea)
            {
                return;
            }

            _screen = screen;
            _safeArea = Screen.safeArea;
            float scale = ToolkitScreenLayout.GetScale(screen.x, screen.y, Application.isMobilePlatform);
            _panel.scale = scale;
            Vector2 position = ButtonPosition(screen.x, screen.y, _safeArea, scale);
            _button.style.left = position.x;
            _button.style.top = position.y;
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.clicked -= OnClicked;
            }

            if (_document != null && _document.panelSettings == _panel)
            {
                _document.panelSettings = null;
            }

            if (_panel != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_panel);
                }
                else
                {
                    DestroyImmediate(_panel);
                }

                _panel = null;
            }
        }
    }
}
