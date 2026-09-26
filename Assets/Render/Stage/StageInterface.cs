using HealerLike.Render.Creatures;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        RenderManager _manager;
        BattleFocus _focus;
        ToolkitGameUI _ui;
        Rect _viewport;
        float _aspect;
        Camera _camera;
        CreaturePortraits _portraits;
        CreatureLooks _portraitLooks;
        PrimitiveMeshes _portraitMeshes;
        Scene _uiScene;

        public ToolkitGameUI ui { get { return _ui; } }
        public CreaturePortraits portraits { get { return _portraits; } }

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
            if (scene.path != GameplayPath && scene.path != MenuPath)
            {
                return;
            }
            ReleasePortraits();
            _uiScene = scene;
#if UNITY_ANDROID && !UNITY_EDITOR
            StageLegacyInput.Configure(scene);
#endif

            _ui = FindInScene<ToolkitGameUI>(scene);
            if (_ui == null)
            {
                GameObject host = new GameObject("Render Interface");
                SceneManager.MoveGameObjectToScene(host, scene);
                _ui = host.AddComponent<ToolkitGameUI>();
            }

            _ui.gameplayScene = "Main";
            _ui.menuScene = "MenuToolkit";
            _ui.sceneLoader = LoadScene;
            if (scene.path == GameplayPath)
            {
                CreatePortraits();
                StageTouchInput touch = _ui.GetComponent<StageTouchInput>();
                if (touch == null) touch = _ui.gameObject.AddComponent<StageTouchInput>();
                touch.Init(FindInScene<InteractionManager>(scene));
                _ui.SetBattleFocus(false, _focus.Toggle);
            }
            _focus.ShowLegacyControl(false);
            _camera = null;
        }

        public void RefreshCreatureIcons()
        {
            if (_portraits == null) return;
            if (_portraitLooks != _manager.creatureLooks || _portraitMeshes != _manager.meshes)
            {
                ReleasePortraits();
                CreatePortraits();
            }
            else _portraits.Invalidate();
        }

        void CreatePortraits()
        {
            if (_ui == null) return;
            _portraitLooks = _manager.creatureLooks;
            _portraitMeshes = _manager.meshes;
            _portraits = new CreaturePortraits(_portraitLooks, _portraitMeshes);
            _ui.SetIconProvider(_portraits);
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (scene == _uiScene) ReleasePortraits();
        }

        void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ReleasePortraits();
        }

        void ReleasePortraits()
        {
            if (_ui != null) _ui.SetIconProvider(null);
            _portraits?.Dispose();
            _portraits = null;
            _portraitLooks = null;
            _portraitMeshes = null;
        }

        void LateUpdate()
        {
            if (_ui == null || _manager.entityManager == null || _manager.gameCamera == null)
            {
                return;
            }

            Camera camera = _manager.gameCamera;
            float aspect = (float)camera.pixelWidth / Mathf.Max(1, camera.pixelHeight);
            Rect viewport = _ui.normalizedWorldViewport;
            if (viewport.width > 0.1f && viewport.height > 0.1f
                && (_camera != camera || _viewport != viewport || !Mathf.Approximately(_aspect, aspect)))
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
            return null;
        }

        bool LoadScene(string scene)
        {
            string path = ScenePath(scene);
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

        static ComponentType FindInScene<ComponentType>(Scene scene) where ComponentType : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ComponentType found = root.GetComponentInChildren<ComponentType>(true);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
