using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // The sink's statuses: one element per target and element, its stacks summed over every handler that
    // resolved to it. An element whose last source has gone keeps a cosmetic tail under the sink and disposes
    // itself when the tail has played out.
    public class StatusPool
    {
        // Every handler that resolved to one element on one target, summed into that element
        class Status
        {
            public SpellEffect effect;
            public Dictionary<ABuffHandlerFactory, int> sources = new Dictionary<ABuffHandlerFactory, int>();
            public float charges;
        }

        readonly Dictionary<StatusKey, Status> _statuses = new Dictionary<StatusKey, Status>();
        // The element each handler fed on each target, so a handler's data is read once
        readonly Dictionary<HandlerKey, EffectElement> _elements = new Dictionary<HandlerKey, EffectElement>();
        readonly List<StatusKey> _dead = new List<StatusKey>();
        readonly List<HandlerKey> _deadSources = new List<HandlerKey>();
        Transform _parent;
        EffectVocabulary _vocabulary;
        SpellLooks _looks;
        PrimitiveMeshes _meshes;
        Material _material;

        public int count { get { return _statuses.Count; } }

        public void Init(Transform parent, EffectVocabulary vocabulary, SpellLooks looks, PrimitiveMeshes meshes,
                         Material material)
        {
            _parent = parent;
            _vocabulary = vocabulary;
            _looks = looks;
            _meshes = meshes;
            _material = material;
        }

        public SpellEffect Get(GameObject target, EffectElement element)
        {
            if (_statuses.TryGetValue(new StatusKey(target, element), out Status status) && status.effect != null)
            {
                return status.effect;
            }

            return null;
        }

        public SpellEffect Get(GameObject target, ABuffHandlerFactory factory)
        {
            if (!_elements.TryGetValue(new HandlerKey(target, factory), out EffectElement element))
            {
                return null;
            }

            return Get(target, element);
        }

        public int Stacks(GameObject target, ABuffHandlerFactory factory)
        {
            return _elements.TryGetValue(new HandlerKey(target, factory), out EffectElement element)
                && _statuses.TryGetValue(new StatusKey(target, element), out Status status)
                && status.sources.TryGetValue(factory, out int stacks) ? stacks : 0;
        }

        public void Set(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
                        float elapsedSeconds, float durationSeconds)
        {
            Status status = null;
            if (_elements.TryGetValue(new HandlerKey(target, factory), out EffectElement known))
            {
                _statuses.TryGetValue(new StatusKey(target, known), out status);
            }

            if (status == null || status.effect == null)
            {
                status = Open(source, target, factory);
                if (status == null)
                {
                    return;
                }
            }

            status.sources[factory] = stacks;
            Refresh(status, elapsedSeconds, durationSeconds);
            Entity caster = null;
            if (source != null)
            {
                caster = source.GetComponent<Entity>();
            }

            if (caster != null)
            {
                status.effect.SetSide(caster.entityType);
            }
        }

        public void Remove(GameObject target, ABuffHandlerFactory factory)
        {
            HandlerKey handler = new HandlerKey(target, factory);
            if (!_elements.TryGetValue(handler, out EffectElement element))
            {
                return;
            }

            _elements.Remove(handler);
            StatusKey key = new StatusKey(target, element);
            if (!_statuses.TryGetValue(key, out Status status))
            {
                return;
            }

            status.sources.Remove(factory);
            if (!Close(key, status) && status.effect != null)
            {
                Refresh(status, status.effect.elapsedSeconds, status.effect.durationSeconds);
            }
        }

        // HitArmor charges feed the plates of their target like one more source, the last charge takes them away
        public void SetCharges(GameObject target, float charges)
        {
            bool hasCharges = float.IsFinite(charges) && charges > 0f;
            StatusKey key = new StatusKey(target, EffectElement.Plates);
            _statuses.TryGetValue(key, out Status status);
            if (!hasCharges)
            {
                if (status != null)
                {
                    status.charges = 0f;
                    if (!Close(key, status) && status.effect != null)
                    {
                        Refresh(status, status.effect.elapsedSeconds, status.effect.durationSeconds);
                    }
                }

                return;
            }

            if (status == null || status.effect == null)
            {
                status = Open(key, EffectComposer.Shield(_vocabulary, charges));
                if (status == null)
                {
                    return;
                }
            }

            if (status.charges != charges)
            {
                status.charges = charges;
                Refresh(status, status.effect.elapsedSeconds, float.PositiveInfinity);
            }
        }

        // Releases what a handler, a target or an element that is gone left behind
        public void Tick()
        {
            _deadSources.Clear();
            foreach (KeyValuePair<HandlerKey, EffectElement> pair in _elements)
            {
                if (pair.Key.factory == null || pair.Key.target == null)
                {
                    _deadSources.Add(pair.Key);
                }
            }

            foreach (HandlerKey key in _deadSources)
            {
                // A handler that is gone takes its stacks with it, a target that is gone takes everything
                if (key.target != null)
                {
                    Remove(key.target, key.factory);
                }
                else
                {
                    _elements.Remove(key);
                }
            }

            _dead.Clear();
            foreach (KeyValuePair<StatusKey, Status> pair in _statuses)
            {
                if (pair.Key.target == null || pair.Value.effect == null)
                {
                    _dead.Add(pair.Key);
                }
            }

            foreach (StatusKey key in _dead)
            {
                SpellEffect effect = _statuses[key].effect;
                if (effect != null)
                {
                    SpellEffect.Dispose(effect.gameObject);
                }

                _statuses.Remove(key);
            }
        }

        public void Clear()
        {
            foreach (KeyValuePair<StatusKey, Status> pair in _statuses)
            {
                if (pair.Value.effect != null)
                {
                    SpellEffect.Dispose(pair.Value.effect.gameObject);
                }
            }

            _statuses.Clear();
            _elements.Clear();
        }

        // Reads the handler once, through its authored row or its derived look
        Status Open(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            if (_vocabulary == null || _looks == null)
            {
                return null;
            }

            SpellLook look = _looks.GetLook(factory, source, target);
            EffectRecipe recipe = EffectComposer.Compose(_vocabulary, look.element, look.family, look.tempo,
                                                         EffectDerivation.Period(factory), 1, 0f, 0f);
            if (recipe == null)
            {
                return null;
            }

            StatusKey key = new StatusKey(target, recipe.element);
            _elements[new HandlerKey(target, factory)] = recipe.element;
            if (_statuses.TryGetValue(key, out Status status) && status.effect != null)
            {
                return status;
            }

            return Open(key, recipe);
        }

        Status Open(StatusKey key, EffectRecipe recipe)
        {
            SpellEffect effect = SpellEffect.Create(recipe, _parent, _meshes, _material, key.target);
            if (effect == null)
            {
                return null;
            }

            // A status follows its unit, so it hangs under the target point
            EffectPlacement.Place(effect, RenderTargets.Anchor(key.target), EffectPlacement.Anchors(key.target));
            Status status = new Status();
            status.effect = effect;
            _statuses[key] = status;
            return status;
        }

        void Refresh(Status status, float elapsedSeconds, float durationSeconds)
        {
            int stacks = 0;
            foreach (KeyValuePair<ABuffHandlerFactory, int> source in status.sources)
            {
                stacks += source.Value;
            }

            SpellEffect effect = status.effect;
            effect.SetCount(EffectComposer.Count(effect.recipe.entry, Mathf.Max(1, stacks), status.charges, 0f));
            effect.SetStatus(Mathf.Max(1, stacks), elapsedSeconds, durationSeconds);
        }

        // The element leaves with its last source and keeps only its cosmetic tail
        bool Close(StatusKey key, Status status)
        {
            if (status.sources.Count > 0 || status.charges > 0f)
            {
                return false;
            }

            _statuses.Remove(key);
            if (status.effect != null)
            {
                status.effect.transform.SetParent(_parent, true);
                status.effect.BeginRemoval();
            }

            return true;
        }
    }
}
