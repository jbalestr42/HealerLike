using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{
    // One flat battlefield and one camera. Borrow the zone owner's snapshot before LateUpdate.
    [DefaultExecutionOrder(10000)]
    public class HLGrassField : MonoBehaviour
    {
        public static readonly int BladeSides = 5;
        public static readonly int MaxZones = 64;

        [SerializeField] GridManager _grid;
        [SerializeField] Transform _ground;
        [SerializeField] Camera _gameplayCamera;
        [SerializeField] ComputeShader _updateGrass;
        [SerializeField] Material _lookMaterial;
        [SerializeField] Shader _ringShader;
        [SerializeField] int _bladeBudget = HLGrassLayout.DefaultBudget;
        [SerializeField] uint _seed = 1;
        [SerializeField] float _bladeHeightScale = 1f;
        // World-space surface top used when the ground has no Renderer, before root lift
        [SerializeField] float _surfaceY = 0.5f;

        GraphicsBuffer _zones;
        GraphicsBuffer _seeds;
        GraphicsBuffer _states;
        GraphicsBuffer _visibleBlades;
        Mesh _ringMesh;
        ComputeShader _compute;
        Plane[] _planes = new Plane[6];
        Vector4[] _planeVectors = new Vector4[6];
        Renderer _groundRenderer;
        int _kernel;
        HLGrassBuildKey _builtKey;

        [SerializeField] HLGrassWind _wind = new HLGrassWind();
        public HLGrassWind wind { get { return _wind; } }

        HLGrassDraw _bladeDraw;
        public HLGrassDraw bladeDraw { get { return _bladeDraw; } }

        HLGrassDraw _ringDraw;
        public HLGrassDraw ringDraw { get { return _ringDraw; } }

        int _zoneCount;
        public int activeZoneCount { get { return _zoneCount; } }

        int _bladeCount;
        public int bladeCount { get { return _bladeCount; } }

        bool _isReady;
        public bool isReady { get { return _isReady; } }

        public int bladeBudget
        {
            get { return _bladeBudget; }
            set { _bladeBudget = Mathf.Clamp(value, 0, HLGrassLayout.MaxBudget); }
        }

        // Presentation-only grass height; spike height, blade width and density stay unchanged
        public float bladeHeightScale
        {
            get { return _bladeHeightScale; }
            set
            {
                _bladeHeightScale = ClampHeightScale(value);
                if (_bladeDraw != null)
                {
                    _bladeDraw.properties.SetFloat("_HL_BladeHeightScale", _bladeHeightScale);
                }
            }
        }

        public void Init(GridManager grid, Transform ground, Camera gameplayCamera, GraphicsBuffer zoneBuffer, int zoneCapacity)
        {
            bool isCapacityValid = zoneCapacity >= 1 && zoneCapacity <= MaxZones;
            if (zoneBuffer == null || !isCapacityValid || zoneCapacity > zoneBuffer.count || zoneBuffer.stride != HLZone.Stride)
            {
                Debug.LogError("[HLGrassField] Borrow a live zone buffer with the 32-byte stride and capacity 1..64.");
                return;
            }

            Release();
            _grid = grid;
            _ground = ground;
            _gameplayCamera = gameplayCamera;
            _groundRenderer = ground != null ? ground.GetComponent<Renderer>() : null;
            SetZoneSnapshot(zoneBuffer, 0);
        }

        void OnEnable()
        {
            _groundRenderer = _ground != null ? _ground.GetComponent<Renderer>() : null;
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        }

        void Update()
        {
            _wind.Advance(Time.deltaTime);
        }

        void LateUpdate()
        {
            if (!CanUpdate())
            {
                return;
            }

            HLGrassBuildKey key = new HLGrassBuildKey(_grid, SurfaceTop(), _seed, _bladeBudget);
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
            _compute.SetVectorArray("_HL_FrustumPlanes", _planeVectors);
            _compute.SetFloat("_HL_Time", Time.time);
            _compute.SetVector("_HL_Wind", _wind.current);
            _compute.SetBuffer(_kernel, "_HL_Zones", _zones);
            _compute.SetInt("_HL_ZoneCount", _zoneCount);
            _visibleBlades.SetCounterValue(0);
            _compute.Dispatch(_kernel, (_bladeCount + 63) / 64, 1, 1);
            GraphicsBuffer.CopyCount(_visibleBlades, _bladeDraw.arguments, 4);
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            _wind.ClearGust();
            ReleaseOwned();
        }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            Release();
        }

        public void TriggerGust(Vector3 towardTarget)
        {
            _wind.TriggerGust(towardTarget);
        }

        // The stage bridge calls this after the registry publishes, including count zero and owner replacement.
        // The zone owner publishes the same buffer and count globally for the ring draw.
        public void SetZoneSnapshot(GraphicsBuffer buffer, int validCount)
        {
            bool isCountValid = validCount >= 0 && validCount <= MaxZones;
            bool isBufferValid = buffer == null ? validCount == 0 : buffer.stride == HLZone.Stride && validCount <= buffer.count;
            if (!isCountValid || !isBufferValid)
            {
                Debug.LogError($"[HLGrassField] Rejected zone snapshot with count {validCount}.");
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

        bool CanUpdate()
        {
            if (_grid == null || _ground == null || _gameplayCamera == null || _zones == null || !_zones.IsValid())
            {
                return false;
            }
            if (_grid.width <= 0 || _grid.height <= 0 || _grid.cells == null)
            {
                return false;
            }
            return _grid.cells.LongLength == (long)_grid.width * _grid.height;
        }

        float SurfaceTop()
        {
            return _groundRenderer != null ? _groundRenderer.bounds.max.y : _surfaceY;
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

            // Queued here rather than in LateUpdate so Editor repaints without a player-loop tick still draw
            _bladeDraw.Submit(camera);
            if (_zoneCount > 0)
            {
                _ringDraw.Submit(camera);
            }
        }

        bool Build(HLGrassBuildKey key)
        {
            HLBladeSeed[] layout = CanBuild() ? key.GenerateLayout() : null;
            if (layout == null)
            {
                Debug.LogError("[HLGrassField] Grass disabled: it needs compute, indirect draws, its assets and a finite grid.", this);
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

            _seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bladeCount, HLBladeSeed.Stride);
            _seeds.SetData(layout);
            _states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bladeCount, HLBladeState.Stride);
            _visibleBlades = new GraphicsBuffer(GraphicsBuffer.Target.Append, _bladeCount, 4);

            Bounds bounds = key.CalculateBounds();
            HLPrimitiveMeshes.Retain();
            Mesh bladeMesh = HLPrimitiveMeshes.Get(HLPrimitive.Cone, BladeSides, 2);
            _bladeDraw = new HLGrassDraw(bladeMesh, HLGrassPalette.CreateBladeMaterial(_lookMaterial), 0, bounds, gameObject.layer);
            _bladeDraw.properties.SetBuffer("_HL_BladeSeeds", _seeds);
            _bladeDraw.properties.SetBuffer("_HL_BladeStates", _states);
            _bladeDraw.properties.SetBuffer("_HL_VisibleBladeIDs", _visibleBlades);
            _bladeDraw.properties.SetFloat("_HL_BladeHeightScale", ClampHeightScale(_bladeHeightScale));
            HLGrassPalette.Apply(_bladeDraw.properties);

            _ringMesh = HLGrassRing.CreateAnnulus();
            Material ringMaterial = new Material(_ringShader);
            ringMaterial.name = "HLGrassRingRuntime";
            ringMaterial.enableInstancing = true;
            _ringDraw = new HLGrassDraw(_ringMesh, ringMaterial, (uint)MaxZones, bounds, gameObject.layer);
            _ringDraw.properties.SetFloat("_HL_SurfaceY", key.surfaceY);
            _ringDraw.properties.SetVector("_HL_FieldRect", key.FieldRect());

            _compute = Instantiate(_updateGrass);
            _kernel = _compute.FindKernel("HLUpdateGrass");
            _compute.SetInt("_HL_BladeCount", _bladeCount);
            _compute.SetBuffer(_kernel, "_HL_BladeSeeds", _seeds);
            _compute.SetBuffer(_kernel, "_HL_BladeStates", _states);
            _compute.SetBuffer(_kernel, "_HL_VisibleBlades", _visibleBlades);
            return true;
        }

        bool CanBuild()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                return false;
            }
            return _updateGrass != null && _lookMaterial != null && _ringShader != null;
        }

        static float ClampHeightScale(float value)
        {
            return HLGrassLayout.Finite(value) ? Mathf.Clamp(value, 0.25f, 1f) : 1f;
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
                HLPrimitiveMeshes.Release();
            }
            if (_ringDraw != null)
            {
                _ringDraw.Release();
                _ringDraw = null;
            }
            HLPrimitiveMeshes.DestroyOwned(_ringMesh);
            HLPrimitiveMeshes.DestroyOwned(_compute);
            _ringMesh = null;
            _compute = null;
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
