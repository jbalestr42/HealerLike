using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Range preview of an ally, the range driver tells it whether it is hovered and whether every range shows
    public class RangePreview : MonoBehaviour, IEntityView
    {
        // Strength of the ring for the hovered, selected or dragged unit, and when every range shows
        public static readonly float FocusStrength = 0.35f;
        public static readonly float ShowAllStrength = 0.15f;

        Entity _entity;
        readonly ZoneHandle _zone = new ZoneHandle();
        bool _selected;
        bool _dragging;
        bool _isHovered;
        bool _showAll;

        public Entity entity { get { return _entity; } }

        public void Init(Entity entity, RenderManager manager)
        {
            Init(entity, manager.zones);
        }

        public void Init(Entity entity, ZoneRegistry zones)
        {
            _zone.Init(zones);
            _selected = false;
            _dragging = false;
            _entity = entity;
        }

        void Update()
        {
            Refresh();
        }

        // Called by StageRangeDriver, which owns the hover raycast and the show all toggle
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
            bool isShown = _selected || _dragging || _isHovered || _showAll;
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled
                || _entity.entityType != Entity.EntityType.Player || !isShown
                || _entity.attributeManager == null
                || !_entity.attributeManager.Has(AttributeType.Range))
            {
                _zone.Clear();
                return;
            }

            float strength = _showAll ? ShowAllStrength : FocusStrength;
            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            _zone.Refresh(ZoneKind.Range, _entity.transform.position, radius, strength);
        }

        void OnDisable()
        {
            _zone.Clear();
            _selected = false;
            _dragging = false;
        }

        void OnDestroy()
        {
            _zone.Clear();
        }
    }
}
