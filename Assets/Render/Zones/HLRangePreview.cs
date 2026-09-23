using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class HLRangePreview : MonoBehaviour, IVisualBehaviour
    {
        [SerializeField] bool _observePointer = true;
        [SerializeField] Camera _camera;
        Entity _entity;
        public static bool AllRanges { get; set; }
        static int _hoverFrame = -1;
        static Entity _hovered;
        public bool ObserveHover { get; set; } = true;
        public static Entity SampleHover(Ray ray, int frame)
        {
            if (_hoverFrame == frame) return _hovered;
            _hoverFrame = frame;
            _hovered = Physics.Raycast(ray, out var hit) ? hit.collider.GetComponentInParent<Entity>() : null;
            return _hovered;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { AllRanges = false; _hoverFrame = -1; _hovered = null; }
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
        }

        void Start()
        {
            if (_entity == null) Init(GetComponentInParent<Entity>());
        }

        public void SetPreviewState(bool selected, bool dragging)
        {
            _selected = selected;
            _dragging = dragging;
        }

        void Update() => Refresh();

        public void Refresh()
        {
            Camera camera = ObserveHover ? (_camera != null ? _camera : Camera.main) : null;
            bool hovered = camera != null && SampleHover(camera.ScreenPointToRay(Input.mousePosition), Time.frameCount) == _entity;
            var current = HLZoneRegistry.Current;
            if (_owner != current) ClearZone();
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled || _entity.entityType != Entity.EntityType.Player || !(_selected || _dragging || hovered || AllRanges) ||
                current == null || _entity.attributeManager == null || !_entity.attributeManager.Has(AttributeType.Range))
            {
                ClearZone();
                return;
            }
            float strength = AllRanges ? 0.15f : 0.35f;
            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            if (_owner == null) _owner = current;
            if (!_owner.Contains(_handle))
                _handle = _owner.Add(HLZoneKind.Range, _entity.transform.position, radius, strength);
            else
                _owner.RefreshZone(_handle, HLZoneKind.Range, _entity.transform.position, radius, strength);
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
