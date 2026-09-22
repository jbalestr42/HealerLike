using UnityEngine;

namespace HealerLike.Render.Zones
{
    /// <summary>
    /// Cosmetic Heal-kind range preview, never an active healing field. Reads Entity's current
    /// range and position; never calls Range.Show or any gameplay selection/drag methods.
    /// This clone exposes no public selected/dragging state on SelectableEntity/DraggableEntity.
    /// The default render-owned pointer selection is therefore an approximation; stage integrations
    /// may disable ObservePointer and supply exact state through SetPreviewState instead.
    /// </summary>
    public sealed class HLRangePreview : MonoBehaviour, IVisualBehaviour
    {
        [SerializeField] bool _observePointer = true;
        [SerializeField] Camera _camera;
        Entity _entity;
        SelectableEntity _selectable;
        DraggableEntity _draggable;
        HLZoneRegistry _owner;
        int _handle;
        bool _selected, _dragging;
        public bool ObservePointer { get => _observePointer; set => _observePointer = value; }
        public Camera PreviewCamera { get => _camera; set => _camera = value; }

        public void Init(Entity entity)
        {
            ClearZone();
            _selected = _dragging = false;
            _entity = entity;
            _selectable = entity != null ? entity.GetComponent<SelectableEntity>() : null;
            _draggable = entity != null ? entity.GetComponent<DraggableEntity>() : null;
        }

        void Start()
        {
            if (_entity == null) Init(GetComponentInParent<Entity>());
        }

        /// <summary>Render-owned state input for stage wiring, including non-pointer selection.</summary>
        public void SetPreviewState(bool selected, bool dragging)
        {
            _selected = selected;
            _dragging = dragging;
            Refresh();
        }

        void Update()
        {
            if (_observePointer && _entity != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) _selected = _dragging = false;
                else if (Input.GetMouseButtonDown(0))
                {
                    Camera camera = _camera != null ? _camera : Camera.main;
                    bool hitSelf = camera != null && Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var hit) &&
                        hit.collider.GetComponentInParent<Entity>() == _entity;
                    _selected = hitSelf && _selectable != null && _selectable.isActiveAndEnabled;
                    _dragging = hitSelf && _draggable != null && _draggable.isActiveAndEnabled && _entity.isDraggable;
                }
                if (Input.GetMouseButtonUp(0)) _dragging = false;
            }
            Refresh();
        }

        /// <summary>Samples current public range/position. Missing attributes or inactive entities hide it.</summary>
        public void Refresh()
        {
            var current = HLZoneRegistry.Current;
            if (_owner != current) ClearZone();
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled || !(_selected || _dragging) ||
                current == null || _entity.attributeManager == null || !_entity.attributeManager.Has(AttributeType.Range))
            {
                ClearZone();
                return;
            }
            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            if (_owner == null) _owner = current;
            if (!_owner.Contains(_handle))
                _handle = _owner.Add(HLZoneKind.Heal, _entity.transform.position, radius, 0.35f);
            else
                _owner.UpdateZone(_handle, HLZoneKind.Heal, _entity.transform.position, radius, 0.35f);
        }

        void ClearZone()
        {
            if (_owner != null) _owner.Remove(_handle);
            _owner = null;
            _handle = 0;
        }
        void OnDisable()
        {
            ClearZone();
            _selected = _dragging = false;
        }
        void OnDestroy() => ClearZone();
    }
}
