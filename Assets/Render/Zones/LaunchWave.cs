using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Projectile.Init calls this on every launch, reused projectiles included
    public class LaunchWave : AProjectileBehaviour
    {
        ZoneRegistry _zones;

        // Render side setup, the RenderManager calls it before Projectile.Init runs the behaviours
        public void Init(ZoneRegistry zones)
        {
            _zones = zones;
        }

        public override void Init(GameObject source)
        {
            if (!isActiveAndEnabled || !source)
            {
                return;
            }

            if (!projectile)
            {
                projectile = GetComponent<Projectile>();
            }

            if (!projectile || !projectile.target || _zones == null)
            {
                return;
            }

            _zones.AddLaunch(source.transform.position, projectile.target.transform.position);
        }
    }
}
