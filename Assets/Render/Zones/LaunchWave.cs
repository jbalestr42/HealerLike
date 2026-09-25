using UnityEngine;
using HealerLike.Render.Environment;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Zones
{
    // One launch: a directional pulse through the grass and a gust through the environment, on every shot the
    // projectile starts, reused projectiles included
    public class LaunchWave : AProjectileBehaviour
    {
        public static readonly float GustStrength = 0.65f;
        public static readonly float GustSeconds = 0.8f;

        ZoneRegistry _zones;
        EnvironmentGust _gust;

        // The RenderManager calls it before Projectile.Init runs the behaviours
        public void Init(ZoneRegistry zones, EnvironmentGust gust)
        {
            _zones = zones;
            _gust = gust;
        }

        public override void Init(GameObject source)
        {
            if (!isActiveAndEnabled || source == null || projectile == null || projectile.target == null)
            {
                return;
            }

            Vector3 sourcePosition = source.transform.position;
            CharacterView screenSource = CharacterView.ScreenSource(source);
            if (screenSource)
            {
                screenSource.TryGetCastPoint(out sourcePosition);
            }
            Vector3 targetPosition = projectile.target.transform.position;
            if (_zones != null)
            {
                _zones.AddLaunch(sourcePosition, targetPosition);
            }

            if (_gust != null)
            {
                _gust.Gust(targetPosition - sourcePosition, GustStrength, GustSeconds);
            }
        }
    }
}
