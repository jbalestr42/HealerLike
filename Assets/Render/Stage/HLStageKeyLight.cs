using HealerLike.Render.Stones;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Stage
{
    /// <summary>
    /// One key light for the stage. HLLookController publishes its direction as _HLKeyLightDir; the builder aims
    /// the light along the stones' serialized cheap-shadow direction so both agree. When the active pipeline
    /// really renders main-light shadows for this light out to the board, the cheap ellipses are turned off.
    /// </summary>
    [DefaultExecutionOrder(-1900)]
    public sealed class HLStageKeyLight : MonoBehaviour
    {
        // Matches HLStoneEnemyVisual / HLStoneTerrainClump directionToKeyLight: upper left, toward the camera.
        public static readonly Vector3 StoneKeyDirection = new Vector3(-1, 2, -1);
        [SerializeField] Light keyLight;
        [Tooltip("Farthest board distance from the camera that real shadows must cover.")]
        [SerializeField, Min(1)] float requiredDistance = 50;
        [SerializeField, Min(.05f)] float pollSeconds = .5f;
        float _next;
        public Light KeyLight { get => keyLight; set => keyLight = value; }
        public bool RealShadows { get; private set; }
        public int Suppressed { get; private set; }

        /// <summary>Rotation whose -forward points toward the light along the given direction.</summary>
        public static Quaternion Aim(Vector3 directionToLight) => Quaternion.LookRotation(-directionToLight.normalized, Vector3.up);

        /// <summary>True when the pipeline's main light casts shadows from this light over the required distance.</summary>
        public static bool RendersRealShadows(UniversalRenderPipelineAsset pipeline, Light light, float requiredDistance)
            => pipeline && light && light.isActiveAndEnabled && light.type == LightType.Directional && light.shadows != LightShadows.None &&
               pipeline.mainLightRenderingMode == LightRenderingMode.PerPixel && pipeline.supportsMainLightShadows &&
               pipeline.shadowDistance >= requiredDistance;

        /// <summary>Cheap ellipses stay on only where real shadows are missing. Returns how many were switched off.</summary>
        public static int ApplyCheapShadows(bool realShadows, HLStoneEnemyVisual[] enemies, HLStoneTerrainClump[] clumps)
        {
            int off = 0;
            if (enemies != null) foreach (var e in enemies) if (e && e.groundShadowEnabled == realShadows) { e.groundShadowEnabled = !realShadows; if (realShadows) off++; }
            if (clumps != null) foreach (var c in clumps) if (c && c.groundShadowEnabled == realShadows) { c.groundShadowEnabled = !realShadows; if (realShadows) off++; }
            return off;
        }

        void OnEnable() { _next = 0; Refresh(); }

        // Stones appear with enemies and the terrain generator; poll at a low rate rather than per frame.
        void Update() { if (Time.unscaledTime >= _next) Refresh(); }

        public void Refresh()
        {
            _next = Time.unscaledTime + pollSeconds;
            RealShadows = RendersRealShadows(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset, keyLight, requiredDistance);
            Suppressed += ApplyCheapShadows(RealShadows,
                FindObjectsByType<HLStoneEnemyVisual>(FindObjectsSortMode.None), FindObjectsByType<HLStoneTerrainClump>(FindObjectsSortMode.None));
        }
    }
}
