using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Look
{
    // Primitive hulls plus depth and normal edges, the grass gets its edges from the second part
    public class Outlines : ScriptableRendererFeature
    {
        // Layers drawn as primitive hulls, the screen edges use the camera depth and normals instead
        public LayerMask layerMask = ~0;
        public bool depthNormalEdges = true;
        public float depthThresholdWorld = 1f;
        public float referenceDistance = 31f;
        public float distanceScale = 1f;
        public float normalAngleDegrees = 55f;
        public float normalDensityDegrees = 35f;

        // The camera normal alpha says which objects get normal edges, the grass writes zero
        public bool useNormalEdgeMask = true;

        [SerializeField] Shader _edgeShader;

        Material _edgeMaterial;
        OutlinesPass _pass;

        public Shader edgeShader { get { return _edgeShader; } set { _edgeShader = value; } }

        public override void Create()
        {
            CoreUtils.Destroy(_edgeMaterial);
            // Without the edge shader only the hulls draw, the renderer asset references it
            _edgeMaterial = _edgeShader != null ? CoreUtils.CreateEngineMaterial(_edgeShader) : null;
            ApplyEdgeSettings();
            Material edgeMaterial = null;
            if (depthNormalEdges)
            {
                edgeMaterial = _edgeMaterial;
            }

            _pass = new OutlinesPass(layerMask, edgeMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            {
                return;
            }

            ApplyEdgeSettings();
            if (_pass != null)
            {
                renderer.EnqueuePass(_pass);
            }
        }

        public void ApplyEdgeSettings()
        {
            if (!_edgeMaterial)
            {
                return;
            }

            Vector4 depth = new Vector4(Mathf.Max(0.001f, depthThresholdWorld), Mathf.Max(1f, referenceDistance),
                                        Mathf.Max(0f, distanceScale), 0f);
            float normalEdgeMask = 0f;
            if (useNormalEdgeMask)
            {
                normalEdgeMask = 1f;
            }

            Vector4 normals = new Vector4(Mathf.Clamp(normalAngleDegrees, 0f, 180f),
                                          Mathf.Clamp(normalDensityDegrees, 0f, 180f), normalEdgeMask, 0f);
            _edgeMaterial.SetVector("_HLEdgeDepth", depth);
            _edgeMaterial.SetVector("_HLEdgeNormals", normals);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_edgeMaterial);
            _edgeMaterial = null;
            _pass = null;
        }
    }
}
