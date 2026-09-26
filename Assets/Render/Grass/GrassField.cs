using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{
    // One flat grass area seen by one camera. The zone owner publishes first, then the field updates. A field
    // given the ground shader also owns the ground simulation over its area and a margin around it, which it
    // steps from what the Ground was told before its own tufts read it; every field samples whatever ground is
    // published.
    public class GrassField : MonoBehaviour
    {
        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] ComputeShader _updateGrass;
        [SerializeField] Material _lookMaterial;
        [SerializeField] Material _ringMaterial;
        [SerializeField] int _bladeBudget = GrassLayout.MaxBudget;
        [SerializeField] uint _seed = 1;
        [SerializeField] float _bladeHeightScale = 1f;
        [SerializeField, Range(0f, 1f)] float _windStrength = 1f;
        // Height segments of each tuft, so it can bend; one is the rigid pyramid
        [SerializeField, Range(1, 8)] int _bladeSegments = 4;
        // Left empty on fields that only read the ground another field owns
        [SerializeField] Shader _groundShader;
        // In cells: how far the ground reaches past the field, and the side of one ground texel
        [SerializeField] float _groundMargin = 3f;
        [SerializeField] float _groundTexel = 0.125f;
        [SerializeField] GroundSpringSettings _groundSpring = GroundSpringSettings.Default;

        GraphicsBuffer _zones;
        GrassTuftBatch _tufts;
        Camera _gameplayCamera;
        Rect _area;
        float _cellSize;
        float _surfaceY;
        bool _isInitialized;
        GroundStamp[] _stamps = new GroundStamp[GroundSimulation.StampCapacity];
        BodyCapsule[] _capsules = new BodyCapsule[GroundSimulation.StampCapacity];
        bool _isGroundFailed;

        GroundSimulation _simulation;
        // The GPU ground this field owns, when it was given the ground shader
        public GroundSimulation simulation { get { return _simulation; } }

        public Material lookMaterial { get { return _lookMaterial; } }

        public GrassDraw tuftDraw { get { return _tufts != null ? _tufts.tufts : null; } }
        public GrassDraw socleDraw { get { return _tufts != null ? _tufts.socles : null; } }

        int _zoneCount;
        public int activeZoneCount { get { return _zoneCount; } }
        public int tuftCount { get { return _tufts != null ? _tufts.count : 0; } }
        public bool isReady { get { return _tufts != null; } }

        public int tuftBudget
        {
            get { return _bladeBudget; }
            set { _bladeBudget = Mathf.Clamp(value, 0, GrassLayout.MaxBudget); }
        }

        public uint seed { get { return _seed; } set { _seed = value; } }

        public int bladeSegments
        {
            get { return _bladeSegments; }
            set { _bladeSegments = Mathf.Clamp(value, 1, GrassBladeMesh.MaxSegments); }
        }

        public float windStrength
        {
            get { return _windStrength; }
            set { _windStrength = Mathf.Clamp01(RenderMath.FiniteOr(value, 0f)); }
        }

        // Presentation-only tuft height; spike height, tuft width and density stay unchanged
        public float bladeHeightScale
        {
            get { return _bladeHeightScale; }
            set
            {
                _bladeHeightScale = ClampHeightScale(value);
                _tufts?.SetHeight(_bladeHeightScale);
            }
        }

        public void Init(Rect area, float cellSize, float surfaceY, Camera camera, GraphicsBuffer zones,
                         int zoneCapacity)
        {
            bool isBufferValid = zones != null && zones.IsValid() && zones.stride == Zone.Stride;
            bool isCapacityValid = zoneCapacity >= 1 && zoneCapacity <= ZonePacker.MaxZones;
            if (!isBufferValid || !isCapacityValid || zoneCapacity > zones.count)
            {
                Debug.LogError("[GrassField] Borrow a live zone buffer with the 32-byte stride and capacity "
                               + $"1..{ZonePacker.MaxZones}.");
                return;
            }

            bool isAreaValid = float.IsFinite(area.x) && float.IsFinite(area.y)
                               && float.IsFinite(area.width) && float.IsFinite(area.height)
                               && area.width > 0f && area.height > 0f;
            if (!isAreaValid || !float.IsFinite(cellSize) || cellSize <= 0f || !float.IsFinite(surfaceY))
            {
                Debug.LogError($"[GrassField] Rejected area {area} with cell size {cellSize} and surface {surfaceY}.");
                return;
            }

            Release();
            _area = area;
            _cellSize = cellSize;
            _surfaceY = surfaceY;
            _gameplayCamera = camera;
            _isInitialized = true;
            _isGroundFailed = false;
            SetZoneSnapshot(zones, 0);
        }

        void OnDisable()
        {
            _isGroundFailed = false;
            ReleaseOwned();
        }

        void OnDestroy()
        {
            Release();
        }

        public void UpdateField(ZoneRegistry zones, Ground ground)
        {
            UpdateField(zones, ground, Time.deltaTime, Time.time);
        }

        // The frame's length and clock given explicitly, so a capture can step the grass at a fixed rate. The zones
        // feed the tufts' own reading; everything the ground was told this frame moves the grass.
        public void UpdateField(ZoneRegistry zones, Ground ground, float deltaTime, float time)
        {
            if (zones == null || ground == null)
            {
                Debug.LogError("[GrassField] UpdateField needs the zone registry and the ground.");
                return;
            }

            if (!_isInitialized || !isActiveAndEnabled)
            {
                return;
            }

            SetZoneSnapshot(zones.buffer, zones.count);
            if (_simulation == null && _groundShader != null && !_isGroundFailed)
            {
                CreateGround();
            }

            if (_simulation != null)
            {
                int count = ground.Collect(_stamps, 0, zones.snapshot, _capsules, _surfaceY, _cellSize);
                _simulation.SetStamps(new System.ReadOnlySpan<GroundStamp>(_stamps, 0, count));
                _simulation.Step(deltaTime, GroundWind.Shader(_windStrength, time));
                _simulation.Publish();
            }

            Dispatch(new GrassBuildKey(_area, _cellSize, _surfaceY, _seed, _bladeBudget), time);
        }

        void CreateGround()
        {
            float margin = Mathf.Max(0f, RenderMath.FiniteOr(_groundMargin, 0f)) * _cellSize;
            Rect area = new Rect(_area.xMin - margin, _area.yMin - margin, _area.width + 2f * margin,
                                 _area.height + 2f * margin);
            float texel = Mathf.Max(0.01f, RenderMath.FiniteOr(_groundTexel, 0.125f)) * _cellSize;
            GroundVolume volume = GroundVolume.Create(area, texel);
            _simulation = new GroundSimulation(_groundShader, volume, _groundSpring);
            if (!_simulation.isValid)
            {
                _simulation.Dispose();
                _simulation = null;
                // Without it the tufts keep the plain wind; no retry every frame
                _isGroundFailed = true;
            }
        }

        // Grass strips around the board borrow the zone buffer with a count of zero
        public void UpdateField(GraphicsBuffer zones, int count)
        {
            UpdateField(zones, count, Time.time);
        }

        // The same on the clock the board's ground runs on, so both winds agree where they blend
        public void UpdateField(GraphicsBuffer zones, int count, float time)
        {
            if (!_isInitialized || !isActiveAndEnabled)
            {
                return;
            }

            SetZoneSnapshot(zones, count);
            Dispatch(new GrassBuildKey(_area, _cellSize, _surfaceY, _seed, _bladeBudget), time);
        }

        // The zone owner publishes the same buffer and count globally for the ring draw
        public void SetZoneSnapshot(GraphicsBuffer buffer, int validCount)
        {
            bool isCountValid = validCount >= 0 && validCount <= ZonePacker.MaxZones;
            bool isBufferValid = validCount == 0;
            if (buffer != null)
            {
                isBufferValid = buffer.IsValid() && buffer.stride == Zone.Stride && validCount <= buffer.count;
            }

            if (!isCountValid || !isBufferValid)
            {
                Debug.LogError($"[GrassField] Rejected zone snapshot with count {validCount}.");
                return;
            }

            _zones = buffer;
            _zoneCount = validCount;
        }

        public void Release()
        {
            _isInitialized = false;
            _gameplayCamera = null;
            ReleaseOwned();
            _zones = null;
            _zoneCount = 0;
        }

        void Dispatch(GrassBuildKey key, float time)
        {
            if (_gameplayCamera == null || _zones == null || !_zones.IsValid())
            {
                return;
            }

            if (_tufts != null && !_tufts.Matches(key, _bladeSegments))
            {
                ReleaseTufts();
            }

            if (_tufts == null)
            {
                if (!GrassTuftBatch.TryCreate(key, _bladeSegments, _meshes, _updateGrass, _lookMaterial,
                    _ringMaterial, gameObject.layer, out GrassTuftBatch created, out string error))
                {
                    ReleaseOwned();
                    enabled = false;
                    Debug.LogError("[GrassField] " + error, this);
                    return;
                }

                _tufts = created;
                _tufts.SetHeight(ClampHeightScale(_bladeHeightScale));
                _tufts.Show(_gameplayCamera, IsDrawn, IsRingDrawn, this);
            }

            _tufts.Dispatch(_gameplayCamera, _zones, _zoneCount, GroundWind.Shader(_windStrength, time));
        }

        bool IsDrawn()
        {
            return isActiveAndEnabled && isReady && tuftCount > 0 && _zones != null && _zones.IsValid();
        }

        bool IsRingDrawn()
        {
            return IsDrawn() && _zoneCount > 0;
        }

        static float ClampHeightScale(float value)
        {
            return float.IsFinite(value) ? Mathf.Clamp(value, 0.25f, 1f) : 1f;
        }

        void ReleaseOwned()
        {
            if (_simulation != null)
            {
                _simulation.Dispose();
                _simulation = null;
            }

            ReleaseTufts();
        }

        void ReleaseTufts()
        {
            _tufts?.Dispose();
            _tufts = null;
        }
    }
}
