using HealerLike.Render.Spells;
using HealerLike.Render.Creatures;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace HealerLike.Render.Stage
{
    // RenderStage alone opts gameplay into the Toolkit HUD. The manager survives menu and expedition changes.
    public class StageInterface : MonoBehaviour
    {
        public static readonly string GameplayPath = "Assets/Scenes/Main.unity";
        public static readonly string MenuPath = "Assets/Scenes/Toolkit/MenuToolkit.unity";
        public static readonly string SandboxPath = StageTarget.SandboxPath;
        public static readonly string SandboxScene = "Sandbox";
        public static readonly string SandboxInputName = "Render Sandbox Input";
        RenderManager _manager;
        BattleFocus _focus;
        ToolkitGameUI _ui;
        Rect _viewport;
        float _aspect;
        Camera _camera;
        CreaturePortraits _portraits;
        StageIcons _icons;
        public StageIcons icons { get { return _icons; } }
        CreatureLooks _portraitLooks;
        PrimitiveMeshes _portraitMeshes;
        SpellLooks _iconLooks;
        EffectVocabulary _iconVocabulary;
        Material _iconMaterial;
        Scene _uiScene;
        UIDocument _document;
        StyleSheet _spellSpacing;
        readonly StageManaGauge _manaGauge = new StageManaGauge();
        readonly StageIconLabels _iconLabels = new StageIconLabels();
        public ToolkitGameUI ui
        {
            get
            {
                return _ui;
            }
        }

        public CreaturePortraits portraits
        {
            get
            {
                return _portraits;
            }
        }

        void Awake()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Init(RenderManager manager, BattleFocus focus)
        {
            _manager = manager;
            _focus = focus;
        }

        public void Attach(Scene scene)
        {
            if (scene.path == SandboxPath)
            {
                AttachSandbox(scene);
                return;
            }

            if (scene.path != GameplayPath && scene.path != MenuPath)
            {
                return;
            }

            ReleasePortraits();
            _uiScene = scene;
#if UNITY_ANDROID && !UNITY_EDITOR
            StageLegacyInput.Configure(scene);
#endif
            _ui = StageSceneObjects.Find<ToolkitGameUI>(scene);
            if (_ui == null)
            {
                GameObject host = new GameObject("Render Interface");
                SceneManager.MoveGameObjectToScene(host, scene);
                _ui = host.AddComponent<ToolkitGameUI>();
            }

            _ui.gameplayScene = "Main";
            _ui.menuScene = "MenuToolkit";
            _ui.sandboxScene = SandboxScene;
            _ui.sceneLoader = LoadScene;
            _document = _ui.GetComponent<UIDocument>();
            if (_spellSpacing == null)
            {
                _spellSpacing = Resources.Load<StyleSheet>("RenderSpellSpacing");
            }
            if (scene.path == GameplayPath)
            {
                CreatePortraits();
                StageTouchInput touch = _ui.GetComponent<StageTouchInput>();
                if (touch == null)
                {
                    touch = _ui.gameObject.AddComponent<StageTouchInput>();
                }

                touch.Init(StageSceneObjects.Find<InteractionManager>(scene), StageSceneObjects.Find<PlayerBehaviour>(scene).grid);
                _ui.SetBattleFocus(false, _focus.Toggle);
            }

            _focus.ShowLegacyControl(false);
            _camera = null;
        }

        // Julien's sandbox keeps its own uGUI: no Toolkit HUD, which would hide his canvases. It still needs the
        // preview's input backend on Android and touch delivery for placing entities, as Main gets them.
        void AttachSandbox(Scene scene)
        {
            ReleasePortraits();
            _uiScene = scene;
            _ui = null;
            _document = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            StageLegacyInput.Configure(scene);
#endif
            AttachSandboxInput(scene);
            _focus.ShowLegacyControl(false);
            _camera = null;
        }

        // One touch host per sandbox scene, fed with its InteractionManager; no roster drag without the Toolkit HUD
        public static StageTouchInput AttachSandboxInput(Scene scene)
        {
            InteractionManager interaction = StageSceneObjects.Find<InteractionManager>(scene);
            if (interaction == null)
            {
                return null;
            }

            StageTouchInput touch = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == SandboxInputName)
                {
                    touch = root.GetComponent<StageTouchInput>();
                }
            }

            if (touch == null)
            {
                GameObject host = new GameObject(SandboxInputName);
                SceneManager.MoveGameObjectToScene(host, scene);
                touch = host.AddComponent<StageTouchInput>();
            }

            touch.Init(interaction);
            return touch;
        }

        public void RefreshCreatureIcons()
        {
            if (_portraits == null)
            {
                return;
            }

            SpellVisualSink sink = _manager.spellSink;
            if (_portraitLooks != _manager.creatureLooks || _portraitMeshes != _manager.meshes
                || _iconLooks != _manager.spellLooks || _iconVocabulary != (sink ? sink.vocabulary : null)
                || _iconMaterial != (sink ? sink.material : null))
            {
                ReleasePortraits();
                CreatePortraits();
            }
            else
            {
                _icons.Invalidate();
            }
        }

        void CreatePortraits()
        {
            if (_ui == null)
            {
                return;
            }

            _portraitLooks = _manager.creatureLooks;
            _portraitMeshes = _manager.meshes;
            _portraits = new CreaturePortraits(_portraitLooks, _portraitMeshes);
            SpellVisualSink sink = _manager.spellSink;
            _iconLooks = _manager.spellLooks;
            _iconVocabulary = sink ? sink.vocabulary : null;
            _iconMaterial = sink ? sink.material : null;
            SpellIcons spells = new SpellIcons(_iconVocabulary, _iconLooks, _manager.meshes, _iconMaterial);
            _icons = new StageIcons(_portraits, spells);
            _ui.SetIconProvider(_icons);
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (scene == _uiScene)
            {
                ReleasePortraits();
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _iconLabels.Dispose();
            ReleasePortraits();
        }

        void ReleasePortraits()
        {
            if (_ui != null)
            {
                _ui.SetIconProvider(null);
            }

            _icons?.Dispose();
            _icons = null;
            _portraits = null;
            _portraitLooks = null;
            _portraitMeshes = null;
            _iconLooks = null;
            _iconVocabulary = null;
            _iconMaterial = null;
        }

        void LateUpdate()
        {
            // Apply after Toolkit initializes its document, including a rebuilt HUD.
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root != null && _spellSpacing != null && !root.styleSheets.Contains(_spellSpacing))
            {
                root.styleSheets.Add(_spellSpacing);
            }

            Character character = _manager != null && _manager.player != null ? _manager.player.character : null;
            _manaGauge.Update(root, character != null ? character.mana : null);
            _iconLabels.Update(root);

            if (_ui == null || _manager.entityManager == null || _manager.gameCamera == null)
            {
                return;
            }

            Camera camera = _manager.gameCamera;
            float aspect = (float)camera.pixelWidth / Mathf.Max(1, camera.pixelHeight);
            Rect viewport = _ui.normalizedWorldViewport;
            if (viewport.width > 0.1f && viewport.height > 0.1f && (_camera != camera || _viewport != viewport
                || !Mathf.Approximately(_aspect, aspect)))
            {
                _camera = camera;
                _viewport = viewport;
                _aspect = aspect;
                _manager.FrameViewport(StageViewport.Inset(viewport, 0.015f), aspect);
                foreach (ARigHost host in _manager.entityManager.GetComponentsInChildren<ARigHost>())
                {
                    if (host.rig != null)
                    {
                        host.rig.SetPresentationForward(-camera.transform.forward);
                    }
                }
            }

            _ui.SetBattleFocus(_focus.isFocused, _focus.Toggle);
        }

        public static string ScenePath(string scene)
        {
            if (scene == "Main" || scene == "MainToolkit")
            {
                return GameplayPath;
            }

            if (scene == "MenuScene" || scene == "MenuToolkit")
            {
                return MenuPath;
            }

            if (scene == SandboxScene)
            {
                return SandboxPath;
            }

            return null;
        }

        // The path a menu request loads. A gameplay choice is recorded in StageTarget: Start explicitly goes back to
        // Main, so a visit to the sandbox never sticks; the menu itself leaves the choice as it is.
        public static string Route(string scene)
        {
            string path = ScenePath(scene);
            if (path == GameplayPath)
            {
                StageTarget.Reset();
            }
            else if (path == SandboxPath)
            {
                StageTarget.Select(SandboxPath);
            }

            return path;
        }

        bool LoadScene(string scene)
        {
            string path = Route(scene);
            if (path == null)
            {
                return false;
            }

#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                return false;
            }

            SceneManager.LoadScene(path, LoadSceneMode.Single);
#endif
            return true;
        }
    }
}
