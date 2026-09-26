using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{
    // One flat grass area seen by one camera. The zone owner publishes first, then the field updates. A field
    // given the ground shader also owns the ground motion over its area and a margin around it, which it steps
    // from the zones before its own tufts read it; every field samples whatever ground is published.
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
        GraphicsBuffer _seeds;
        GraphicsBuffer _states;
        GraphicsBuffer _visibleTufts;
        Plane[] _planes = new Plane[6];
        Vector4[] _planeVectors = new Vector4[6];
        Camera _gameplayCamera;
        int _kernel;
        GrassBuildKey _builtKey;
        Rect _area;
        float _cellSize;
        float _surfaceY;
        float _cullMargin;
        bool _isInitialized;
        GrassDraw _ringDraw;
        GroundStamp[] _stamps = new GroundStamp[GroundMotion.StampCapacity];
        BodyCapsule[] _capsules = new BodyCapsule[GroundMotion.StampCapacity];
        Vector2 _gust;
        bool _isGroundFailed;

        GroundMotion _ground;
        public GroundMotion ground { get { return _ground; } }

        public Material lookMaterial { get { return _lookMaterial; } }

        GrassDraw _tuftDraw;
        public GrassDraw tuftDraw { get { return _tuftDraw; } }

        GrassDraw _socleDraw;
        public GrassDraw socleDraw { get { return _socleDraw; } }

        int _zoneCount;
        public int activeZoneCount { get { return _zoneCount; } }

        int _tuftCount;
        public int tuftCount { get { return _tuftCount; } }

        bool _isReady;
        public bool isReady { get { return _isReady; } }

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

        // The cosmetic gust pulse this frame, EnvironmentGust.Sample, in world space
        public Vector3 gust { set { _gust = GroundWind.Gust(value); } }

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
                if (_tuftDraw != null)
                {
                    _tuftDraw.properties.SetFloat("_HLBladeHeightScale", _bladeHeightScale);
                    _socleDraw.properties.SetFloat("_HLBladeHeightScale", _bladeHeightScale);
                }
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

            bool isAreaValid = float.IsFinite(area.width) && float.IsFinite(area.height)
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
            SetZoneSnapshot(zones, 0);
        }

        void OnDisable()
        {
            ReleaseOwned();
        }

        void OnDestroy()
        {
            Release();
        }

        public void UpdateField(ZoneRegistry zones)
        {
            UpdateField(zones, Time.deltaTime, Time.time);
        }

        // The frame's length and clock given explicitly, so a capture can step the grass at a fixed rate
        public void UpdateField(ZoneRegistry zones, float deltaTime, float time)
        {
            if (zones == null)
            {
                Debug.LogError("[GrassField] UpdateField needs the zone registry.");
                return;
            }

            if (!_isInitialized)
            {
                return;
            }

            SetZoneSnapshot(zones.buffer, zones.count);
            if (_ground == null && _groundShader != null && !_isGroundFailed)
            {
                CreateGround();
            }

            if (_ground != null)
            {
                int count = ZoneStamps.Append(zones.snapshot, _stamps, 0);
                count += BodyStamps.Append(_capsules, zones.GatherBodies(_capsules), _surfaceY, _cellSize, _stamps,
                                           count);
                _ground.SetStamps(new System.ReadOnlySpan<GroundStamp>(_stamps, 0, count));
                _ground.Step(deltaTime, GroundWind.Shader(_windStrength, time), _gust);
                _ground.Publish(_gust);
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
            _ground = new GroundMotion(_groundShader, volume, _groundSpring);
            if (!_ground.isValid)
            {
                _ground.Dispose();
                _ground = null;
                // Without it the tufts keep the plain wind; no retry every frame
                _isGroundFailed = true;
            }
        }

        // Grass strips around the board borrow the zone buffer with a count of zero
        public void UpdateField(GraphicsBuffer zones, int count)
        {
            if (!_isInitialized)
            {
                return;
            }

            SetZoneSnapshot(zones, count);
            Dispatch(new GrassBuildKey(_area, _cellSize, _surfaceY, _seed, _bladeBudget), Time.time);
        }

        // The zone owner publishes the same buffer and count globally for the ring draw
        public void SetZoneSnapshot(GraphicsBuffer buffer, int validCount)
        {
            bool isCountValid = validCount >= 0 && validCount <= ZonePacker.MaxZones;
            bool isBufferValid = validCount == 0;
            if (buffer != null)
            {
                isBufferValid = buffer.stride == Zone.Stride && validCount <= buffer.count;
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

            if (_isReady && !key.Matches(_builtKey))
            {
                ReleaseOwned();
            }

            if (!_isReady && !Build(key))
            {
                return;
            }

            if (_tuftCount == 0)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(_gameplayCamera, _planes);
            for (int i = 0; i < _planes.Length; i++)
            {
                Vector3 normal = _planes[i].normal;
                _planeVectors[i] = new Vector4(normal.x, normal.y, normal.z, _planes[i].distance);
            }

            // Every field shares the compute asset, so each dispatch binds all of its own state first
            _updateGrass.SetInt("_HLBladeCount", _tuftCount);
            _updateGrass.SetBuffer(_kernel, "_HLBladeSeeds", _seeds);
            _updateGrass.SetBuffer(_kernel, "_HLBladeStates", _states);
            _updateGrass.SetBuffer(_kernel, "_HLVisibleBlades", _visibleTufts);
            _updateGrass.SetVectorArray("_HLFrustumPlanes", _planeVectors);
            _updateGrass.SetFloat("_HLCullMargin", _cullMargin);
            _updateGrass.SetVector("_HLGroundWind", GroundWind.Shader(_windStrength, time));
            BindGround();
            _updateGrass.SetBuffer(_kernel, "_HLZones", _zones);
            _updateGrass.SetInt("_HLZoneCount", _zoneCount);
            _visibleTufts.SetCounterValue(0);
            _updateGrass.Dispatch(_kernel, (_tuftCount + 63) / 64, 1, 1);
            GraphicsBuffer.CopyCount(_visibleTufts, _tuftDraw.arguments, 4);
            GraphicsBuffer.CopyCount(_visibleTufts, _socleDraw.arguments, 4);
        }

        // The ground published this frame, or none: the tufts then take the plain wind with this field's gust
        void BindGround()
        {
            bool isActive = Shader.GetGlobalFloat(GroundMotion.ActiveId) > 0.5f;
            Texture motion = isActive ? Shader.GetGlobalTexture(GroundMotion.MotionId) : null;
            Texture crush = isActive ? Shader.GetGlobalTexture(GroundMotion.CrushId) : null;
            isActive = motion != null && crush != null;
            _updateGrass.SetTexture(_kernel, GroundMotion.MotionId, isActive ? motion : Texture2D.blackTexture);
            _updateGrass.SetTexture(_kernel, GroundMotion.CrushId, isActive ? crush : Texture2D.blackTexture);
            _updateGrass.SetVector(GroundMotion.RectId, isActive ? Shader.GetGlobalVector(GroundMotion.RectId)
                                                                  : new Vector4(0f, 0f, 1f, 1f));
            _updateGrass.SetFloat(GroundMotion.ActiveId, isActive ? 1f : 0f);
            _updateGrass.SetVector(GroundMotion.GustId, isActive ? Shader.GetGlobalVector(GroundMotion.GustId)
                                                                  : (Vector4)_gust);
        }

        bool IsDrawn()
        {
            return isActiveAndEnabled && _isReady && _tuftCount > 0 && _zones != null && _zones.IsValid();
        }

        bool Build(GrassBuildKey key)
        {
            TuftSeed[] layout = CanBuild() ? key.GenerateLayout() : null;
            if (layout == null)
            {
                Debug.LogError("[GrassField] Grass disabled: it needs compute, indirect draws, its assets "
                               + "and a finite area.", this);
                enabled = false;
                return false;
            }

            _builtKey = key;
            _tuftCount = layout.Length;
            _isReady = true;
            if (_tuftCount == 0)
            {
                return true;
            }

            _seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _tuftCount, TuftSeed.Stride);
            _seeds.SetData(layout);
            _states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _tuftCount, TuftState.Stride);
            _visibleTufts = new GraphicsBuffer(GraphicsBuffer.Target.Append, _tuftCount, 4);
            _kernel = _updateGrass.FindKernel("HLUpdateGrass");
            _cullMargin = GrassBounds.Envelope(key.cellSize);

            Bounds bounds = key.CalculateBounds();
            int layer = gameObject.layer;
            _tuftDraw = GrassDraw.Tufts(GrassBladeMesh.Shared(_bladeSegments), _lookMaterial, bounds, layer, 1f);
            // Both ordinary blades and hostile spikes carry the graphic field shadows.
            _tuftDraw.shadowCastingMode = ShadowCastingMode.On;
            _tuftDraw.properties.SetFloat("_HLGrassSpikeShadowsOnly", 0f);
            // The socle lies flat on the ground under every tuft, so it takes the yaw and scale but never the lean
            _socleDraw = GrassDraw.Tufts(_meshes.socle, _lookMaterial, bounds, layer, 0f);
            _tuftDraw.BindTufts(_seeds, _states, _visibleTufts, ClampHeightScale(_bladeHeightScale));
            _socleDraw.BindTufts(_seeds, _states, _visibleTufts, ClampHeightScale(_bladeHeightScale));
            _ringDraw = GrassDraw.Rings(_meshes.annulus, _ringMaterial, bounds, layer, key);
            _tuftDraw.Show(_gameplayCamera, IsDrawn);
            _socleDraw.Show(_gameplayCamera, IsDrawn);
            _ringDraw.Show(_gameplayCamera, () => IsDrawn() && _zoneCount > 0);
            return true;
        }

        bool CanBuild()
        {
            bool isDeviceReady = SystemInfo.supportsComputeShaders && SystemInfo.supportsInstancing
                                 && SystemInfo.supportsIndirectArgumentsBuffer;
            return isDeviceReady && _updateGrass != null && _meshes != null && _lookMaterial != null
                   && _ringMaterial != null;
        }

        static float ClampHeightScale(float value)
        {
            return float.IsFinite(value) ? Mathf.Clamp(value, 0.25f, 1f) : 1f;
        }

        void ReleaseOwned()
        {
            if (_ground != null)
            {
                _ground.Dispose();
                _ground = null;
                GroundMotion.Unpublish();
            }

            _isReady = false;
            _tuftCount = 0;
            _seeds = DisposeBuffer(_seeds);
            _states = DisposeBuffer(_states);
            _visibleTufts = DisposeBuffer(_visibleTufts);
            _tuftDraw = ReleaseDraw(_tuftDraw);
            _socleDraw = ReleaseDraw(_socleDraw);
            _ringDraw = ReleaseDraw(_ringDraw);
        }

        static GrassDraw ReleaseDraw(GrassDraw draw)
        {
            if (draw != null)
            {
                draw.Release();
            }

            return null;
        }

        static GraphicsBuffer DisposeBuffer(GraphicsBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Dispose();
            }

            return null;
        }
    }
}
