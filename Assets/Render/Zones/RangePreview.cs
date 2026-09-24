using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Range preview of an ally, the range driver tells it whether it is hovered
    public class RangePreview : MonoBehaviour, IEntityView
    {
        // Strength of the ring for the hovered, selected or dragged unit
        public static readonly float FocusStrength = 0.35f;

        Entity _entity;
        readonly ZoneHandle _zone = new ZoneHandle();
        bool _selected;
        bool _dragging;
        bool _isHovered;

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

        // Called by StageRangeDriver, which owns the hover raycast
        public void Show(bool isHovered)
        {
            _isHovered = isHovered;
        }

        public void SetPreviewState(bool selected, bool dragging)
        {
            _selected = selected;
            _dragging = dragging;
        }

        public void Refresh()
        {
            bool isShown = _selected || _dragging || _isHovered;
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled
                || _entity.entityType != Entity.EntityType.Player || !isShown
                || _entity.attributeManager == null
                || !_entity.attributeManager.Has(AttributeType.Range))
            {
                _zone.Clear();
                return;
            }

            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            _zone.Refresh(ZoneKind.Range, _entity.transform.position, radius, FocusStrength);
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
