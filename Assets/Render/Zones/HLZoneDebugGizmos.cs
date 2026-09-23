using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLZoneDebugGizmos : MonoBehaviour
    {
        [SerializeField] HLZoneRegistry _registry;

        public HLZoneRegistry registry { get { return _registry; } set { _registry = value; } }

        public static Color ColorFor(HLZone zone)
        {
            float red = zone.kind == (int)HLZoneKind.Hostile ? 1f : 0f;
            float green = zone.kind == (int)HLZoneKind.Heal ? 1f : 0.25f;
            return new Color(red, green, 0.25f, Mathf.Clamp01(zone.strength));
        }

        void OnDrawGizmos()
        {
            HLZoneRegistry owner = _registry;
            if (owner == null)
            {
                return;
            }

            Color previous = Gizmos.color;
            foreach (HLZone zone in owner.snapshot)
            {
                Gizmos.color = ColorFor(zone);
                Vector3 last = zone.position + Vector3.right * zone.radius;
                for (int i = 1; i <= 64; i++)
                {
                    float angle = i * (2 * Mathf.PI / 64);
                    Vector3 next = zone.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * zone.radius;
                    Gizmos.DrawLine(last, next);
                    last = next;
                }
            }

            Gizmos.color = previous;
        }
    }
}
