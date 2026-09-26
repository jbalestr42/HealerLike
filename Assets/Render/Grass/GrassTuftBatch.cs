using System;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // A complete tuft generation: owned GPU buffers and draws, borrowing authored meshes and materials.
    public class GrassTuftBatch : IDisposable
    {
        readonly Plane[] _planes = new Plane[6];
        readonly Vector4[] _planeVectors = new Vector4[6];
        GrassBuildKey _key;
        ComputeShader _compute;
        int _kernel;
        int _segments;
        public GrassDraw rings { get; private set; }
        public GraphicsBuffer seeds { get; private set; }
        public GraphicsBuffer states { get; private set; }
        public GraphicsBuffer visible { get; private set; }
        public GrassDraw tufts { get; private set; }
        public GrassDraw socles { get; private set; }
        public int count { get; private set; }

        public static bool TryCreate(GrassBuildKey key, int segments, PrimitiveMeshes meshes, ComputeShader compute,
            Material material, Material ringMaterial, int layer, out GrassTuftBatch batch, out string error,
            Func<GraphicsBuffer.Target, int, int, GraphicsBuffer> allocate = null)
        {
            batch = null;
            error = "Grass needs compute, indirect draws, valid meshes/materials and a finite area.";
            bool device = SystemInfo.supportsComputeShaders && SystemInfo.supportsInstancing
                && SystemInfo.supportsIndirectArgumentsBuffer;
            if (!device || compute == null || !compute.HasKernel("HLUpdateGrass") || meshes == null
                || meshes.socle == null || meshes.annulus == null || material == null || ringMaterial == null)
            {
                return false;
            }

            TuftSeed[] layout = key.GenerateLayout();
            if (layout == null)
            {
                return false;
            }

            GrassTuftBatch candidate = new GrassTuftBatch();
            try
            {
                candidate._key = key;
                candidate._segments = segments;
                candidate._compute = compute;
                candidate._kernel = compute.FindKernel("HLUpdateGrass");
                candidate.count = layout.Length;
                if (layout.Length > 0)
                {
                    candidate.Allocate(layout, meshes, material, ringMaterial, layer, allocate ?? AllocateBuffer);
                }
                batch = candidate;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                candidate.Dispose();
                error = "Could not allocate grass: " + exception.Message;
                return false;
            }
        }

        static GraphicsBuffer AllocateBuffer(GraphicsBuffer.Target target, int count, int stride)
        {
            return new GraphicsBuffer(target, count, stride);
        }

        void Allocate(TuftSeed[] layout, PrimitiveMeshes meshes, Material material, Material ringMaterial,
            int layer, Func<GraphicsBuffer.Target, int, int, GraphicsBuffer> allocate)
        {
            seeds = allocate(GraphicsBuffer.Target.Structured, count, TuftSeed.Stride);
            seeds.SetData(layout);
            states = allocate(GraphicsBuffer.Target.Structured, count, TuftState.Stride);
            visible = allocate(GraphicsBuffer.Target.Append, count, 4);
            Bounds bounds = _key.CalculateBounds();
            tufts = GrassDraw.Tufts(GrassBladeMesh.Shared(_segments), material, bounds, layer, 1f);
            tufts.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            tufts.properties.SetFloat("_HLGrassSpikeShadowsOnly", 0f);
            socles = GrassDraw.Tufts(meshes.socle, material, bounds, layer, 0f);
            rings = GrassDraw.Rings(meshes.annulus, ringMaterial, bounds, layer, _key);
            SetHeight(1f);
        }

        public bool Matches(GrassBuildKey key, int segments)
        {
            return _key.Matches(key) && _segments == segments;
        }

        public void Show(Camera camera, Func<bool> isShown, Func<bool> isRingShown, UnityEngine.Object owner)
        {
            tufts?.Show(camera, isShown, owner);
            socles?.Show(camera, isShown, owner);
            rings?.Show(camera, isRingShown, owner);
        }

        public void SetHeight(float scale)
        {
            tufts?.BindTufts(seeds, states, visible, scale);
            socles?.BindTufts(seeds, states, visible, scale);
        }

        public void Dispatch(Camera camera, GraphicsBuffer zones, int zoneCount, Vector4 wind)
        {
            if (count == 0)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            for (int i = 0; i < _planes.Length; i++)
            {
                Vector3 normal = _planes[i].normal;
                _planeVectors[i] = new Vector4(normal.x, normal.y, normal.z, _planes[i].distance);
            }

            // Fields share this compute asset, so each dispatch binds its complete state.
            _compute.SetInt("_HLBladeCount", count);
            _compute.SetBuffer(_kernel, "_HLBladeSeeds", seeds);
            _compute.SetBuffer(_kernel, "_HLBladeStates", states);
            _compute.SetBuffer(_kernel, "_HLVisibleBlades", visible);
            _compute.SetVectorArray("_HLFrustumPlanes", _planeVectors);
            _compute.SetFloat("_HLCullMargin", GrassBounds.Envelope(_key.cellSize));
            _compute.SetVector("_HLGroundWind", wind);
            BindGround();
            _compute.SetBuffer(_kernel, "_HLZones", zones);
            _compute.SetInt("_HLZoneCount", zoneCount);
            visible.SetCounterValue(0);
            _compute.Dispatch(_kernel, (count + 63) / 64, 1, 1);
            GraphicsBuffer.CopyCount(visible, tufts.arguments, 4);
            GraphicsBuffer.CopyCount(visible, socles.arguments, 4);
        }

        void BindGround()
        {
            bool active = Shader.GetGlobalFloat(GroundSimulation.ActiveId) > 0.5f;
            Texture motion = active ? Shader.GetGlobalTexture(GroundSimulation.MotionId) : null;
            Texture crush = active ? Shader.GetGlobalTexture(GroundSimulation.CrushId) : null;
            Texture state = active ? Shader.GetGlobalTexture(GroundSimulation.StateId) : null;
            active = motion != null && crush != null && state != null;
            _compute.SetTexture(_kernel, GroundSimulation.MotionId, active ? motion : Texture2D.blackTexture);
            _compute.SetTexture(_kernel, GroundSimulation.CrushId, active ? crush : Texture2D.blackTexture);
            _compute.SetTexture(_kernel, GroundSimulation.StateId, active ? state : Texture2D.blackTexture);
            _compute.SetVector(GroundSimulation.RectId, active ? Shader.GetGlobalVector(GroundSimulation.RectId)
                : new Vector4(0f, 0f, 1f, 1f));
            _compute.SetFloat(GroundSimulation.ActiveId, active ? 1f : 0f);
        }

        public void Dispose()
        {
            tufts?.Release();
            socles?.Release();
            rings?.Release();
            tufts = null;
            socles = null;
            rings = null;
            seeds?.Dispose();
            states?.Dispose();
            visible?.Dispose();
            seeds = null;
            states = null;
            visible = null;
            count = 0;
        }
    }
}
