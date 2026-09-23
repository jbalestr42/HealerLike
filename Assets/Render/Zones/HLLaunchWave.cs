using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Zones
{
    // Projectile.Init calls this on every launch, reused projectiles included
    public class HLLaunchWave : AProjectileBehaviour
    {
        [SerializeField] HLGrassField _field;

        public HLGrassField field { get { return _field; } set { _field = value; } }

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
            HLZoneRegistry.current?.AddLaunch(from, to);
            if (!_field)
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
