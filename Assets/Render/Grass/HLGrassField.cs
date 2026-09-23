using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{
    // One flat grass area seen by one camera. The zone owner publishes first, then the field updates.
    public class HLGrassField : MonoBehaviour
    {
        public static readonly int BladeSides = 5;
        public static readonly int MaxZones = 64;

        [SerializeField] HLPrimitiveMeshes _meshes;
        [SerializeField] ComputeShader _updateGrass;
        [SerializeField] Material _lookMaterial;
        [SerializeField] Material _ringMaterial;
        [SerializeField] int _bladeBudget = HLGrassLayout.DefaultBudget;
        [SerializeField] uint _seed = 1;
        [SerializeField] float _bladeHeightScale = 1f;
        // World-space surface top used when the ground has no Renderer, before root lift
        [SerializeField] float _surfaceY = 0.5f;

        // Stage scene wiring, removed in D2
        [SerializeField] GridManager _grid;
        [SerializeField] Transform _ground;
        [SerializeField] Camera _gameplayCamera;
        [SerializeField] Shader _ringShader;

        GraphicsBuffer _zones;
        GraphicsBuffer _seeds;
        GraphicsBuffer _states;
        GraphicsBuffer _visibleBlades;
        Plane[] _planes = new Plane[6];
        Vector4[] _planeVectors = new Vector4[6];
        Renderer _groundRenderer;
        int _kernel;
        HLGrassBuildKey _builtKey;
        Rect _area;
        float _cellSize;
        bool _isInitialized;

        // Runtime copies for the stage scene, removed in D2
        HLPrimitiveMeshes _stageMeshes;
        Material _stageBladeMaterial;
        Material _stageRingMaterial;

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

        public uint seed { get { return _seed; } set { _seed = value; } }

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

        public void Init(Rect area, float cellSize, float surfaceY, Camera camera, GraphicsBuffer zones, int zoneCapacity)
        {
            bool isBufferValid = zones != null && zones.IsValid() && zones.stride == HLZone.Stride;
            bool isCapacityValid = zoneCapacity >= 1 && zoneCapacity <= MaxZones;
            if (!isBufferValid || !isCapacityValid || zoneCapacity > zones.count)
            {
                Debug.LogError("[HLGrassField] Borrow a live zone buffer with the 32-byte stride and capacity 1..64.");
                return;
            }

            bool isAreaValid = float.IsFinite(area.width) && float.IsFinite(area.height) && area.width > 0f && area.height > 0f;
            if (!isAreaValid || !float.IsFinite(cellSize) || cellSize <= 0f || !float.IsFinite(surfaceY))
            {
                Debug.LogError($"[HLGrassField] Rejected area {area} with cell size {cellSize} and surface {surfaceY}.");
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
            _groundRenderer = _ground != null ? _ground.GetComponent<Renderer>() : null;
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        }

        void Update()
        {
            _wind.Advance(Time.deltaTime);
        }

        // The stage scene drives its fields here until the render manager attaches it, removed in D2
        void LateUpdate()
        {
            if (_isInitialized || _grid == null || _ground == null)
            {
                return;
            }

            if (_grid.width <= 0 || _grid.height <= 0 || _grid.cells == null)
            {
                return;
            }

            if (_grid.cells.LongLength != (long)_grid.width * _grid.height)
            {
                return;
            }

            Vector3 center = _grid.transform.position;
            float width = _grid.width * _grid.size;
            float height = _grid.height * _grid.size;
            float surfaceY = _groundRenderer != null ? _groundRenderer.bounds.max.y : _surfaceY;
            Rect area = new Rect(center.x - width * 0.5f, center.z - height * 0.5f, width, height);
            Dispatch(new HLGrassBuildKey(area, _grid.size, surfaceY, _seed, _bladeBudget));
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

        public void UpdateField(HLZoneRegistry zones)
        {
            if (zones == null)
            {
                Debug.LogError("[HLGrassField] UpdateField needs the zone registry.");
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
            Dispatch(new HLGrassBuildKey(_area, _cellSize, _surfaceY, _seed, _bladeBudget));
        }

        public void TriggerGust(Vector3 towardTarget)
        {
            _wind.TriggerGust(towardTarget);
        }

        // The zone owner publishes the same buffer and count globally for the ring draw
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

        void Dispatch(HLGrassBuildKey key)
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
            _updateGrass.SetFloat("_HL_Time", Time.time);
            _updateGrass.SetVector("_HL_Wind", _wind.current);
            _updateGrass.SetBuffer(_kernel, "_HL_Zones", _zones);
            _updateGrass.SetInt("_HL_ZoneCount", _zoneCount);
            _visibleBlades.SetCounterValue(0);
            _updateGrass.Dispatch(_kernel, (_bladeCount + 63) / 64, 1, 1);
            GraphicsBuffer.CopyCount(_visibleBlades, _bladeDraw.arguments, 4);
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
                Debug.LogError("[HLGrassField] Grass disabled: it needs compute, indirect draws, its assets and a finite area.", this);
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
            _kernel = _updateGrass.FindKernel("HLUpdateGrass");

            HLPrimitiveMeshes meshes = _meshes;
            Material bladeMaterial = _lookMaterial;
            Material ringMaterial = _ringMaterial;
            if (!_isInitialized)
            {
                CreateStageResources();
                meshes = _stageMeshes;
                bladeMaterial = _stageBladeMaterial;
                ringMaterial = _stageRingMaterial;
            }

            Bounds bounds = key.CalculateBounds();
            _bladeDraw = new HLGrassDraw(meshes.bladeCone, bladeMaterial, 0, bounds, gameObject.layer);
            _bladeDraw.properties.SetBuffer("_HL_BladeSeeds", _seeds);
            _bladeDraw.properties.SetBuffer("_HL_BladeStates", _states);
            _bladeDraw.properties.SetBuffer("_HL_VisibleBladeIDs", _visibleBlades);
            _bladeDraw.properties.SetFloat("_HL_BladeHeightScale", ClampHeightScale(_bladeHeightScale));
            HLGrassPalette.Apply(_bladeDraw.properties);

            _ringDraw = new HLGrassDraw(meshes.annulus, ringMaterial, (uint)MaxZones, bounds, gameObject.layer);
            _ringDraw.properties.SetFloat("_HL_SurfaceY", key.surfaceY);
            _ringDraw.properties.SetVector("_HL_FieldRect", key.FieldRect());
            return true;
        }

        bool CanBuild()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                return false;
            }

            if (!_isInitialized)
            {
                return _updateGrass != null && _lookMaterial != null && _ringShader != null;
            }

            return _updateGrass != null && _meshes != null && _lookMaterial != null && _ringMaterial != null;
        }

        // The stage scene references the look material and the ring shader, not the grass assets, removed in D2
        void CreateStageResources()
        {
            if (_stageMeshes == null)
            {
                _stageMeshes = StageSceneMeshes.Create();
                _stageBladeMaterial = HLGrassPalette.CreateBladeMaterial(_lookMaterial);
                _stageRingMaterial = new Material(_ringShader);
                _stageRingMaterial.name = "HLGrassRingRuntime";
                _stageRingMaterial.enableInstancing = true;
            }
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

            if (_ringDraw != null)
            {
                _ringDraw.Release();
                _ringDraw = null;
            }

            if (_stageMeshes != null)
            {
                StageSceneMeshes.Release(_stageMeshes);
                Destroy(_stageBladeMaterial);
                Destroy(_stageRingMaterial);
                _stageMeshes = null;
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
