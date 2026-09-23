using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HealerLike.Render.Stones;

namespace HealerLike.Render.Stage
{
    // The stage key light. When the pipeline renders real main light shadows out to the board, the stones'
    // cheap ground ellipses are turned off.
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

        public bool realShadows { get; private set; }

        public int suppressed { get; private set; }

        public void Init()
        {
            _isInitialized = true;
            Refresh();
        }

        // Stones appear with enemies, so this polls at a low rate rather than every frame
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
            realShadows = RendersRealShadows(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset, _keyLight,
                                             _requiredDistance);
            // Discs the key light already hid are inactive, so they are searched too
            suppressed += ApplyCheapShadows(realShadows, FindObjectsByType<StoneGroundDisc>(FindObjectsInactive.Include,
                                                                                         FindObjectsSortMode.None));
        }

        // Rotation whose back points toward the light along the given direction
        public static Quaternion Aim(Vector3 directionToLight)
        {
            return Quaternion.LookRotation(-directionToLight.normalized, Vector3.up);
        }

        public static bool RendersRealShadows(UniversalRenderPipelineAsset pipeline, Light light, float requiredDistance)
        {
            if (pipeline == null || light == null || !light.isActiveAndEnabled)
            {
                return false;
            }

            return light.type == LightType.Directional && light.shadows != LightShadows.None
                   && pipeline.mainLightRenderingMode == LightRenderingMode.PerPixel && pipeline.supportsMainLightShadows
                   && pipeline.shadowDistance >= requiredDistance;
        }

        // Returns how many cheap ellipses were switched off
        public static int ApplyCheapShadows(bool realShadows, StoneGroundDisc[] discs)
        {
            int off = 0;
            if (discs == null)
            {
                return off;
            }

            foreach (StoneGroundDisc disc in discs)
            {
                if (disc != null && disc.isShadow && disc.isAllowed == realShadows)
                {
                    disc.isAllowed = !realShadows;
                    off += realShadows ? 1 : 0;
                }
            }

            return off;
        }
    }
}
