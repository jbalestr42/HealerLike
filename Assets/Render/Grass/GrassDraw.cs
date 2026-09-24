using System;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{
    // One indirect draw over a borrowed mesh and material; owns its argument buffer
    public class GrassDraw
    {
        Mesh _mesh;
        RenderParams _parameters;
        Camera _camera;
        Func<bool> _isShown;

        Material _material;
        public Material material { get { return _material; } }

        GraphicsBuffer _arguments;
        public GraphicsBuffer arguments { get { return _arguments; } }

        public MaterialPropertyBlock properties { get { return _parameters.matProps; } }

        public ShadowCastingMode shadowCastingMode
        {
            get { return _parameters.shadowCastingMode; }
            set { _parameters.shadowCastingMode = value; }
        }

        public GrassDraw(Mesh mesh, Material material, uint instanceCount, Bounds bounds, int layer)
        {
            _mesh = mesh;
            _material = material;

            GraphicsBuffer.IndirectDrawIndexedArgs[] data = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            data[0].indexCountPerInstance = mesh.GetIndexCount(0);
            data[0].instanceCount = instanceCount;
            data[0].startIndex = mesh.GetIndexStart(0);
            data[0].baseVertexIndex = (uint)mesh.GetBaseVertex(0);
            data[0].startInstance = 0;
            _arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1,
                                            GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _arguments.SetData(data);

            _parameters = new RenderParams(material);
            _parameters.worldBounds = bounds;
            _parameters.matProps = new MaterialPropertyBlock();
            _parameters.layer = layer;
            _parameters.shadowCastingMode = ShadowCastingMode.Off;
            _parameters.receiveShadows = true;
            _parameters.lightProbeUsage = LightProbeUsage.Off;
            _parameters.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // A tuft or socle over the field's tufts; a lean of one tilts the mesh with its tuft, zero keeps it flat
        public static GrassDraw Tufts(Mesh mesh, Material material, Bounds bounds, int layer, float lean)
        {
            GrassDraw draw = new GrassDraw(mesh, material, 0, bounds, layer);
            draw.properties.SetFloat("_HLTuftLean", lean);
            return draw;
        }

        // One ring per zone slot, clipped to the field
        public static GrassDraw Rings(Mesh mesh, Material material, Bounds bounds, int layer, GrassBuildKey key)
        {
            GrassDraw draw = new GrassDraw(mesh, material, (uint)ZonePacker.MaxZones, bounds, layer);
            draw.properties.SetFloat("_HLSurfaceY", key.surfaceY);
            draw.properties.SetVector("_HLFieldRect", key.FieldRect());
            return draw;
        }

        // The tuft seeds and states the compute writes, the ids of the tufts it found visible, and the
        // presentation-only height scale
        public void BindTufts(GraphicsBuffer seeds, GraphicsBuffer states, GraphicsBuffer visibleIds,
                              float heightScale)
        {
            properties.SetBuffer("_HLBladeSeeds", seeds);
            properties.SetBuffer("_HLBladeStates", states);
            properties.SetBuffer("_HLVisibleBladeIDs", visibleIds);
            properties.SetFloat("_HLBladeHeightScale", heightScale);
        }

        // Draws on every render of the camera while isShown says so. Indirect submissions last for one render,
        // so each is queued from the render of the camera consuming it, and Editor repaints without a
        // player-loop tick still draw.
        public void Show(Camera camera, Func<bool> isShown)
        {
            Hide();
            _camera = camera;
            _isShown = isShown;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        public void Hide()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _camera = null;
            _isShown = null;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || !_isShown())
            {
                return;
            }

            _parameters.camera = camera;
            Graphics.RenderMeshIndirect(_parameters, _mesh, _arguments);
        }

        public void Release()
        {
            Hide();
            if (_arguments != null)
            {
                _arguments.Dispose();
                _arguments = null;
            }

            _material = null;
            _mesh = null;
        }
    }
}
