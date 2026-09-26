using UnityEngine;

namespace HealerLike.Render.Look
{
    // The one look of the render stage, set up by the RenderManager at attach. The globals go up when they
    // change, on Init, on a fog update, on a new setting and on enable; nothing uploads them per frame.
    public class LookController : MonoBehaviour
    {
        // Starts from the defaults, which LookCore.hlsl mirrors; the fog changes it at run time
        LookSettings _settings = LookSettings.Default;

        public LookSettings settings
        {
            get { return _settings; }
            set
            {
                _settings = value.Validated();
                ApplyGlobals();
            }
        }

        void OnEnable()
        {
            ApplyGlobals();
        }

        // The fog range comes from the camera distance to the board, the hatch keeps its authored world spacing
        public void Init(Vector2 fogRange)
        {
            UpdateFog(fogRange);
        }

        // The fog starts past every playable corner seen from wherever the camera moved
        public void UpdateFog(Vector2 fogRange)
        {
            LookSettings value = _settings;
            value.fogStart = fogRange.x;
            value.fogEnd = fogRange.y;
            settings = value;
        }

        void OnDisable()
        {
            LookShaderProperties.ClearApplied();
        }

        public void ApplyGlobals()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            LookShaderProperties.Publish(_settings, QualitySettings.activeColorSpace);
        }

        public static Vector4 ToWorkingColor(Color srgb, ColorSpace colorSpace)
        {
            return LookShaderProperties.ToWorkingColor(srgb, colorSpace);
        }
    }
}
