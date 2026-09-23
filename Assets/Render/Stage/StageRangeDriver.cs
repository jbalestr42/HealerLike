using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // Tells each ally range preview whether it is hovered and whether every range shows.
    // Gameplay selection is private, so hover is the one pointer state the stage can read.
    public class StageRangeDriver : MonoBehaviour
    {
        readonly List<RangePreview> _previews = new List<RangePreview>();
        Camera _camera;
        bool _isInitialized = false;

        public bool showAll { get; set; }

        public Entity hovered { get; private set; }

        public IReadOnlyList<RangePreview> previews { get { return _previews; } }

        public void Init(Camera camera)
        {
            _camera = camera;
            _isInitialized = true;
        }

        public void Add(RangePreview preview)
        {
            if (preview != null && !_previews.Contains(preview))
            {
                _previews.Add(preview);
            }
        }

        public void Clear()
        {
            _previews.Clear();
            hovered = null;
        }

        // One raycast per frame for every preview
        void Update()
        {
            if (!_isInitialized || _camera == null)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            Entity hit = null;
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                hit = raycastHit.collider.GetComponentInParent<Entity>();
            }

            Apply(hit);
        }

        public void Apply(Entity hoveredEntity)
        {
            hovered = hoveredEntity;
            _previews.RemoveAll(preview => preview == null);
            foreach (RangePreview preview in _previews)
            {
                preview.Show(hoveredEntity != null && preview.entity == hoveredEntity, showAll);
            }
        }
    }
}
