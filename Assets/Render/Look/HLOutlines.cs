using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Look
{

    public class HLOutlines : ScriptableRendererFeature
    {
                public LayerMask LayerMask = ~0;
                public bool DepthNormalEdges = true;
        [Min(0.001f)] public float DepthThresholdWorld = 1f;
        [Min(1f)] public float ReferenceDistance = 31f;
        [Min(0f)] public float DistanceScale = 1f;
        [Range(0f, 180f)] public float NormalAngleDegrees = 55f;
        [Range(0f, 180f)] public float NormalDensityDegrees = 35f;
                public bool UseNormalEdgeMask = true;
        [SerializeField] Shader edgeShader;
        Material edgeMaterial;
        HLOutlinesPass pass;

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
                Mathf.Max(0.001f, DepthThresholdWorld), Mathf.Max(1f, ReferenceDistance), Mathf.Max(0f, DistanceScale), 0));
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
