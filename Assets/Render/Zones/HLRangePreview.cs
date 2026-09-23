using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Range preview of an ally, all the previews share one hover raycast per frame
    public class HLRangePreview : MonoBehaviour, IVisualBehaviour
    {
        [SerializeField] bool _observePointer = true;
        [SerializeField] Camera _camera;
        Entity _entity;
        HLZoneRegistry _owner;
        int _handle;
        bool _selected;
        bool _dragging;

        static int _hoverFrame = -1;
        static Entity _hovered;

        public static bool allRanges { get; set; }

        // Hover stays independent so the featured mode does not hide the range under the cursor
        public bool observeHover { get; set; } = true;

        // Old stage mode flag, selection now only comes through SetPreviewState
        public bool observePointer { get { return _observePointer; } set { _observePointer = value; } }

        public Camera previewCamera { get { return _camera; } set { _camera = value; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            allRanges = false;
            _hoverFrame = -1;
            _hovered = null;
        }

        public static Entity SampleHover(Ray ray, int frame)
        {
            if (_hoverFrame == frame)
            {
                return _hovered;
            }

            _hoverFrame = frame;
            _hovered = Physics.Raycast(ray, out RaycastHit hit) ? hit.collider.GetComponentInParent<Entity>() : null;
            return _hovered;
        }

        public void Init(Entity entity)
        {
            ClearZone();
            _selected = false;
            _dragging = false;
            _entity = entity;
        }

        void Start()
        {
            if (_entity == null)
            {
                Init(GetComponentInParent<Entity>());
            }
        }

        void Update()
        {
            Refresh();
        }

        public void SetPreviewState(bool selected, bool dragging)
        {
            _selected = selected;
            _dragging = dragging;
        }

        public void Refresh()
        {
            Camera camera = null;
            if (observeHover)
            {
                camera = _camera != null ? _camera : Camera.main;
            }

            bool hovered = camera != null
                           && SampleHover(camera.ScreenPointToRay(Input.mousePosition), Time.frameCount) == _entity;
            HLZoneRegistry current = HLZoneRegistry.current;
            if (_owner != current)
            {
                ClearZone();
            }

            bool isShown = _selected || _dragging || hovered || allRanges;
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled
                || _entity.entityType != Entity.EntityType.Player || !isShown
                || current == null || _entity.attributeManager == null
                || !_entity.attributeManager.Has(AttributeType.Range))
            {
                ClearZone();
                return;
            }

            float strength = allRanges ? 0.15f : 0.35f;
            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            if (_owner == null)
            {
                _owner = current;
            }

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
