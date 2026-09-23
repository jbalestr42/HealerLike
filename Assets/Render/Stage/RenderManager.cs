using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HealerLike.Render.Creatures;
using HealerLike.Render.Environment;
using HealerLike.Render.Grass;
using HealerLike.Render.Look;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // Attaches the render layer to every loaded scene that holds an EntityManager, and draws its entities
    public class RenderManager : MonoBehaviour
    {
        [SerializeField] CreatureLooks _creatureLooks;
        [SerializeField] SpellLooks _spellLooks;
        [SerializeField] HLPrimitiveMeshes _meshes;
        [SerializeField] RenderPipelineAsset _pipeline;
        [SerializeField] Material _groundMaterial;
        [SerializeField] Color _backgroundColor = new Color32(191, 210, 224, 255);
        [SerializeField] Color _ambientColor = new Color(0.35f, 0.4f, 0.5f);
        [SerializeField] List<string> _hiddenObjectNames = new List<string>();
        [SerializeField] GameObject _environmentPrefab;
        [SerializeField] HLLookController _look;
        [SerializeField] HLZoneRegistry _zones;
        [SerializeField] HLGrassField _grass;
        [SerializeField] HLSpellVisualSink _spellSink;
        [SerializeField] HLStoneEffects _stoneEffects;
        [SerializeField] HLStoneDeathBridge _stoneDeath;
        [SerializeField] HLBattleFocus _battleFocus;
        [SerializeField] HLStageRangeDriver _rangeDriver;
        [SerializeField] HLStageKeyLight _keyLight;

        HLRenderRegistry _registry = new HLRenderRegistry();
        RenderPipelineAsset _previousPipeline;
        Scene _scene;
        GameObject _environment;
        HLEnvironmentGrass _environmentGrass;
        HLEnvironmentRidge _ridge;
        Button _nextWaveButton;
        Pose _portraitPose;
        Pose _landscapePose;
        int _deliveryToken;
        bool _isPipelineSwapped = false;

        EntityManager _entityManager;
        public EntityManager entityManager { get { return _entityManager; } }

        PlayerBehaviour _player;
        public PlayerBehaviour player { get { return _player; } }

        Camera _gameCamera;
        public Camera gameCamera { get { return _gameCamera; } }

        Bounds _board;
        public Bounds board { get { return _board; } }

        HLEnvironmentGust _gust;
        public HLEnvironmentGust gust { get { return _gust; } }

        HLEnvironmentForeground _foreground;
        public HLEnvironmentForeground foreground { get { return _foreground; } }

        bool _isLandscape = false;
        public bool isLandscape { get { return _isLandscape; } }

        public Pose overviewPose { get { return _isLandscape ? _landscapePose : _portraitPose; } }

        public HLRenderRegistry registry { get { return _registry; } }
        public HLZoneRegistry zones { get { return _zones; } }
        public HLGrassField grass { get { return _grass; } }
        public HLLookController look { get { return _look; } }
        public HLSpellVisualSink spellSink { get { return _spellSink; } }
        public HLStoneEffects stoneEffects { get { return _stoneEffects; } }
        public StoneMeshCache stoneMeshes { get { return _stoneEffects.stoneMeshes; } }
        public CreatureLooks creatureLooks { get { return _creatureLooks; } }
        public SpellLooks spellLooks { get { return _spellLooks; } }
        public HLPrimitiveMeshes meshes { get { return _meshes; } }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Init(EntityManager entityManager, PlayerBehaviour player)
        {
            if (entityManager == null || player == null || player.grid == null)
            {
                Debug.LogError("[RenderManager] Init needs the EntityManager, the PlayerBehaviour and its grid.");
                return;
            }

            if (entityManager == _entityManager)
            {
                return;
            }

            Detach();
            _entityManager = entityManager;
            _player = player;
            _scene = entityManager.gameObject.scene;

            // Attach steps, all on scene instances, never on assets
            if (!AdoptCamera())
            {
                _entityManager = null;
                return;
            }

            SwapPipeline();
            SetLighting();
            SetBoard();
            Subscribe();

            // Init chain
            _look.Init(_gameCamera, _board);
            _zones.Init();
            _registry.Init(_spellSink, _zones);
            Rect boardRect = BoardRect();
            _grass.Init(boardRect, player.grid.size, _board.max.y, _gameCamera, _zones.buffer, HLGrassField.MaxZones);
            InitEnvironment(boardRect);
            _spellSink.Init(this);
            _battleFocus.Init(this);
            _rangeDriver.Init(_gameCamera);
            _keyLight.Init();
            Debug.Log($"[RenderManager] Attached to {_scene.name}");
        }

        void LateUpdate()
        {
            if (_entityManager == null)
            {
                return;
            }

            // Producers moved their zones in Update, so the frame's zones are final here
            _zones.PublishFrame(Time.deltaTime);
            _grass.UpdateField(_zones);
            _environmentGrass.UpdateStrips(_zones);
        }

        // One counter for every projectile, so a token never names two deliveries on one rig
        public int NextDeliveryToken()
        {
            if (++_deliveryToken == 0)
            {
                ++_deliveryToken;
            }

            return _deliveryToken;
        }

        // Landscape keeps the wave 3 framing, the look is calibrated for portrait
        public void SetLandscape(bool isLandscape)
        {
            _isLandscape = isLandscape;
            if (_gameCamera == null)
            {
                return;
            }

            _gameCamera.aspect = isLandscape ? 16f / 9f : HLStageCalibration.PortraitAspect;
            _gameCamera.transform.SetPositionAndRotation(overviewPose.position, overviewPose.rotation);
            _look.Init(_gameCamera, _board);
            _foreground.Build();
            _ridge.Build();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EntityManager entityManager = FindInScene<EntityManager>(scene);
            if (entityManager == null)
            {
                return;
            }

            // His unparented Instantiate calls land in the active scene and unload with it
            SceneManager.SetActiveScene(scene);
            Init(entityManager, FindInScene<PlayerBehaviour>(scene));
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (scene == _scene)
            {
                Detach();
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Detach();
            if (_isPipelineSwapped)
            {
                QualitySettings.renderPipeline = _previousPipeline;
                _isPipelineSwapped = false;
            }
        }

        bool AdoptCamera()
        {
            _gameCamera = Camera.main;
            if (_gameCamera == null)
            {
                Debug.LogError($"[RenderManager] {_scene.name} has no main camera to adopt.");
                return false;
            }

            // Cinemachine would overwrite the overview pose on the next frame
            CinemachineBrain brain = _gameCamera.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            _gameCamera.clearFlags = CameraClearFlags.SolidColor;
            _gameCamera.backgroundColor = _backgroundColor;
            _gameCamera.fieldOfView = HLStageCalibration.PortraitFov;
            _gameCamera.nearClipPlane = 0.1f;
            _gameCamera.farClipPlane = 200f;
            _gameCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            return true;
        }

        void SwapPipeline()
        {
            if (_isPipelineSwapped)
            {
                return;
            }

            _previousPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = _pipeline;
            _isPipelineSwapped = true;
        }

        void SetLighting()
        {
            foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (sceneLight.type == LightType.Directional && sceneLight.gameObject.scene == _scene)
                {
                    sceneLight.enabled = false;
                }
            }

            RenderSettings.sun = _keyLight.keyLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _ambientColor;
        }

        void SetBoard()
        {
            GridManager grid = _player.grid;
#if HEALERLIKE_SEAMS
            Renderer groundRenderer = grid.ground.GetComponent<Renderer>();
#else
            // TODO: read GridManager.ground once S5 lands, until then the ground is the grid's one renderer
            Renderer groundRenderer = grid.GetComponentInChildren<Renderer>();
#endif
            float surfaceY = 0.5f;
            if (groundRenderer != null)
            {
                surfaceY = groundRenderer.bounds.max.y;
                groundRenderer.sharedMaterial = _groundMaterial;
            }
            else
            {
                Debug.LogError("[RenderManager] The grid has no ground renderer, the board keeps its own look.");
            }

            Vector3 center = grid.transform.position;
            _board = new Bounds(new Vector3(center.x, surfaceY, center.z), new Vector3(grid.width * grid.size, 0f, grid.height * grid.size));
            _portraitPose = HLStageCalibration.PlayableFrame(_board, HLStageCalibration.PortraitPitch, HLStageCalibration.PortraitFov,
                                                             HLStageCalibration.PortraitAspect, HLStageCalibration.PortraitCentreY);
            _landscapePose = HLStageCalibration.PlayableFrame(_board, 46f, HLStageCalibration.PortraitFov, 16f / 9f, 0.46f);
            _gameCamera.aspect = _isLandscape ? 16f / 9f : HLStageCalibration.PortraitAspect;
            _gameCamera.transform.SetPositionAndRotation(overviewPose.position, overviewPose.rotation);

            foreach (string hiddenName in _hiddenObjectNames)
            {
                if (!Hide(hiddenName))
                {
                    Debug.LogError($"[RenderManager] No object named {hiddenName} in {_scene.name} to hide.");
                }
            }
        }

        bool Hide(string objectName)
        {
            bool isFound = false;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName && child.TryGetComponent(out Renderer hidden))
                    {
                        hidden.enabled = false;
                        isFound = true;
                    }
                }
            }

            return isFound;
        }

        void InitEnvironment(Rect boardRect)
        {
            float surfaceY = _board.max.y;
            _environment = Instantiate(_environmentPrefab, transform);
            _environmentGrass = _environment.GetComponent<HLEnvironmentGrass>();
            _gust = _environment.GetComponent<HLEnvironmentGust>();
            _foreground = _environment.GetComponentInChildren<HLEnvironmentForeground>();
            _ridge = _environment.GetComponentInChildren<HLEnvironmentRidge>();

            // The plane sits a hair under the board top
            Transform ground = _environment.transform.Find("Ground");
            if (ground != null)
            {
                ground.position = new Vector3(_board.center.x, surfaceY - 0.01f, _board.center.z);
            }

            HLLookSettings lookSettings = _look.settings;
            _environmentGrass.Init(boardRect, _player.grid.size, surfaceY, _gameCamera, _zones, this);
            _environment.GetComponent<HLEnvironmentScatter>().Init(boardRect, _player.grid.size, surfaceY, _gameCamera, _gust,
                                                                   lookSettings.fogEnd, this);
            _foreground.Init(_gameCamera, surfaceY, this);
            _ridge.Init(_gameCamera, boardRect, surfaceY, lookSettings.fogStart, lookSettings.fogEnd, this);
        }

        void Subscribe()
        {
            _entityManager.OnEntitySpawned.AddListener(OnEntitySpawned);
            _entityManager.OnEntityKilled.AddListener(OnEntityKilled);
            _player.OnCharacterInit.AddListener(OnCharacterInit);
            AscensionGameType.OnRoundEnd.AddListener(OnRoundEnd);
#if HEALERLIKE_SEAMS
            _entityManager.OnProjectileSpawned.AddListener(OnProjectileSpawned);
            _entityManager.OnAreaOfEffectStarted.AddListener(OnAreaOfEffectStarted);
#endif

            GameView gameView = FindInScene<GameView>(_scene);
            _nextWaveButton = gameView != null && gameView.gameHUD != null ? gameView.gameHUD.nextWaveButton : null;
            if (_nextWaveButton != null)
            {
                _nextWaveButton.onClick.AddListener(OnNextWave);
            }
        }

        void Detach()
        {
            AscensionGameType.OnRoundEnd.RemoveListener(OnRoundEnd);
            if (_entityManager != null)
            {
                _entityManager.OnEntitySpawned.RemoveListener(OnEntitySpawned);
                _entityManager.OnEntityKilled.RemoveListener(OnEntityKilled);
#if HEALERLIKE_SEAMS
                _entityManager.OnProjectileSpawned.RemoveListener(OnProjectileSpawned);
                _entityManager.OnAreaOfEffectStarted.RemoveListener(OnAreaOfEffectStarted);
#endif
            }

            if (_player != null)
            {
                _player.OnCharacterInit.RemoveListener(OnCharacterInit);
            }

            if (_nextWaveButton != null)
            {
                _nextWaveButton.onClick.RemoveListener(OnNextWave);
            }

            if (_environment != null)
            {
                Destroy(_environment);
            }

            // The children can go first when the manager itself is destroyed
            if (_grass != null)
            {
                _grass.Release();
            }

            if (_spellSink != null)
            {
                _spellSink.Clear();
            }

            if (_rangeDriver != null)
            {
                _rangeDriver.Clear();
            }

            _entityManager = null;
            _player = null;
            _nextWaveButton = null;
            _environment = null;
        }

        void OnEntitySpawned(Entity entity)
        {
            if (entity.model == null)
            {
                return;
            }

            // His sockets and HUD stay live, so projectiles leave from where gameplay puts them
            foreach (Renderer modelRenderer in entity.model.GetComponentsInChildren<Renderer>(true))
            {
                modelRenderer.enabled = false;
            }

            GameObject viewGo = Instantiate(_creatureLooks.GetView(entity.data, entity.entityType), entity.model.transform);
            foreach (IEntityView view in viewGo.GetComponentsInChildren<IEntityView>())
            {
                view.Init(entity, this);
            }

            foreach (HLRangePreview preview in viewGo.GetComponentsInChildren<HLRangePreview>())
            {
                _rangeDriver.Add(preview);
            }

            _battleFocus.MarkDirty();
        }

        void OnCharacterInit(Character character)
        {
            GameObject viewGo = Instantiate(_creatureLooks.GetView(character.data), character.transform);
            viewGo.GetComponent<HLCharacterView>().Init(character, this);
            foreach (HLHealPulse pulse in viewGo.GetComponentsInChildren<HLHealPulse>())
            {
                pulse.Init(character.gameObject, _registry, _zones);
            }

            foreach (HLTrampleZone trample in viewGo.GetComponentsInChildren<HLTrampleZone>())
            {
                trample.InitFootprint(_zones);
            }
        }

        void OnEntityKilled(Entity entity)
        {
            if (entity != null)
            {
                _stoneDeath.HandleDeparture(entity.health, entity.GetComponentInChildren<HLStoneEnemyVisual>());
            }

            _battleFocus.MarkDirty();
        }

        void OnNextWave()
        {
            _battleFocus.Focus();
        }

        void OnRoundEnd()
        {
            _battleFocus.Overview();
        }

#if HEALERLIKE_SEAMS
        // S1: added before Projectile.Init, which then calls their Init(source) with his own behaviours
        void OnProjectileSpawned(GameObject prefab, GameObject projectileGo)
        {
            ProjectileLook projectileLook = _spellLooks.GetProjectileLook(prefab);
            projectileGo.AddComponent<HLProjectileVisualObserver>().Init(this, projectileLook);
            projectileGo.AddComponent<HLStoneProjectileImpactBridge>();
            projectileGo.AddComponent<HLLaunchWave>().Init(_zones, _grass);
            projectileGo.AddComponent<HLStageLaunchGust>().Init(_gust);
            if (projectileGo.GetComponent<ChainLightningProjectile>() != null)
            {
                projectileGo.AddComponent<HLChainContactVisual>().Init(this);
            }

            foreach (LineRenderer line in projectileGo.GetComponentsInChildren<LineRenderer>(true))
            {
                line.enabled = false;
            }
        }

        // S2
        void OnAreaOfEffectStarted(AreaOfEffect area)
        {
            area.gameObject.AddComponent<HLAreaPulse>().Init(_zones);
            area.gameObject.AddComponent<HLLegacyAreaVisualMask>();
        }
#endif

        Rect BoardRect()
        {
            return new Rect(_board.min.x, _board.min.z, _board.size.x, _board.size.z);
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
