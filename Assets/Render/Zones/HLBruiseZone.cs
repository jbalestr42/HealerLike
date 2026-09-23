using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class HLBruiseZone : MonoBehaviour, IVisualBehaviour
    {
        Entity _entity;
        HLZoneRegistry _owner;
        int _handle;
        public void Init(Entity entity) { Clear(); _entity = entity; Refresh(); }
        void Start() { if (!_entity) Init(GetComponentInParent<Entity>()); }
        void Update() => Refresh();
        public void Refresh()
        {
            if (_owner != HLZoneRegistry.Current) Clear();
            if (!isActiveAndEnabled || !_entity || !_entity.isActiveAndEnabled ||
                _entity.entityType != Entity.EntityType.Computer ||
                (_entity.health != null && _entity.health.Value <= 0) ||
                _entity.attributeManager == null || !_entity.attributeManager.Has(AttributeType.Range))
            { Clear(); return; }
            _owner = HLZoneRegistry.Current;
            if (!_owner) return;
            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            if (!_owner.Contains(_handle)) _handle = _owner.Add(HLZoneKind.Bruise, _entity.transform.position, radius, 1);
            else _owner.RefreshZone(_handle, HLZoneKind.Bruise, _entity.transform.position, radius, 1);
        }
        void Clear() { if (_owner) _owner.Remove(_handle); _owner = null; _handle = 0; }
        void OnDisable() => Clear();
        void OnDestroy() => Clear();
    }
}
