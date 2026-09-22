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
        [Min(.001f)] public float DepthThresholdWorld = 1f;
        [Min(1f)] public float ReferenceDistance = 31f;
        [Min(0f)] public float DistanceScale = 1f;
        [Range(0f, 180f)] public float NormalAngleDegrees = 55f;
        [Range(0f, 180f)] public float NormalDensityDegrees = 35f;
        [Tooltip("Use camera-normal alpha as per-object normal-edge eligibility. Grass already writes zero.")]
        public bool UseNormalEdgeMask = true;
        [SerializeField] private Shader edgeShader;
        private Material edgeMaterial;
        private HLOutlinesPass pass;

        public override void Create()
        {
            CoreUtils.Destroy(edgeMaterial);
            if (edgeShader == null) edgeShader = Shader.Find("Hidden/HL/Look/DepthNormalOutline");
            edgeMaterial = edgeShader != null ? CoreUtils.CreateEngineMaterial(edgeShader) : null;
            ApplyEdgeSettings();
            pass = new HLOutlinesPass(LayerMask, DepthNormalEdges ? edgeMaterial : null);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                renderingData.cameraData.cameraType == CameraType.Reflection) return;
            int main = renderingData.lightData.mainLightIndex;
            HLLookController.PublishMainLightDirection(main >= 0 ? renderingData.lightData.visibleLights[main].light : null);
            ApplyEdgeSettings();
            if (pass != null) renderer.EnqueuePass(pass);
        }

        public void ApplyEdgeSettings()
        {
            if (!edgeMaterial) return;
            edgeMaterial.SetVector("_HLEdgeDepth", new Vector4(
                Mathf.Max(.001f, DepthThresholdWorld), Mathf.Max(1f, ReferenceDistance), Mathf.Max(0f, DistanceScale), 0));
            edgeMaterial.SetVector("_HLEdgeNormals", new Vector4(
                Mathf.Clamp(NormalAngleDegrees, 0, 180), Mathf.Clamp(NormalDensityDegrees, 0, 180), UseNormalEdgeMask ? 1 : 0, 0));
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(edgeMaterial);
            edgeMaterial = null;
            pass = null;
        }
    }
}
