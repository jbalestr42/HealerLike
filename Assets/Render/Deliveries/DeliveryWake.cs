using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Deliveries
{
    // A short world-space silhouette records direction without stretching when the caster moves.
    // It borrows the tip's material, owns only its renderer, and clears on every visibility boundary.
    public sealed class DeliveryWake : IDisposable
    {
        TrailRenderer _renderer;
        readonly MaterialPropertyBlock _paint = new MaterialPropertyBlock();
        Vector3 _lastPosition;
        bool _hasPosition;

        public void Draw(Transform parent, Material material, DeliveryPresentation look, Color colour, float width)
        {
            if (look == null || !look.IsValid() || look.trailSeconds <= 0f || look.trailWidth <= 0f)
            {
                Hide();
                return;
            }
            if (!_renderer)
            {
                GameObject go = new GameObject("DeliveryWake");
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
                _renderer = go.AddComponent<TrailRenderer>();
                _renderer.emitting = false;
                _renderer.autodestruct = false;
                _renderer.generateLightingData = true;
                _renderer.shadowCastingMode = ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
                _renderer.numCapVertices = 3;
                _renderer.numCornerVertices = 3;
                _renderer.minVertexDistance = 0.025f;
                _renderer.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            }
            Vector3 position = parent.position;
            if (!_hasPosition || Vector3.Distance(position, _lastPosition) > Mathf.Max(0.01f, look.trailBreakDistance))
                _renderer.Clear();
            _lastPosition = position;
            _hasPosition = true;
            _renderer.gameObject.SetActive(true);
            _renderer.sharedMaterial = material;
            _renderer.time = look.trailSeconds;
            _renderer.widthMultiplier = Mathf.Max(0f, width * look.trailWidth);
            _renderer.emitting = true;
            _paint.SetColor(RenderObjects.BaseColorId, colour);
            _renderer.SetPropertyBlock(_paint);
        }

        public void Hide()
        {
            _hasPosition = false;
            if (!_renderer) return;
            _renderer.emitting = false;
            _renderer.Clear();
            _renderer.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (_renderer) RenderObjects.Release(_renderer.gameObject);
            _renderer = null;
            _hasPosition = false;
        }
    }
}
