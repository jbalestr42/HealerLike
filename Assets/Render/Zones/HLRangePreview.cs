using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Range preview of an ally, the range driver tells it whether it is hovered and whether every range shows
    public class HLRangePreview : MonoBehaviour, IVisualBehaviour, IEntityView
    {
        [SerializeField] bool _observePointer = true;
        [SerializeField] Camera _camera;
        Entity _entity;
        HLZoneRegistry _zones;
        HLZoneRegistry _owner;
        int _handle;
        bool _selected;
        bool _dragging;
        bool _isHovered;
        bool _showAll;
        bool _isShownByDriver = false;
        bool _isInitialized = false;

        // Shared hover and show all state for the previews the driver does not call yet, removed in D2
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

        public void Init(Entity entity, HLZoneRegistry zones)
        {
            ClearZone();
            _zones = zones;
            _isInitialized = true;
            _selected = false;
            _dragging = false;
            _entity = entity;
        }

        public Entity entity { get { return _entity; } }

        public void Init(Entity entity, RenderManager manager)
        {
            Init(entity, manager.zones);
        }

        // Called by EntityModel.Init on the staged model copies, removed in D2
        public void Init(Entity entity)
        {
            ClearZone();
            _selected = false;
            _dragging = false;
            _entity = entity;
        }

        void Start()
        {
            // Staged model copies only, removed in D2
            if (_entity == null)
            {
                Init(GetComponentInParent<Entity>());
            }
        }

        void Update()
        {
            Refresh();
        }

        // Called by HLStageRangeDriver, which owns the hover raycast and the show all toggle
        public void Show(bool isHovered, bool showAll)
        {
            _isShownByDriver = true;
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
            bool isHovered = _isHovered;
            bool showAll = _showAll;
            if (!_isShownByDriver)
            {
                // Samples the shared statics itself until the driver calls Show, removed in D2
                isHovered = SampleOwnHover();
                showAll = allRanges;
            }

            // Falls back to the static registry until the RenderManager calls Init, removed in D2
            HLZoneRegistry zones = _isInitialized ? _zones : HLZoneRegistry.current;
            if (_owner != zones)
            {
                ClearZone();
            }

            bool isShown = _selected || _dragging || isHovered || showAll;
            if (!isActiveAndEnabled || _entity == null || !_entity.isActiveAndEnabled
                || _entity.entityType != Entity.EntityType.Player || !isShown
                || zones == null || _entity.attributeManager == null
                || !_entity.attributeManager.Has(AttributeType.Range))
            {
                ClearZone();
                return;
            }

            float strength = showAll ? 0.15f : 0.35f;
            float radius = _entity.attributeManager.Get(AttributeType.Range).Value;
            _owner = zones;
            if (!_owner.Contains(_handle))
            {
                _handle = _owner.Add(HLZoneKind.Range, _entity.transform.position, radius, strength);
            }
            else
            {
                _owner.RefreshZone(_handle, HLZoneKind.Range, _entity.transform.position, radius, strength);
            }
        }

        bool SampleOwnHover()
        {
            if (!observeHover)
            {
                return false;
            }

            Camera camera = _camera != null ? _camera : Camera.main;
            if (camera == null)
            {
                return false;
            }

            return SampleHover(camera.ScreenPointToRay(Input.mousePosition), Time.frameCount) == _entity;
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
