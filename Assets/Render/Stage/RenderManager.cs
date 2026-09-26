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
        StageInterface _interface;
        StageCreaturePlacement _placement;
        public StageCreaturePlacement placement { get { return _placement; } }
        readonly StageEnvironment _environment = new StageEnvironment();
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

        public EnvironmentGust gust { get { return _environment.gust; } }

        public EnvironmentForeground foreground { get { return _environment.foreground; } }

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
            _interface = gameObject.AddComponent<StageInterface>();
            _interface.Init(this, _battleFocus);
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
            _look.Init(StageCalibration.BackgroundFog(_gameCamera.transform.position, _board,
                _gameCamera.transform.eulerAngles.y));
            _zones.Init();
            Rect boardRect = BoardRect();
            _grass.Init(boardRect, player.grid.size, _board.max.y, _gameCamera, _zones.buffer, ZonePacker.MaxZones);
            _environment.Init(_environmentPrefab, this, boardRect);
            _spellSink.Init(this);
            _battleFocus.Init(this);
            _rangeDriver.Init(_gameCamera);
            _spawns.Init(this, _rangeDriver, _battleFocus);
            if (_placement == null)
            {
                _placement = gameObject.AddComponent<StageCreaturePlacement>();
            }
            _placement.Init(_creatureLooks, _meshes, StageSceneObjects.Find<InteractionManager>(_scene),
                _gameCamera, StageCalibration.CellSize);
            _keyLight.Init();
            Debug.Log($"[RenderManager] Attached to {_scene.name}");
        }

        void LateUpdate()
        {
            if (_entityManager == null)
            {
                return;
            }

            // Observers published and producers moved their zones in Update, so the frame is final here
            _spellSink.Tick();
            _zones.PublishFrame(Time.deltaTime);
            _grass.UpdateField(_zones);
            _environment.Tick(_zones);
            _battleFocus.Tick();
            if (_placement != null)
            {
                _placement.Tick();
            }
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

        // Asset tuning never detaches the game or reframes its camera. Existing views keep their owners and
        // subscriptions; SpawnDressing continues to dress later spawns with the same edited vocabulary.
        public int RebuildViews()
        {
            int rebuilt = _spawns.RebuildViews();
            if (_placement != null)
            {
                _placement.Refresh(_creatureLooks, _meshes);
            }
            if (_interface != null)
            {
                _interface.RefreshCreatureIcons();
            }
            return rebuilt;
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
            _look.Init(StageCalibration.BackgroundFog(_gameCamera.transform.position, _board,
                _gameCamera.transform.eulerAngles.y));
            FrameEnvironment(true);
            _battleFocus.MarkDirty();
        }

        public void FrameViewport(Rect viewport, float aspect)
        {
            if (_gameCamera == null || aspect <= 0f)
            {
                return;
            }

            _isLandscape = aspect > 1f;
            _dressing.FrameBoard(_board, aspect, viewport);
            _dressing.Frame(_gameCamera, _isLandscape, aspect);
            _battleFocus.SetViewport(viewport);
            FrameEnvironment(true);
        }

        // Refresh scenery only after the camera settles or its orientation changes, never every render frame.
        public void FrameEnvironment(bool force = false)
        {
            _environment.Frame(_gameCamera, _board, force);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _interface.Attach(scene);
            EntityManager entityManager = StageSceneObjects.Find<EntityManager>(scene);
            if (entityManager == null)
            {
                return;
            }

            // The game's unparented Instantiate calls land in the active scene and unload with it
            SceneManager.SetActiveScene(scene);
            Init(entityManager, StageSceneObjects.Find<PlayerBehaviour>(scene));
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

        void Detach()
        {
            _spawns.Clear();
            if (_placement != null)
            {
                _placement.Clear();
            }
            if (_battleFocus != null)
            {
                _battleFocus.Clear();
            }

            _environment.Clear();

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
        }

        Rect BoardRect()
        {
            return new Rect(_board.min.x, _board.min.z, _board.size.x, _board.size.z);
        }

    }
}
