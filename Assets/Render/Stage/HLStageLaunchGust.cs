using UnityEngine;
using HealerLike.Render.Environment;

namespace HealerLike.Render.Stage
{
    // Projectile.Init calls this once per launch, beside HLLaunchWave's grass gust
    public class HLStageLaunchGust : AProjectileBehaviour
    {
        public static readonly float Strength = 0.65f;
        public static readonly float Seconds = 0.8f;

        // removed in D2
        public static HLEnvironmentGust Target { get; set; }

        HLEnvironmentGust _gust;

        // The manager hands the environment gust before Projectile.Init runs the behaviours
        public void Init(HLEnvironmentGust gust)
        {
            _gust = gust;
        }

        public override void Init(GameObject source)
        {
            if (!isActiveAndEnabled || source == null)
            {
                return;
            }

            if (projectile == null)
            {
                projectile = GetComponent<Projectile>();
            }

            if (projectile == null || projectile.target == null)
            {
                return;
            }

            Launch(_gust != null ? _gust : Target, source.transform.position, projectile.target.transform.position);
        }

        public static bool Launch(HLEnvironmentGust gust, Vector3 sourcePosition, Vector3 targetPosition)
        {
            if (gust == null)
            {
                return false;
            }

            gust.Gust(targetPosition - sourcePosition, Strength, Seconds);
            return true;
        }
    }
}
