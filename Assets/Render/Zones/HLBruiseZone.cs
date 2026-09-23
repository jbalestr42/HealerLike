using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Enemy range readout on the enemy views
    public class HLBruiseZone : MonoBehaviour, IEntityView
    {
        // A range as wide as the board bruises every cell and carries no information (the Soldier's is 100)
        public static readonly float BruiseMaxRange = 16f;

        Entity _entity;
        HLZoneRegistry _zones;
        HLZoneRegistry _owner;
        int _handle;

        public void Init(Entity entity, HLZoneRegistry zones)
        {
            Clear();
            _zones = zones;
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
            HLZoneRegistry zones = _zones;
            if (_owner != zones)
            {
                Clear();
            }

            if (!isActiveAndEnabled || !_entity || !_entity.isActiveAndEnabled
                || _entity.entityType != Entity.EntityType.Computer
                || (_entity.health != null && _entity.health.Value <= 0f)
                || _entity.attributeManager == null || !_entity.attributeManager.Has(AttributeType.Range))
            {
                Clear();
                return;
            }

            _owner = zones;
            if (!_owner)
            {
                return;
            }

            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            if (!Bruises(radius))
            {
                Clear();
                return;
            }

            if (!_owner.Contains(_handle))
            {
                _handle = _owner.Add(HLZoneKind.Bruise, _entity.transform.position, radius, 1f);
            }
            else
            {
                _owner.RefreshZone(_handle, HLZoneKind.Bruise, _entity.transform.position, radius, 1f);
            }
        }

        void Clear()
        {
            if (_owner)
            {
                _owner.Remove(_handle);
            }

            _owner = null;
            _handle = 0;
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}
