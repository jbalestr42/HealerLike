using UnityEngine;

namespace HealerLike.Render.Zones
{
    /// <summary>Cosmetic debug view of the published zones; does not allocate or own render resources.</summary>
    public sealed class HLZoneDebugGizmos : MonoBehaviour
    {
        [SerializeField] HLZoneRegistry _registry;
        public HLZoneRegistry Registry { get => _registry; set => _registry = value; }
        public static Color ColorFor(HLZone zone) => new Color(
            zone.kind == (int)HLZoneKind.Hostile ? 1 : 0,
            zone.kind == (int)HLZoneKind.Heal ? 1 : 0.25f, 0.25f, Mathf.Clamp01(zone.strength));

        void OnDrawGizmos()
        {
            var owner = _registry != null ? _registry : HLZoneRegistry.Current;
            if (owner == null) return;
            Color previous = Gizmos.color;
            foreach (var zone in owner.Snapshot)
            {
                Gizmos.color = ColorFor(zone);
                Vector3 last = zone.position + Vector3.right * zone.radius;
                for (int i = 1; i <= 64; i++)
                {
                    float angle = i * (2 * Mathf.PI / 64);
                    Vector3 next = zone.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * zone.radius;
                    Gizmos.DrawLine(last, next);
                    last = next;
                }
            }
            Gizmos.color = previous;
        }
    }
}
