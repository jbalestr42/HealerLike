using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Zones
{
    // Projectile.Init calls this on every launch, reused projectiles included
    public class LaunchWave : AProjectileBehaviour
    {
        [SerializeField] GrassField _field;
        ZoneRegistry _zones;

        public GrassField field { get { return _field; } set { _field = value; } }

        // Render side setup, the RenderManager calls it before Projectile.Init runs the behaviours
        public void Init(ZoneRegistry zones, GrassField field)
        {
            _zones = zones;
            _field = field;
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

            if (!projectile || !projectile.target)
            {
                return;
            }

            Vector3 from = source.transform.position;
            Vector3 to = projectile.target.transform.position;
            if (_zones != null)
            {
                _zones.AddLaunch(from, to);
            }

            if (_field)
            {
                _field.TriggerGust(to - from);
            }
        }
    }
}
