using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{
    // One flat grass area seen by one camera. The zone owner publishes first, then the field updates.
    public class GrassField : MonoBehaviour
    {
        public static readonly int MaxZones = 64;
        public static readonly string InstancedKeyword = "HL_GRASS_INSTANCED";

        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] ComputeShader _updateGrass;
        [SerializeField] Material _lookMaterial;
        [SerializeField] Material _ringMaterial;
        [SerializeField] int _bladeBudget = GrassLayout.MaxBudget;
        [SerializeField] uint _seed = 1;
        [SerializeField] float _bladeHeightScale = 1f;
        // World-space surface top used when the ground has no Renderer, before root lift
        [SerializeField] float _surfaceY = 0.5f;

        GraphicsBuffer _zones;
        GraphicsBuffer _seeds;
        GraphicsBuffer _states;
        GraphicsBuffer _visibleBlades;
        Plane[] _planes = new Plane[6];
        Vector4[] _planeVectors = new Vector4[6];
        Camera _gameplayCamera;
        int _kernel;
        GrassBuildKey _builtKey;
        Rect _area;
        float _cellSize;
        bool _isInitialized;

        public Material lookMaterial { get { return _lookMaterial; } }

        GrassDraw _bladeDraw;
        public GrassDraw bladeDraw { get { return _bladeDraw; } }

        GrassDraw _socleDraw;
        public GrassDraw socleDraw { get { return _socleDraw; } }

        GrassDraw _ringDraw;
        public GrassDraw ringDraw { get { return _ringDraw; } }

        int _zoneCount;
        public int activeZoneCount { get { return _zoneCount; } }

        int _bladeCount;
        public int bladeCount { get { return _bladeCount; } }

        bool _isReady;
        public bool isReady { get { return _isReady; } }

        public int bladeBudget
        {
            get { return _bladeBudget; }
            set { _bladeBudget = Mathf.Clamp(value, 0, GrassLayout.MaxBudget); }
        }

        public uint seed { get { return _seed; } set { _seed = value; } }

        // Presentation-only tuft height; spike height, tuft width and density stay unchanged
        public float bladeHeightScale
        {
            get { return _bladeHeightScale; }
            set
            {
                _bladeHeightScale = ClampHeightScale(value);
                if (_bladeDraw != null)
                {
                    _bladeDraw.properties.SetFloat("_HL_BladeHeightScale", _bladeHeightScale);
                    _socleDraw.properties.SetFloat("_HL_BladeHeightScale", _bladeHeightScale);
                }
            }
        }

        public void Init(Rect area, float cellSize, float surfaceY, Camera camera, GraphicsBuffer zones,
                         int zoneCapacity)
        {
            bool isBufferValid = zones != null && zones.IsValid() && zones.stride == Zone.Stride;
            bool isCapacityValid = zoneCapacity >= 1 && zoneCapacity <= MaxZones;
            if (!isBufferValid || !isCapacityValid || zoneCapacity > zones.count)
            {
                Debug.LogError("[GrassField] Borrow a live zone buffer with the 32-byte stride and capacity 1..64.");
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

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            ReleaseOwned();
        }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            Release();
        }

        public void UpdateField(ZoneRegistry zones)
        {
            if (zones == null)
            {
                Debug.LogError("[GrassField] UpdateField needs the zone registry.");
                return;
            }

            UpdateField(zones.buffer, zones.count);
        }

        // Grass strips around the board borrow the zone buffer with a count of zero
        public void UpdateField(GraphicsBuffer zones, int count)
        {
            if (!_isInitialized)
            {
                return;
            }

            SetZoneSnapshot(zones, count);
            Dispatch(new GrassBuildKey(_area, _cellSize, _surfaceY, _seed, _bladeBudget));
        }

        // The zone owner publishes the same buffer and count globally for the ring draw
        public void SetZoneSnapshot(GraphicsBuffer buffer, int validCount)
        {
            bool isCountValid = validCount >= 0 && validCount <= MaxZones;
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

        public void SetZoneCount(int validCount)
        {
            SetZoneSnapshot(_zones, validCount);
        }

        public void Release()
        {
            ReleaseOwned();
            _zones = null;
            _zoneCount = 0;
        }

        void Dispatch(GrassBuildKey key)
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

            if (_bladeCount == 0)
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
            _updateGrass.SetInt("_HL_BladeCount", _bladeCount);
            _updateGrass.SetBuffer(_kernel, "_HL_BladeSeeds", _seeds);
            _updateGrass.SetBuffer(_kernel, "_HL_BladeStates", _states);
            _updateGrass.SetBuffer(_kernel, "_HL_VisibleBlades", _visibleBlades);
            _updateGrass.SetVectorArray("_HL_FrustumPlanes", _planeVectors);
            _updateGrass.SetBuffer(_kernel, "_HL_Zones", _zones);
            _updateGrass.SetInt("_HL_ZoneCount", _zoneCount);
            _visibleBlades.SetCounterValue(0);
            _updateGrass.Dispatch(_kernel, (_bladeCount + 63) / 64, 1, 1);
            GraphicsBuffer.CopyCount(_visibleBlades, _bladeDraw.arguments, 4);
            GraphicsBuffer.CopyCount(_visibleBlades, _socleDraw.arguments, 4);
        }

        void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _gameplayCamera || !isActiveAndEnabled || !_isReady || _bladeCount == 0)
            {
                return;
            }

            if (_zones == null || !_zones.IsValid())
            {
                return;
            }

            // Queued here rather than in the update so Editor repaints without a player-loop tick still draw
            _bladeDraw.Submit(camera);
            _socleDraw.Submit(camera);
            if (_zoneCount > 0)
            {
                _ringDraw.Submit(camera);
            }
        }

        bool Build(GrassBuildKey key)
        {
            BladeSeed[] layout = CanBuild() ? key.GenerateLayout() : null;
            if (layout == null)
            {
                Debug.LogError("[GrassField] Grass disabled: it needs compute, indirect draws, its assets "
                               + "and a finite area.", this);
                enabled = false;
                return false;
            }

            _builtKey = key;
            _bladeCount = layout.Length;
            _isReady = true;
            if (_bladeCount == 0)
            {
                return true;
            }

            _seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bladeCount, BladeSeed.Stride);
            _seeds.SetData(layout);
            _states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bladeCount, BladeState.Stride);
            _visibleBlades = new GraphicsBuffer(GraphicsBuffer.Target.Append, _bladeCount, 4);
            _kernel = _updateGrass.FindKernel("HLUpdateGrass");

            Bounds bounds = key.CalculateBounds();
            _bladeDraw = CreateTuftDraw(_meshes.tuft, bounds, 1f);
            _bladeDraw.shadowCastingMode = ShadowCastingMode.On;
            // The socle lies flat on the ground under every tuft, so it takes the yaw and scale but never the lean
            _socleDraw = CreateTuftDraw(_meshes.socle, bounds, 0f);

            _ringDraw = new GrassDraw(_meshes.annulus, _ringMaterial, (uint)MaxZones, bounds, gameObject.layer);
            _ringDraw.properties.SetFloat("_HL_SurfaceY", key.surfaceY);
            _ringDraw.properties.SetVector("_HL_FieldRect", key.FieldRect());
            return true;
        }

        GrassDraw CreateTuftDraw(Mesh mesh, Bounds bounds, float lean)
        {
            GrassDraw draw = new GrassDraw(mesh, _lookMaterial, 0, bounds, gameObject.layer);
            draw.properties.SetBuffer("_HL_BladeSeeds", _seeds);
            draw.properties.SetBuffer("_HL_BladeStates", _states);
            draw.properties.SetBuffer("_HL_VisibleBladeIDs", _visibleBlades);
            draw.properties.SetFloat("_HL_BladeHeightScale", ClampHeightScale(_bladeHeightScale));
            draw.properties.SetFloat("_HL_TuftLean", lean);
            return draw;
        }

        bool CanBuild()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing
                || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                return false;
            }

            return _updateGrass != null && _meshes != null && _lookMaterial != null && _ringMaterial != null;
        }

        static float ClampHeightScale(float value)
        {
            return float.IsFinite(value) ? Mathf.Clamp(value, 0.25f, 1f) : 1f;
        }

        void ReleaseOwned()
        {
            _isReady = false;
            _bladeCount = 0;
            _seeds = DisposeBuffer(_seeds);
            _states = DisposeBuffer(_states);
            _visibleBlades = DisposeBuffer(_visibleBlades);
            if (_bladeDraw != null)
            {
                _bladeDraw.Release();
                _bladeDraw = null;
            }

            if (_socleDraw != null)
            {
                _socleDraw.Release();
                _socleDraw = null;
            }

            if (_ringDraw != null)
            {
                _ringDraw.Release();
                _ringDraw = null;
            }
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
