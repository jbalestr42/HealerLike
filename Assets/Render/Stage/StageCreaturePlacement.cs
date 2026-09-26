using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Watches the existing placement interaction after mouse/touch processing. Its model still owns position.
    [DefaultExecutionOrder(200)]
    public sealed class StageCreaturePlacement : MonoBehaviour
    {
        CreatureLooks _looks;
        PrimitiveMeshes _meshes;
        InteractionManager _interaction;
        Camera _camera;
        float _cellSize;
        AInteraction _selection;
        AInteraction _settleOnRefresh;
        CreaturePreview _preview;
        EntityData _data;
        GameObject _legacyModel;
        Renderer[] _legacyRenderers;
        bool[] _legacyEnabled;

        public CreaturePreview preview { get { return _preview; } }
        public EntityData data { get { return _data; } }
        public GameObject legacyModel { get { return _legacyModel; } }

        public void Init(CreatureLooks looks, PrimitiveMeshes meshes, InteractionManager interaction,
            Camera camera, float cellSize)
        {
            Clear();
            _looks = looks;
            _meshes = meshes;
            _interaction = interaction;
            _camera = camera;
            _cellSize = cellSize;
        }

        void LateUpdate()
        {
            Tick(_interaction != null ? _interaction.GetInteraction() : null, Time.unscaledTime, Time.unscaledDeltaTime);
        }

        // Explicit input lets native capture and unit tests observe the adapter without creating game managers.
        public void Tick(AInteraction active, float time, float deltaTime)
        {
            if (!EntityPlacementReadout.TryRead(active, out EntityData selectedData, out GameObject model,
                out Entity.EntityType side))
            {
                ClearVisual();
                _settleOnRefresh = null;
                return;
            }
            if (!ReferenceEquals(active, _selection) || model != _legacyModel)
            {
                ClearVisual();
                _selection = active;
                _legacyModel = model;
                _data = selectedData;
                CreaturePreview created = new CreaturePreview();
                if (!created.Init(_looks, _data, side, _meshes, transform, _cellSize))
                {
                    created.Dispose();
                    return;
                }
                _preview = created;
                if (ReferenceEquals(active, _settleOnRefresh)) _preview.CompleteAppearance();
                _settleOnRefresh = null;
                _legacyRenderers = model.GetComponentsInChildren<Renderer>(true);
                _legacyEnabled = new bool[_legacyRenderers.Length];
                for (int i = 0; i < _legacyRenderers.Length; i++)
                {
                    _legacyEnabled[i] = _legacyRenderers[i].enabled;
                    _legacyRenderers[i].enabled = false;
                }
            }
            if (_preview == null) return;
            foreach (Renderer renderer in _legacyRenderers)
                if (renderer) renderer.enabled = false;
            _preview.Tick(time, deltaTime,
                new FootFrame(model.transform.position, Vector3.up, _cellSize),
                _camera != null ? -_camera.transform.forward : (Vector3?)null);
        }

        public void Refresh(CreatureLooks looks, PrimitiveMeshes meshes)
        {
            _looks = looks;
            _meshes = meshes;
            Refresh();
        }

        public void Refresh()
        {
            _settleOnRefresh = _selection;
            ClearVisual();
        }

        public void Clear()
        {
            ClearVisual();
            _settleOnRefresh = null;
            _interaction = null;
        }

        void ClearVisual()
        {
            if (_legacyRenderers != null)
            {
                for (int i = 0; i < _legacyRenderers.Length; i++)
                    if (_legacyRenderers[i]) _legacyRenderers[i].enabled = _legacyEnabled[i];
            }
            if (_preview != null) _preview.Dispose();
            _preview = null;
            _selection = null;
            _data = null;
            _legacyModel = null;
            _legacyRenderers = null;
            _legacyEnabled = null;
        }

        void OnDisable() { ClearVisual(); }
        void OnDestroy() { Clear(); }
    }
}
