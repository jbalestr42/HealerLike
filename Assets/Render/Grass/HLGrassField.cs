using System;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{
    /// <summary>One flat battlefield and one camera. Borrow the zone owner's snapshot before LateUpdate.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class HLGrassField : MonoBehaviour
    {
        [SerializeField] GridManager grid;
        [SerializeField] Transform ground;
        [SerializeField] Camera gameplayCamera;
        [SerializeField] ComputeShader updateGrass;
        [SerializeField] Shader grassShader;
        [SerializeField] Shader ringShader;
        [SerializeField, Range(0, HLGrassLayout.MaxBudget)] int bladeBudget = HLGrassLayout.DefaultBudget;
        [SerializeField] uint seed = 1;
        [SerializeField] Vector2 windDirection = new Vector2(1, 0.35f);
        [SerializeField, Min(0)] float windSpeed = 1.2f;
        [SerializeField, Range(0, 0.1f)] float windAmplitude = 0.065f;
        [Tooltip("Used when no ground Renderer is present. World-space surface top, before root lift.")]
        [SerializeField] float surfaceY = 0.5f;
        GraphicsBuffer zones;
        int zoneCount;
        GraphicsBuffer seeds, states, visibleGrass, visibleCones, grassArgs, coneArgs, ringArgs;
        Mesh bladeMesh, coneMesh, ringMesh;
        Material bladeMaterial, coneMaterial, ringMaterial;
        ComputeShader compute;
        RenderParams bladeParams, coneParams, ringParams;
        readonly Plane[] planes = new Plane[6];
        readonly Vector4[] planeVectors = new Vector4[6];
        Renderer groundRenderer;
        int kernel, count, builtWidth, builtHeight, builtBudget;
        uint builtSeed;
        float builtSize, builtSurface;
        Vector3 builtOrigin;
        bool ready;
        float gustRemaining;
        Vector2 gustDirection;
        /// <summary>Cosmetic 0.5-second response to a public projectile launch.</summary>
        public void TriggerGust(Vector3 towardTarget)
        {
            Vector2 direction = new Vector2(towardTarget.x, towardTarget.z);
            if (float.IsNaN(direction.sqrMagnitude) || float.IsInfinity(direction.sqrMagnitude) || direction.sqrMagnitude < 1e-8f) return;
            gustDirection = direction.normalized; gustRemaining = 0.5f;
        }
        public void AdvanceGust(float scaledSeconds)
        {
            if (scaledSeconds >= 0 && !float.IsInfinity(scaledSeconds)) gustRemaining = Mathf.Max(0, gustRemaining - scaledSeconds);
        }
        public Vector4 Wind
        {
            get
            {
                Vector2 direction = gustRemaining > 0 ? gustDirection : windDirection.sqrMagnitude > 1e-8f ? windDirection.normalized : Vector2.right;
                return new Vector4(direction.x, direction.y, Mathf.Max(0, windSpeed), Mathf.Clamp(windAmplitude, 0, 0.1f) * (gustRemaining > 0 ? 2 : 1));
            }
        }
        void Update() => AdvanceGust(Time.deltaTime);
        public int BladeCount => count;
        public int ActiveZoneCount => zoneCount;
        public bool IsReady => ready;
        public int BladeBudget { get => bladeBudget; set => bladeBudget = Mathf.Clamp(value, 0, HLGrassLayout.MaxBudget); }

        public void Initialize(GridManager assignedGrid, Transform assignedGround, Camera camera,
            GraphicsBuffer zoneBuffer, int zoneCapacity)
        {
            if (zoneBuffer == null || zoneCapacity < 1 || zoneCapacity > 64 || zoneCapacity > zoneBuffer.count || zoneBuffer.stride != HLZone.Stride)
                throw new ArgumentException("Borrow a live zone buffer with the frozen 32-byte stride and capacity 1..64.");
            Release();
            grid = assignedGrid; ground = assignedGround; gameplayCamera = camera;
            groundRenderer = ground != null ? ground.GetComponent<Renderer>() : null;
            SetZoneSnapshot(zoneBuffer, 0);
        }

        /// <summary>Stage bridge calls after the registry publishes, including count zero and owner replacement.
        /// The same buffer/count must be globally published by the zone owner for the ring draw.</summary>
        public void SetZoneSnapshot(GraphicsBuffer buffer, int validCount)
        {
            if (validCount < 0 || validCount > 64 || (buffer == null && validCount != 0) ||
                (buffer != null && (buffer.stride != HLZone.Stride || validCount > buffer.count)))
                throw new ArgumentOutOfRangeException(nameof(validCount));
            zones = buffer; zoneCount = validCount;
        }
        public void SetZoneCount(int validCount) => SetZoneSnapshot(zones, validCount);

        void OnEnable() { groundRenderer = ground != null ? ground.GetComponent<Renderer>() : null; }
        void LateUpdate()
        {
            if (grid == null || ground == null || gameplayCamera == null || zones == null || !zones.IsValid()) return;
            if (grid.width <= 0 || grid.height <= 0 || grid.cells == null || grid.cells.LongLength != (long)grid.width * grid.height) return;
            float top = groundRenderer != null ? groundRenderer.bounds.max.y : surfaceY;
            if (ready && (builtWidth != grid.width || builtHeight != grid.height || builtSize != grid.size ||
                builtOrigin != grid.transform.position || builtSurface != top || builtSeed != seed || builtBudget != bladeBudget)) ReleaseOwned();
            if (!ready && !Build(top)) return;
            if (count == 0) return;
            GeometryUtility.CalculateFrustumPlanes(gameplayCamera, planes);
            for (int i = 0; i < 6; i++) planeVectors[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
            compute.SetVectorArray("_HL_FrustumPlanes", planeVectors);
            compute.SetFloat("_HL_Time", Time.time);
            compute.SetVector("_HL_Wind", Wind);
            compute.SetBuffer(kernel, "_HL_Zones", zones);
            compute.SetInt("_HL_ZoneCount", zoneCount);
            visibleGrass.SetCounterValue(0); visibleCones.SetCounterValue(0);
            compute.Dispatch(kernel, (count + 63) / 64, 1, 1);
            GraphicsBuffer.CopyCount(visibleGrass, grassArgs, 4);
            GraphicsBuffer.CopyCount(visibleCones, coneArgs, 4);
            bladeParams.camera = coneParams.camera = ringParams.camera = gameplayCamera;
            Graphics.RenderMeshIndirect(in bladeParams, bladeMesh, grassArgs);
            Graphics.RenderMeshIndirect(in coneParams, coneMesh, coneArgs);
            if (zoneCount > 0) Graphics.RenderMeshIndirect(in ringParams, ringMesh, ringArgs);
        }

        bool Build(float top)
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                Debug.LogWarning("HLGrassField disabled: compute, instancing and indirect arguments are required.", this);
                enabled = false; return false;
            }
            if (updateGrass == null || grassShader == null || ringShader == null)
            {
                Debug.LogWarning("HLGrassField requires its compute, grass and ring shader assets; assign them in the stage.", this);
                enabled = false; return false;
            }
            try
            {
                var layout = HLGrassLayout.Generate(grid.width, grid.height, grid.size, grid.transform.position, top, bladeBudget, seed);
                count = layout.Length;
                builtWidth = grid.width; builtHeight = grid.height; builtSize = grid.size;
                builtOrigin = grid.transform.position; builtSurface = top; builtSeed = seed; builtBudget = bladeBudget;
                if (count == 0) { ready = true; return true; }
                Bounds bounds = HLGrassBounds.Calculate(grid.width, grid.height, grid.size, builtOrigin, top);
                seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, HLBladeSeed.Stride); seeds.SetData(layout);
                states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, HLBladeState.Stride);
                visibleGrass = new GraphicsBuffer(GraphicsBuffer.Target.Append, count, 4);
                visibleCones = new GraphicsBuffer(GraphicsBuffer.Target.Append, count, 4);
                bladeMesh = HLGrassBlade.CreateStrip(); coneMesh = HLGrassCone.CreateCone(); ringMesh = HLGrassRing.CreateAnnulus();
                grassArgs = CreateArguments(bladeMesh, 0); coneArgs = CreateArguments(coneMesh, 0); ringArgs = CreateArguments(ringMesh, 64);
                bladeMaterial = CreateGrassMaterial(false); coneMaterial = CreateGrassMaterial(true);
                ringMaterial = new Material(ringShader) { name = "HLGrassRingRuntime", enableInstancing = true };
                bladeParams = Parameters(bladeMaterial, bounds, visibleGrass);
                coneParams = Parameters(coneMaterial, bounds, visibleCones);
                ringParams = Parameters(ringMaterial, bounds, null);
                ringParams.matProps.SetFloat("_HL_SurfaceY", top);
                ringParams.matProps.SetVector("_HL_FieldRect", new Vector4(builtOrigin.x - grid.width * grid.size / 2,
                    builtOrigin.z - grid.height * grid.size / 2, builtOrigin.x + grid.width * grid.size / 2, builtOrigin.z + grid.height * grid.size / 2));
                compute = Instantiate(updateGrass); kernel = compute.FindKernel("HLUpdateGrass");
                compute.SetInt("_HL_BladeCount", count);
                compute.SetBuffer(kernel, "_HL_BladeSeeds", seeds); compute.SetBuffer(kernel, "_HL_BladeStates", states);
                compute.SetBuffer(kernel, "_HL_VisibleGrass", visibleGrass); compute.SetBuffer(kernel, "_HL_VisibleCones", visibleCones);
                ready = true; return true;
            }
            catch (Exception e) { ReleaseOwned(); Debug.LogException(e, this); enabled = false; return false; }
        }
        Material CreateGrassMaterial(bool cone)
        {
            var material = new Material(grassShader) { name = cone ? "HLGrassConeRuntime" : "HLGrassBladeRuntime", enableInstancing = true };
            material.SetFloat("_HL_Cone", cone ? 1 : 0); material.SetFloat("_HL_Cull", cone ? 2 : 0);
            material.SetVector("_HL_InstanceTint", Vector4.one);
            SetColor(material, "_HL_RootColor", 46, 125, 79); SetColor(material, "_HL_MidColor", 79, 168, 79);
            SetColor(material, "_HL_TipColor", 155, 210, 74); SetColor(material, "_HL_HealColor", 198, 242, 74);
            SetColor(material, "_HL_SlateRoot", 58, 66, 87); SetColor(material, "_HL_SlateTip", 74, 84, 104);
            return material;
        }
        static void SetColor(Material material, string property, byte r, byte g, byte b)
        {
            Color color = new Color32(r, g, b, 255);
            material.SetVector(property, QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
        }
        RenderParams Parameters(Material material, Bounds bounds, GraphicsBuffer visible)
        {
            var properties = new MaterialPropertyBlock();
            if (visible != null)
            {
                properties.SetBuffer("_HL_BladeSeeds", seeds); properties.SetBuffer("_HL_BladeStates", states);
                properties.SetBuffer("_HL_VisibleBladeIDs", visible);
            }
            return new RenderParams(material) { camera = gameplayCamera, worldBounds = bounds, matProps = properties,
                layer = gameObject.layer, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true,
                lightProbeUsage = LightProbeUsage.Off, reflectionProbeUsage = ReflectionProbeUsage.Off };
        }
        static GraphicsBuffer CreateArguments(Mesh mesh, uint instances)
        {
            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            buffer.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = mesh.GetIndexCount(0),
                instanceCount = instances, startIndex = mesh.GetIndexStart(0), baseVertexIndex = (uint)mesh.GetBaseVertex(0), startInstance = 0 } });
            return buffer;
        }
        void OnDisable() { gustRemaining = 0; ReleaseOwned(); }
        void OnDestroy() => Release();
        public void Release() { ReleaseOwned(); zones = null; zoneCount = 0; }
        void ReleaseOwned()
        {
            ready = false; count = 0;
            seeds?.Dispose(); seeds = null; states?.Dispose(); states = null;
            visibleGrass?.Dispose(); visibleGrass = null; visibleCones?.Dispose(); visibleCones = null;
            grassArgs?.Dispose(); grassArgs = null; coneArgs?.Dispose(); coneArgs = null; ringArgs?.Dispose(); ringArgs = null;
            DisposeObject(bladeMesh); bladeMesh = null; DisposeObject(coneMesh); coneMesh = null; DisposeObject(ringMesh); ringMesh = null;
            DisposeObject(bladeMaterial); bladeMaterial = null; DisposeObject(coneMaterial); coneMaterial = null;
            DisposeObject(ringMaterial); ringMaterial = null; DisposeObject(compute); compute = null;
        }
        static void DisposeObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
    }
}
