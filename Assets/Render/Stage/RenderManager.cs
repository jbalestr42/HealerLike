using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
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
        static readonly int gridOriginId = Shader.PropertyToID("_HLGridOrigin");
        static readonly int gridCellId = Shader.PropertyToID("_HLGridCell");
        static readonly int gridExtentId = Shader.PropertyToID("_HLGridExtent");
        static readonly int gridStrengthId = Shader.PropertyToID("_HLGridStrength");

        [SerializeField] CreatureLooks _creatureLooks;
        [SerializeField] SpellLooks _spellLooks;
        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] RenderPipelineAsset _pipeline;
        [SerializeField] Material _groundMaterial;
        [SerializeField] Color _backgroundColor = new Color32(191, 210, 224, 255);
        [SerializeField] Color _ambientColor = new Color(0.35f, 0.4f, 0.5f);
        [SerializeField] List<string> _hiddenObjectNames = new List<string>();
        [SerializeField] float _gridStrength = 0.12f;
        [SerializeField] GameObject _environmentPrefab;
        [SerializeField] LookController _look;
        [SerializeField] ZoneRegistry _zones;
        [SerializeField] GrassField _grass;
        [SerializeField] SpellVisualSink _spellSink;
        [SerializeField] StoneEffects _stoneEffects;
        [SerializeField] StoneDeathBridge _stoneDeath;
        [SerializeField] BattleFocus _battleFocus;
        [SerializeField] StageRangeDriver _rangeDriver;
        [SerializeField] StageKeyLight _keyLight;

        RenderRegistry _registry = new RenderRegistry();
        RenderPipelineAsset _previousPipeline;
        Scene _scene;
        Renderer _boardGround;
        GameObject _environment;
        EnvironmentGrass _environmentGrass;
        EnvironmentRidge _ridge;
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

        EnvironmentGust _gust;
        public EnvironmentGust gust { get { return _gust; } }

        EnvironmentForeground _foreground;
        public EnvironmentForeground foreground { get { return _foreground; } }

        bool _isLandscape = false;
        public bool isLandscape { get { return _isLandscape; } }

        public Pose overviewPose { get { return _isLandscape ? _landscapePose : _portraitPose; } }

        public RenderRegistry registry { get { return _registry; } }
        public ZoneRegistry zones { get { return _zones; } }
        public GrassField grass { get { return _grass; } }
        public LookController look { get { return _look; } }
        public SpellVisualSink spellSink { get { return _spellSink; } }
        public StoneEffects stoneEffects { get { return _stoneEffects; } }
        public StoneMeshCache stoneMeshes { get { return _stoneEffects.stoneMeshes; } }
        public CreatureLooks creatureLooks { get { return _creatureLooks; } }
        public SpellLooks spellLooks { get { return _spellLooks; } }
        public PrimitiveMeshes meshes { get { return _meshes; } }

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
            _look.Init(StageCalibration.BackgroundFog(_gameCamera.transform.position, _board));
            _zones.Init();
            Rect boardRect = BoardRect();
            _grass.Init(boardRect, player.grid.size, _board.max.y, _gameCamera, _zones.buffer, GrassField.MaxZones);
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

        // Landscape keeps the wide framing, the look is calibrated for portrait
        public void SetLandscape(bool isLandscape)
        {
            _isLandscape = isLandscape;
            if (_gameCamera == null)
            {
                return;
            }

            _gameCamera.aspect = isLandscape ? StageCalibration.LandscapeAspect : StageCalibration.PortraitAspect;
            _gameCamera.transform.SetPositionAndRotation(overviewPose.position, overviewPose.rotation);
            _look.Init(StageCalibration.BackgroundFog(_gameCamera.transform.position, _board));
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

            // The game's unparented Instantiate calls land in the active scene and unload with it
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
            _gameCamera.fieldOfView = StageCalibration.PortraitFov;
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
            Renderer groundRenderer = grid.ground ? grid.ground.GetComponent<Renderer>() : null;
            _boardGround = groundRenderer;
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
            Vector3 boardSize = new Vector3(grid.width * grid.size, 0f, grid.height * grid.size);
            _board = new Bounds(new Vector3(center.x, surfaceY, center.z), boardSize);
            _portraitPose = StageCalibration.PlayableFrame(_board, StageCalibration.PortraitPitch,
                                                             StageCalibration.PortraitFov, StageCalibration.PortraitAspect,
                                                             StageCalibration.PortraitCentreY);
            _landscapePose = StageCalibration.PlayableFrame(_board, StageCalibration.LandscapePitch,
                                                              StageCalibration.PortraitFov, StageCalibration.LandscapeAspect,
                                                              StageCalibration.LandscapeCentreY);
            _gameCamera.aspect = _isLandscape ? StageCalibration.LandscapeAspect : StageCalibration.PortraitAspect;
            _gameCamera.transform.SetPositionAndRotation(overviewPose.position, overviewPose.rotation);

            // The board ground draws the cell grid from these
            Shader.SetGlobalVector(gridOriginId, _board.min);
            Shader.SetGlobalFloat(gridCellId, grid.size);
            Shader.SetGlobalVector(gridExtentId, _board.size);
            Shader.SetGlobalFloat(gridStrengthId, _gridStrength);

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
                    // The scene's far ground shares the board ground's name, the board itself stays
                    bool isNamed = child.name == objectName;
                    if (isNamed && child.TryGetComponent(out Renderer hidden) && hidden != _boardGround)
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
            _environmentGrass = _environment.GetComponent<EnvironmentGrass>();
            _gust = _environment.GetComponent<EnvironmentGust>();
            _foreground = _environment.GetComponentInChildren<EnvironmentForeground>();
            _ridge = _environment.GetComponentInChildren<EnvironmentRidge>();

            // The plane sits a hair under the board top
            Transform ground = _environment.transform.Find("Ground");
            if (ground != null)
            {
                ground.position = new Vector3(_board.center.x, surfaceY - 0.01f, _board.center.z);
            }

            LookSettings lookSettings = _look.settings;
            _environmentGrass.Init(boardRect, _player.grid.size, surfaceY, _gameCamera, _zones, this);
            EnvironmentScatter scatter = _environment.GetComponent<EnvironmentScatter>();
            scatter.Init(boardRect, _player.grid.size, surfaceY, _gameCamera, _gust, lookSettings.fogEnd, this);
            _foreground.Init(_gameCamera, surfaceY, this);
            _ridge.Init(_gameCamera, boardRect, surfaceY, lookSettings.fogStart, lookSettings.fogEnd, this);
        }

        void Subscribe()
        {
            _entityManager.OnEntitySpawned.AddListener(OnEntitySpawned);
            _entityManager.OnEntityKilled.AddListener(OnEntityKilled);
            _player.OnCharacterInit.AddListener(OnCharacterInit);
            AscensionGameType.OnRoundEnd.AddListener(OnRoundEnd);
            _entityManager.OnProjectileSpawned.AddListener(OnProjectileSpawned);
            _entityManager.OnAreaOfEffectStarted.AddListener(OnAreaOfEffectStarted);

            GameView gameView = FindInScene<GameView>(_scene);
            _nextWaveButton = null;
            if (gameView != null && gameView.gameHUD != null)
            {
                _nextWaveButton = gameView.gameHUD.nextWaveButton;
            }

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
                _entityManager.OnProjectileSpawned.RemoveListener(OnProjectileSpawned);
                _entityManager.OnAreaOfEffectStarted.RemoveListener(OnAreaOfEffectStarted);
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

            Shader.SetGlobalFloat(gridStrengthId, 0f);

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

            // The model's sockets and HUD stay live, so projectiles leave from where gameplay puts them
            foreach (Renderer modelRenderer in entity.model.GetComponentsInChildren<Renderer>(true))
            {
                modelRenderer.enabled = false;
            }

            GameObject viewPrefab = _creatureLooks.GetView(entity.data, entity.entityType);
            GameObject viewGo = Instantiate(viewPrefab, entity.model.transform);
            foreach (IEntityView view in viewGo.GetComponentsInChildren<IEntityView>())
            {
                view.Init(entity, this);
            }

            foreach (RangePreview preview in viewGo.GetComponentsInChildren<RangePreview>())
            {
                _rangeDriver.Add(preview);
            }

            _battleFocus.MarkDirty();
        }

        void OnCharacterInit(Character character)
        {
            GameObject viewGo = Instantiate(_creatureLooks.GetView(character.data), character.transform);
            viewGo.GetComponent<CharacterView>().Init(character, this);
            foreach (HealPulse pulse in viewGo.GetComponentsInChildren<HealPulse>())
            {
                pulse.Init(character.gameObject, _registry, _zones);
            }

            foreach (TrampleZone trample in viewGo.GetComponentsInChildren<TrampleZone>())
            {
                trample.InitFootprint(_zones);
            }
        }

        void OnEntityKilled(Entity entity)
        {
            if (entity != null)
            {
                _stoneDeath.HandleDeparture(entity);
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

        // EntityManager raises it before Projectile.Init, which then calls Init(source) on these with the
        // projectile's own behaviours
        void OnProjectileSpawned(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            GameObject projectileGo = projectile.gameObject;
            ProjectileLook projectileLook = _spellLooks.GetSpawnedLook(projectile);
            projectileGo.AddComponent<ProjectileVisualObserver>().Init(this, projectileLook);
            projectileGo.AddComponent<StoneProjectileImpactBridge>();
            projectileGo.AddComponent<LaunchWave>().Init(_zones);
            projectileGo.AddComponent<StageLaunchGust>().Init(_gust);
            if (projectile is ChainLightningProjectile)
            {
                projectileGo.AddComponent<ChainContactVisual>().Init(this);
            }

            foreach (LineRenderer line in projectileGo.GetComponentsInChildren<LineRenderer>(true))
            {
                line.enabled = false;
            }
        }

        void OnAreaOfEffectStarted(AreaOfEffect area)
        {
            area.gameObject.AddComponent<AreaPulse>().Init(_zones);
            area.gameObject.AddComponent<LegacyAreaVisualMask>();
        }

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
