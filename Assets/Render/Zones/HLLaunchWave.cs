using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Zones
{
    /// <summary>Projectile.Init calls this once per launch, including reused projectile instances.</summary>
    [DisallowMultipleComponent]
    public sealed class HLLaunchWave : AProjectileBehaviour
    {
        [SerializeField] HLGrassField _field;
        public HLGrassField Field { get => _field; set => _field = value; }
        public override void Init(GameObject source)
        {
            if (!isActiveAndEnabled || !source) return;
            if (!projectile) projectile = GetComponent<Projectile>();
            if (!projectile || !projectile.target) return;
            Vector3 from = source.transform.position, to = projectile.target.transform.position;
            HLZoneRegistry.Current?.AddLaunch(from, to);
            if (!_field) _field = FindAnyObjectByType<HLGrassField>();
            if (_field) _field.TriggerGust(to - from);
        }
    }
}
