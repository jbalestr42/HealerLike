using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public interface ICreaturePortraitCapture : IDisposable
    {
        Texture2D Capture(EntityData data, Entity.EntityType side);
    }

    // One scene owns this cache. Full cards and detail panels borrow the same screenshot.
    // No eviction destroys a texture while a visible element still holds it. Beyond the bounded
    // capacity, the UI keeps its ordinary fallback until the next invalidation or scene attachment.
    public sealed class CreaturePortraits : IToolkitIconProvider, IDisposable
    {
        public const int Capacity = 64;
        readonly Dictionary<(EntityData, Entity.EntityType), Texture2D> _images =
            new Dictionary<(EntityData, Entity.EntityType), Texture2D>();
        readonly ICreaturePortraitCapture _capture;
        readonly int _capacity;
        bool _disposed;

        public event Action Changed;
        public int cachedCount { get { return _images.Count; } }
        public int captureCount { get; private set; }
        public bool isDisposed { get { return _disposed; } }

        public CreaturePortraits(CreatureLooks looks, PrimitiveMeshes meshes)
            : this(new CreaturePortraitRenderer(looks, meshes)) { }

        public CreaturePortraits(ICreaturePortraitCapture capture, int capacity = Capacity)
        {
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _capacity = Mathf.Clamp(capacity, 1, Capacity);
        }

        public Texture2D GetCreatureIcon(EntityData data, Entity.EntityType side)
        {
            if (_disposed || data == null) return null;
            var key = (data, side);
            if (_images.TryGetValue(key, out Texture2D image)) return image;
            if (_images.Count >= _capacity) return null;
            try
            {
                captureCount++;
                image = _capture.Capture(data, side);
            }
            catch (Exception error)
            {
                // A failed request is cached too, so a missing GPU resource cannot retry every UI refresh.
                Debug.LogWarning("[CreaturePortraits] Could not capture " + data.name + ": " + error.Message);
            }
            _images.Add(key, image);
            return image;
        }

        public void Invalidate()
        {
            if (_disposed) return;
            Clear();
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
            _capture.Dispose();
            Changed?.Invoke();
            Changed = null;
        }

        void Clear()
        {
            foreach (Texture2D image in _images.Values) RenderObjects.Release(image);
            _images.Clear();
        }
    }
}
