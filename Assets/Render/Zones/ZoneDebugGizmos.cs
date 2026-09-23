using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class ZoneDebugGizmos : MonoBehaviour
    {
        [SerializeField] ZoneRegistry _registry;

        public ZoneRegistry registry { get { return _registry; } set { _registry = value; } }

        public static Color ColorFor(Zone zone)
        {
            float red = zone.kind == (int)ZoneKind.Hostile ? 1f : 0f;
            float green = zone.kind == (int)ZoneKind.Heal ? 1f : 0.25f;
            return new Color(red, green, 0.25f, Mathf.Clamp01(zone.strength));
        }

        void OnDrawGizmos()
        {
            ZoneRegistry owner = _registry;
            if (owner == null)
            {
                return;
            }

            Color previous = Gizmos.color;
            foreach (Zone zone in owner.snapshot)
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
