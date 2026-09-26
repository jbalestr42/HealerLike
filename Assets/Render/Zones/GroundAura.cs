using UnityEngine;

namespace HealerLike.Render.Zones
{
    // A creature's health written into the ground around it: a rocky enemy burns the grass to ash in a disc that
    // shrinks as its health falls, so the grass regrows toward it as it weakens; an ally's grass dies in a disc
    // that spreads and deepens as its health falls. The disc follows the creature and is gone with it.
    public class GroundAura : MonoBehaviour
    {
        // In cells: the ash around a rocky enemy at no health and what full health adds
        public static readonly float AshMinRadius = 0.4f;
        public static readonly float AshRadiusRange = 1.8f;
        // In cells: the dead grass around an ally at full health and what its missing health adds
        public static readonly float WiltMinRadius = 0.5f;
        public static readonly float WiltRadiusRange = 1.4f;

        readonly ZoneHandle _zone = new ZoneHandle();
        ResourceAttribute _health;
        Collider _hold;
        ZoneKind _kind;
        float _cellSize = 1f;

        public ZoneKind kind { get { return _kind; } }

        // Enemies burn, allies wilt; any other side leaves the ground alone
        public void Init(Entity entity, ZoneRegistry zones, float cellSize)
        {
            _health = entity != null ? entity.health : null;
            _hold = EntityHold.Find(entity);
            _kind = ZoneKind.None;
            if (entity != null && entity.entityType == Entity.EntityType.Computer)
            {
                _kind = ZoneKind.Ash;
            }
            else if (entity != null && entity.entityType == Entity.EntityType.Player)
            {
                _kind = ZoneKind.Wilt;
            }

            _cellSize = RenderMath.IsPositive(cellSize) ? cellSize : 1f;
            _zone.Init(zones);
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            // A held creature paints nothing along its drag
            if (!isActiveAndEnabled || _health == null || _kind == ZoneKind.None || EntityHold.IsHeld(_hold))
            {
                _zone.Clear();
                return;
            }

            float health = HealthShare(_health.Value, _health.Max);
            float radius = Radius(_kind, health) * _cellSize;
            float strength = Strength(_kind, health);
            if (strength <= 0f || radius <= 0f)
            {
                _zone.Clear();
                return;
            }

            _zone.Refresh(_kind, transform.position, radius, strength);
        }

        public static float HealthShare(float value, float max)
        {
            if (!RenderMath.IsPositive(max) || !float.IsFinite(value))
            {
                return 0f;
            }

            return Mathf.Clamp01(value / max);
        }

        // In cells, for a health share
        public static float Radius(ZoneKind kind, float health)
        {
            health = Mathf.Clamp01(health);
            if (kind == ZoneKind.Ash)
            {
                return AshMinRadius + AshRadiusRange * health;
            }

            if (kind == ZoneKind.Wilt)
            {
                return WiltMinRadius + WiltRadiusRange * (1f - health);
            }

            return 0f;
        }

        // Ash burns fully while the enemy stands; an ally's grass dies as deep as its health is missing
        public static float Strength(ZoneKind kind, float health)
        {
            health = Mathf.Clamp01(health);
            if (kind == ZoneKind.Ash)
            {
                return health > 0f ? 1f : 0f;
            }

            if (kind == ZoneKind.Wilt)
            {
                return 1f - health;
            }

            return 0f;
        }

        void OnDisable()
        {
            _zone.Clear();
        }

        void OnDestroy()
        {
            _zone.Clear();
        }
    }
}
