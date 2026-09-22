using HealerLike.Render.Environment;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    /// <summary>Stage launch adapter: Projectile.Init calls this once per real launch, beside HLLaunchWave's grass gust.</summary>
    [DisallowMultipleComponent]
    public sealed class HLStageLaunchGust : AProjectileBehaviour
    {
        // Environment beauty report: .65 strength over .8 s per launch.
        public const float Strength = .65f, Seconds = .8f;
        // Set by HLStageBeautyWiring on enable, cleared on disable. Prefabs cannot reference the scene's gust.
        public static HLEnvironmentGust Target { get; set; }
        public override void Init(GameObject source)
        {
            if (!isActiveAndEnabled || !source) return;
            if (!projectile) projectile = GetComponent<Projectile>();
            if (!projectile || !projectile.target) return;
            Launch(Target, source.transform.position, projectile.target.transform.position);
        }
        public static bool Launch(HLEnvironmentGust gust, Vector3 sourcePosition, Vector3 targetPosition)
        {
            if (!gust) return false;
            gust.Gust(targetPosition - sourcePosition, Strength, Seconds);
            return true;
        }
    }
}
