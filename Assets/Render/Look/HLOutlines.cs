using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Look
{
    /// <summary>Explicit primitive hull draw plus depth/normal edges, including grass.
    /// The integration owner adds this feature to renderer assets and retains the edge shader.</summary>
    public sealed class HLOutlines : ScriptableRendererFeature
    {
        [Tooltip("Object layers selected for primitive hulls. Screen edges use the camera depth/normals.")]
        public LayerMask LayerMask = ~0;
        [Tooltip("Add depth/normal edges, including indirect grass that writes camera depth and normals.")]
        public bool DepthNormalEdges = true;
        [SerializeField] private Shader edgeShader;
        private Material edgeMaterial;
        private HLOutlinesPass pass;

        public override void Create()
        {
            CoreUtils.Destroy(edgeMaterial);
            if (edgeShader == null) edgeShader = Shader.Find("Hidden/HL/Look/DepthNormalOutline");
            edgeMaterial = edgeShader != null ? CoreUtils.CreateEngineMaterial(edgeShader) : null;
            pass = new HLOutlinesPass(LayerMask, DepthNormalEdges ? edgeMaterial : null);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                renderingData.cameraData.cameraType == CameraType.Reflection) return;
            if (pass != null) renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(edgeMaterial);
            edgeMaterial = null;
            pass = null;
        }
    }
}
