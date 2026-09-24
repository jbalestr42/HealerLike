using UnityEngine;
using UnityEngine.SceneManagement;
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
    // Attaches the render layer to every loaded scene that holds an EntityManager: dresses the scene, runs the
    // render services in order, and hands what the game spawns to SpawnDressing
    public class RenderManager : MonoBehaviour
    {
        [SerializeField] CreatureLooks _creatureLooks;
        [SerializeField] SpellLooks _spellLooks;
        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] StageDressing _dressing;
        [SerializeField] EnvironmentRoot _environmentPrefab;
        [SerializeField] LookController _look;
        [SerializeField] ZoneRegistry _zones;
        [SerializeField] GrassField _grass;
        [SerializeField] SpellVisualSink _spellSink;
        [SerializeField] StoneEffects _stoneEffects;
        [SerializeField] BattleFocus _battleFocus;
        [SerializeField] StageRangeDriver _rangeDriver;
        [SerializeField] StageKeyLight _keyLight;
        [SerializeField] DeliveryVocabulary _deliveryVocabulary;

        RenderRegistry _registry = new RenderRegistry();
        Scene _scene;
        EnvironmentRoot _environment;
        SpawnDressing _spawns = new SpawnDressing();
        int _deliveryToken;
        bool _isLandscape = false;

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

        public Pose overviewPose { get { return _dressing.OverviewPose(_isLandscape); } }

        public RenderRegistry registry { get { return _registry; } }
        public ZoneRegistry zones { get { return _zones; } }
        public GrassField grass { get { return _grass; } }
        public LookController look { get { return _look; } }
        public SpellVisualSink spellSink { get { return _spellSink; } }
        public StoneEffects stoneEffects { get { return _stoneEffects; } }
        public CreatureLooks creatureLooks { get { return _creatureLooks; } }
        public SpellLooks spellLooks { get { return _spellLooks; } }
        public PrimitiveMeshes meshes { get { return _meshes; } }
        public Renderer boardGround { get { return _dressing.boardGround; } }
        public StageKeyLight keyLight { get { return _keyLight; } }
        public DeliveryVocabulary deliveryVocabulary { get { return _deliveryVocabulary; } }

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
            _gameCamera = _dressing.AdoptCamera(_scene);
            if (_gameCamera == null)
            {
                _entityManager = null;
                return;
            }

            _dressing.SwapPipeline();
            _dressing.SetLighting(_scene, _keyLight.keyLight);
            _board = _dressing.SetBoard(_scene, player.grid);
            _dressing.FrameBoard(_board);
            _dressing.Frame(_gameCamera, _isLandscape);

            // Init chain
            _look.Init(StageCalibration.BackgroundFog(_gameCamera.transform.position, _board));
            _zones.Init();
            Rect boardRect = BoardRect();
            _grass.Init(boardRect, player.grid.size, _board.max.y, _gameCamera, _zones.buffer, ZonePacker.MaxZones);
            InitEnvironment(boardRect);
            _spellSink.Init(this);
            _battleFocus.Init(this);
            _rangeDriver.Init(_gameCamera);
            _spawns.Init(this, _rangeDriver, _battleFocus);
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
            _environment.grass.UpdateStrips(_zones);
            _battleFocus.Tick();
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

            _dressing.Frame(_gameCamera, isLandscape);
            _look.Init(StageCalibration.BackgroundFog(_gameCamera.transform.position, _board));
            _foreground.Build();
            _environment.ridge.Build();
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
            if (_dressing != null)
            {
                _dressing.RestorePipeline();
            }
        }

        void InitEnvironment(Rect boardRect)
        {
            _environment = Instantiate(_environmentPrefab, transform);
            _gust = _environment.gust;
            _foreground = _environment.foreground;
            LookSettings lookSettings = _look.settings;
            _environment.Init(_meshes, _gameCamera, boardRect, _player.grid.size, _board.max.y, _zones,
                lookSettings.fogStart, lookSettings.fogEnd);
        }

        void Detach()
        {
            _spawns.Clear();
            if (_battleFocus != null)
            {
                _battleFocus.Clear();
            }

            if (_environment != null)
            {
                Destroy(_environment.gameObject);
            }

            if (_dressing != null)
            {
                _dressing.Clear();
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
            _environment = null;
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
