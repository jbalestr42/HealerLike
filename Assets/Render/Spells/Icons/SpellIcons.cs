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
        // Keyed by the source and the class that sizes it: the same skill cast by another class is another image
        readonly Dictionary<(object source, CharacterData owner), Texture2D> _images =
            new Dictionary<(object source, CharacterData owner), Texture2D>();
        readonly EffectVocabulary _vocabulary;
        readonly SpellLooks _looks;
        readonly ISpellIconCapture _capture;
        readonly Func<object, CharacterData> _ownerOf;
        bool _disposed;
        public event Action Changed;
        public int cachedCount { get { return _images.Count; } }

        // ownerOf names the class each source is sized by (the live caster's during a run); without it every
        // source is read with no class
        public SpellIcons(EffectVocabulary vocabulary, SpellLooks looks, PrimitiveMeshes meshes, Material material,
            Func<object, CharacterData> ownerOf = null)
            : this(vocabulary, looks, new SpellIconRenderer(meshes, material), ownerOf)
        {
        }

        public SpellIcons(EffectVocabulary vocabulary, SpellLooks looks, ISpellIconCapture capture,
            Func<object, CharacterData> ownerOf = null)
        {
            _vocabulary = vocabulary;
            _looks = looks;
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _ownerOf = ownerOf;
        }

        public Texture2D GetIcon(object source)
        {
            object key = SpellIconDerivation.Source(source);
            if (_disposed || key == null || key is UnityEngine.Object asset && !asset)
            {
                return null;
            }
            CharacterData owner = _ownerOf?.Invoke(key);
            (object, CharacterData) entry = (key, owner ? owner : null);
            if (_images.TryGetValue(entry, out Texture2D image))
            {
                return image;
            }
            if (_images.Count >= Capacity)
            {
                return null;
            }
            SpellIconRecipe recipe = SpellIconComposer.Compose(key, _vocabulary, _looks, entry.Item2);
            if (recipe == null)
            {
                _images.Add(entry, null);
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
            _images.Add(entry, image);
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
