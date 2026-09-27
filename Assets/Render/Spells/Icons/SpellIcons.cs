using System;
using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // One interface attachment owns all captured textures. Failed requests are cached, never retried per frame.
    public class SpellIcons : IDisposable
    {
        public static readonly int Capacity = 128;
        readonly Dictionary<object, Texture2D> _images = new Dictionary<object, Texture2D>();
        readonly EffectVocabulary _vocabulary;
        readonly SpellLooks _looks;
        readonly ISpellIconCapture _capture;
        bool _disposed;
        public event Action Changed;
        public int cachedCount { get { return _images.Count; } }

        public SpellIcons(EffectVocabulary vocabulary, SpellLooks looks, PrimitiveMeshes meshes, Material material)
            : this(vocabulary, looks, new SpellIconRenderer(meshes, material))
        {
        }

        public SpellIcons(EffectVocabulary vocabulary, SpellLooks looks, ISpellIconCapture capture)
        {
            _vocabulary = vocabulary;
            _looks = looks;
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        }

        public Texture2D GetIcon(object source)
        {
            object key = SpellIconDerivation.Source(source);
            if (_disposed || key == null || key is UnityEngine.Object asset && !asset)
            {
                return null;
            }
            if (_images.TryGetValue(key, out Texture2D image))
            {
                return image;
            }
            if (_images.Count >= Capacity)
            {
                return null;
            }
            SpellIconRecipe recipe = SpellIconComposer.Compose(key, _vocabulary, _looks);
            if (recipe == null)
            {
                _images.Add(key, null);
                return null;
            }
            try
            {
                image = _capture.Capture(recipe);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[SpellIcons] Could not capture icon: " + error.Message);
            }
            _images.Add(key, image);
            return image;
        }

        public void Invalidate()
        {
            if (_disposed)
            {
                return;
            }
            Clear();
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            Clear();
            _capture.Dispose();
            Action changed = Changed;
            Changed = null;
            changed?.Invoke();
        }

        void Clear()
        {
            foreach (Texture2D image in _images.Values)
            {
                RenderObjects.Release(image);
            }
            _images.Clear();
        }
    }
}
