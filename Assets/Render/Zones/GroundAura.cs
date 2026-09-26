using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // A creature's health written into the ground around it: a rocky enemy burns the grass to ash in a disc that
    // shrinks as its health falls, so the grass regrows toward it as it weakens; an ally's grass dies in a disc
    // that spreads and deepens as its health falls. The disc follows the creature and is gone with it.
    public class GroundAura : MonoBehaviour
    {
        public enum Mark
        {
            None,
            Ash,
            Wilt
        }

        // In cells: the ash around a rocky enemy at no health and what full health adds
        public static readonly float AshMinRadius = 0.4f;
        public static readonly float AshRadiusRange = 1.8f;
        // In cells: the dead grass around an ally at full health and what its missing health adds
        public static readonly float WiltMinRadius = 0.5f;
        public static readonly float WiltRadiusRange = 1.4f;

        GroundHandle _handle;
        ResourceAttribute _health;
        Collider _hold;
        float _cellSize = 1f;

        Mark _mark;
        public Mark mark { get { return _mark; } }

        public bool isShown { get { return _handle != null && _handle.isShown; } }

        // Enemies burn, allies wilt; any other side leaves the ground alone
        public void Init(Entity entity, Ground ground, float cellSize)
        {
            _health = entity != null ? entity.health : null;
            _hold = EntityHold.Find(entity);
            _mark = Mark.None;
            if (entity != null && entity.entityType == Entity.EntityType.Computer)
            {
                _mark = Mark.Ash;
            }
            else if (entity != null && entity.entityType == Entity.EntityType.Player)
            {
                _mark = Mark.Wilt;
            }

            _cellSize = RenderMath.IsPositive(cellSize) ? cellSize : 1f;
            _handle?.Release();
            _handle = null;
            if (ground != null && _mark != Mark.None)
            {
                _handle = ground.Hold(_mark == Mark.Ash ? ground.vocabulary.ash : ground.vocabulary.wilt);
            }
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_handle == null)
            {
                return;
            }

            // A held creature paints nothing along its drag
            if (!isActiveAndEnabled || _health == null || EntityHold.IsHeld(_hold))
            {
                _handle.Hide();
                return;
            }

            float health = HealthShare(_health.Value, _health.Max);
            _handle.Show(transform.position, Radius(_mark, health) * _cellSize, Strength(_mark, health));
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
        public static float Radius(Mark mark, float health)
        {
            health = Mathf.Clamp01(health);
            if (mark == Mark.Ash)
            {
                return AshMinRadius + AshRadiusRange * health;
            }

            if (mark == Mark.Wilt)
            {
                return WiltMinRadius + WiltRadiusRange * (1f - health);
            }

            return 0f;
        }

        // Ash burns fully while the enemy stands; an ally's grass dies as deep as its health is missing
        public static float Strength(Mark mark, float health)
        {
            health = Mathf.Clamp01(health);
            if (mark == Mark.Ash)
            {
                return health > 0f ? 1f : 0f;
            }

            if (mark == Mark.Wilt)
            {
                return 1f - health;
            }

            return 0f;
        }

        void OnDisable()
        {
            _handle?.Hide();
        }

        void OnDestroy()
        {
            _handle?.Release();
            _handle = null;
        }
    }
}
