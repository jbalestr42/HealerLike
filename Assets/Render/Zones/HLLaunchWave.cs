using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Zones
{
    // Projectile.Init calls this on every launch, reused projectiles included
    public class HLLaunchWave : AProjectileBehaviour
    {
        [SerializeField] HLGrassField _field;
        HLZoneRegistry _zones;
        bool _isInitialized = false;

        public HLGrassField field { get { return _field; } set { _field = value; } }

        // Render side setup, the RenderManager calls it before Projectile.Init runs the behaviours
        public void Init(HLZoneRegistry zones, HLGrassField field)
        {
            _zones = zones;
            _field = field;
            _isInitialized = true;
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
            // Falls back to the static registry until the RenderManager calls Init, removed in D2
            HLZoneRegistry zones = _isInitialized ? _zones : HLZoneRegistry.current;
            if (zones != null)
            {
                zones.AddLaunch(from, to);
            }

            // Scene lookup for the staged projectile copies, removed in D2
            if (!_isInitialized && !_field)
            {
                _field = FindAnyObjectByType<HLGrassField>();
            }

            if (_field)
            {
                _field.TriggerGust(to - from);
            }
        }
    }
}
