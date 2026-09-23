using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Range preview of an ally, the range driver tells it whether it is hovered and whether every range shows
    public class HLRangePreview : MonoBehaviour, IEntityView
    {
        Entity _entity;
        HLZoneRegistry _zones;
        HLZoneRegistry _owner;
        int _handle;
        bool _selected;
        bool _dragging;
        bool _isHovered;
        bool _showAll;

        public Entity entity { get { return _entity; } }

        public void Init(Entity entity, RenderManager manager)
        {
            Init(entity, manager.zones);
        }

        public void Init(Entity entity, HLZoneRegistry zones)
        {
            ClearZone();
            _zones = zones;
            _selected = false;
            _dragging = false;
            _entity = entity;
        }

        void Update()
        {
            Refresh();
        }

        // Called by HLStageRangeDriver, which owns the hover raycast and the show all toggle
        public void Show(bool isHovered, bool showAll)
        {
            _isHovered = isHovered;
            _showAll = showAll;
        }

        public void SetPreviewState(bool selected, bool dragging)
        {
            _selected = selected;
            _dragging = dragging;
        }

        public void Refresh()
        {
            if (_owner != _zones)
            {
                ClearZone();
            }

            bool isShown = _selected || _dragging || _isHovered || _showAll;
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled
                || _entity.entityType != Entity.EntityType.Player || !isShown
                || _zones == null || _entity.attributeManager == null
                || !_entity.attributeManager.Has(AttributeType.Range))
            {
                ClearZone();
                return;
            }

            float strength = _showAll ? 0.15f : 0.35f;
            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            _owner = _zones;
            if (!_owner.Contains(_handle))
            {
                _handle = _owner.Add(HLZoneKind.Range, _entity.transform.position, radius, strength);
            }
            else
            {
                _owner.RefreshZone(_handle, HLZoneKind.Range, _entity.transform.position, radius, strength);
            }
        }

        void ClearZone()
        {
            if (_owner != null)
            {
                _owner.Remove(_handle);
            }

            _owner = null;
            _handle = 0;
        }

        void OnDisable()
        {
            ClearZone();
            _selected = false;
            _dragging = false;
        }

        void OnDestroy()
        {
            ClearZone();
        }
    }
}
