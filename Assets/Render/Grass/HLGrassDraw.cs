using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Grass
{
    // One indirect draw over a borrowed mesh; owns its material and its argument buffer
    public class HLGrassDraw
    {
        Mesh _mesh;
        RenderParams _parameters;

        Material _material;
        public Material material { get { return _material; } }

        GraphicsBuffer _arguments;
        public GraphicsBuffer arguments { get { return _arguments; } }

        public MaterialPropertyBlock properties { get { return _parameters.matProps; } }

        public HLGrassDraw(Mesh mesh, Material material, uint instanceCount, Bounds bounds, int layer)
        {
            _mesh = mesh;
            _material = material;

            GraphicsBuffer.IndirectDrawIndexedArgs[] data = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            data[0].indexCountPerInstance = mesh.GetIndexCount(0);
            data[0].instanceCount = instanceCount;
            data[0].startIndex = mesh.GetIndexStart(0);
            data[0].baseVertexIndex = (uint)mesh.GetBaseVertex(0);
            data[0].startInstance = 0;
            _arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
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

        // Indirect submissions last for one render, so they are queued for the camera consuming them
        public void Submit(Camera camera)
        {
            _parameters.camera = camera;
            Graphics.RenderMeshIndirect(_parameters, _mesh, _arguments);
        }

        public void Release()
        {
            if (_arguments != null)
            {
                _arguments.Dispose();
                _arguments = null;
            }
            HLPrimitiveMeshes.DestroyOwned(_material);
            _material = null;
            _mesh = null;
        }
    }
}
