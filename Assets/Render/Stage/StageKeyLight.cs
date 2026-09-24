using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Stage
{
    // The stage key light, and whether the pipeline renders its real shadows out to the board.
    // The stones' cheap ground shadows read that and hide while real ones are drawn
    public class StageKeyLight : MonoBehaviour
    {
        // Upper right, a little behind the board, so cast shadows fall to the lower left of the portrait view.
        // Mostly from the side: from further behind, the camera sees the unlit side of every grass tuft.
        public static readonly Vector3 KeyDirection = new Vector3(1f, 1.7f, 0.5f);

        [SerializeField] Light _keyLight;
        // Farthest board distance from the camera that real shadows must cover
        [SerializeField] float _requiredDistance = 50f;
        [SerializeField] float _pollSeconds = 0.5f;

        float _next;
        bool _isInitialized = false;

        public Light keyLight { get { return _keyLight; } set { _keyLight = value; } }

        bool _realShadows;
        public bool realShadows { get { return _realShadows; } }

        public void Init()
        {
            _isInitialized = true;
            Refresh();
        }

        // The pipeline can be swapped after Init, so this polls at a low rate rather than every frame
        void Update()
        {
            if (_isInitialized && Time.unscaledTime >= _next)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            _next = Time.unscaledTime + _pollSeconds;
            RenderPipelineAsset current = GraphicsSettings.currentRenderPipeline;
            UniversalRenderPipelineAsset pipeline = current as UniversalRenderPipelineAsset;
            _realShadows = RendersRealShadows(pipeline, _keyLight, _requiredDistance);
        }

        // Rotation whose back points toward the light along the given direction
        public static Quaternion Aim(Vector3 directionToLight)
        {
            return Quaternion.LookRotation(-directionToLight.normalized, Vector3.up);
        }

        public static bool RendersRealShadows(UniversalRenderPipelineAsset pipeline, Light light,
                                              float requiredDistance)
        {
            if (pipeline == null || light == null || !light.isActiveAndEnabled)
            {
                return false;
            }

            return light.type == LightType.Directional && light.shadows != LightShadows.None
                   && pipeline.mainLightRenderingMode == LightRenderingMode.PerPixel
                   && pipeline.supportsMainLightShadows
                   && pipeline.shadowDistance >= requiredDistance;
        }

    }
}
