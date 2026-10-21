using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Enemy range readout on the enemy views
    public class BruiseZone : MonoBehaviour, IEntityView
    {
        // A range as wide as the board bruises every cell and carries no information (the Soldier's is 100)
        public static readonly float BruiseMaxRange = 16f;

        Entity _entity;
        readonly ZoneHandle _zone = new ZoneHandle();

        public void Init(Entity entity, ZoneRegistry zones)
        {
            _zone.Init(zones);
            _entity = entity;
            Refresh();
        }

        public void Init(Entity entity, RenderManager manager)
        {
            Init(entity, manager.zones);
        }

        public static bool Bruises(float range)
        {
            return range > 0f && range < BruiseMaxRange;
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled
                || _entity.entityType != Entity.EntityType.Computer
                || (_entity.health != null && _entity.health.Value <= 0f)
                || _entity.attributeManager == null || !_entity.attributeManager.Has(AttributeType.Range))
            {
                _zone.Clear();
                return;
            }

            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            if (!Bruises(radius))
            {
                _zone.Clear();
                return;
            }

            _zone.Refresh(ZoneKind.Bruise, _entity.transform.position, radius, 1f);
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
