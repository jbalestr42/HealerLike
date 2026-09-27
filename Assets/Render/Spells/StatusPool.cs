using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Spells
{
    // Equivalent authored layers share a visual; unlike families, clocks and whole-recipe overrides stay separate.
    public class StatusPool
    {
        class Status
        {
            public SpellEffect effect;
            public readonly Dictionary<ABuffHandlerFactory, int> sources = new Dictionary<ABuffHandlerFactory, int>();
            public float charges;
            public ARigHost host;
            public CreatureRig rig;
            public int revision;
            public Transform anchor;
            public EffectAnchors localAnchors;
        }
        readonly Dictionary<StatusKey, Status> _statuses = new Dictionary<StatusKey, Status>();
        readonly Dictionary<HandlerKey, List<StatusKey>> _handlers = new Dictionary<HandlerKey, List<StatusKey>>();
        readonly List<HandlerKey> _deadHandlers = new List<HandlerKey>();
        readonly List<StatusKey> _dead = new List<StatusKey>();
        Transform _parent;
        EffectVocabulary _vocabulary;
        SpellLooks _looks;
        PrimitiveMeshes _meshes;
        Material _material;
        Ground _ground;
        public int count => _statuses.Count;

        public void Init(Transform parent, EffectVocabulary vocabulary, SpellLooks looks, PrimitiveMeshes meshes,
            Material material, Ground ground = null)
        {
            _parent = parent; _vocabulary = vocabulary; _looks = looks;
            _meshes = meshes; _material = material; _ground = ground;
        }
        // Label lookup exists for legacy inspection; composition and ownership use entry identity and layer.
        public SpellEffect Get(GameObject target, EffectElement element)
        {
            foreach (var pair in _statuses)
                if (pair.Key.target == target && pair.Key.element == element) return pair.Value.effect;
            return null;
        }
        public SpellEffect GetShield(GameObject target)
        {
            foreach (var pair in _statuses)
                if (pair.Key.target == target && pair.Value.effect?.recipe.presentation?.isShield == true)
                    return pair.Value.effect;
            return null;
        }
        public SpellEffect Get(GameObject target, ABuffHandlerFactory factory)
        {
            if (_handlers.TryGetValue(new HandlerKey(target, factory), out var keys))
                foreach (StatusKey key in keys)
                    if (_statuses.TryGetValue(key, out Status status) && status.effect) return status.effect;
            return null;
        }
        public int Stacks(GameObject target, ABuffHandlerFactory factory)
        {
            if (_handlers.TryGetValue(new HandlerKey(target, factory), out var keys))
                foreach (StatusKey key in keys)
                    if (_statuses.TryGetValue(key, out Status status) && status.sources.TryGetValue(factory, out int stacks))
                        return stacks;
            return 0;
        }
        public void Set(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
            float elapsedSeconds, float durationSeconds)
        {
            if (target == null || factory == null) return;
            HandlerKey handler = new HandlerKey(target, factory);
            if (!_handlers.TryGetValue(handler, out var keys))
            {
                if (_vocabulary == null || _looks == null) return;
                keys = new List<StatusKey>();
                List<EffectRecipe> recipes = _looks.Compose(_vocabulary, factory, source, target);
                for (int i = 0; i < recipes.Count; i++)
                {
                    StatusKey key = new StatusKey(target, recipes[i], i, factory);
                    if (!_statuses.TryGetValue(key, out Status status) || !status.effect)
                        status = Open(key, recipes[i]);
                    if (status != null) keys.Add(key);
                }
                if (keys.Count == 0) return;
                _handlers.Add(handler, keys);
            }
            Entity caster = source ? source.GetComponent<Entity>() : null;
            foreach (StatusKey key in keys)
            {
                if (!_statuses.TryGetValue(key, out Status status) || !status.effect) continue;
                status.sources[factory] = stacks;
                Refresh(status, elapsedSeconds, durationSeconds);
                if (caster) status.effect.SetSide(caster.entityType);
            }
        }
        public void Remove(GameObject target, ABuffHandlerFactory factory)
        {
            HandlerKey handler = new HandlerKey(target, factory);
            if (!_handlers.TryGetValue(handler, out var keys)) return;
            _handlers.Remove(handler);
            foreach (StatusKey key in keys)
            {
                if (!_statuses.TryGetValue(key, out Status status)) continue;
                status.sources.Remove(factory);
                if (!Close(key, status) && status.effect)
                    Refresh(status, status.effect.elapsedSeconds, status.effect.durationSeconds);
            }
        }
        public void SetCharges(GameObject target, float charges)
        {
            EffectRecipe recipe = EffectComposer.Shield(_vocabulary, charges);
            if (recipe == null) return;
            StatusKey key = new StatusKey(target, recipe);
            _statuses.TryGetValue(key, out Status status);
            if (!float.IsFinite(charges) || charges <= 0)
            {
                if (status == null) return;
                status.charges = 0;
                if (!Close(key, status) && status.effect)
                    Refresh(status, status.effect.elapsedSeconds, status.effect.durationSeconds);
                return;
            }
            if (status == null || !status.effect) status = Open(key, recipe);
            if (status == null || status.charges == charges) return;
            status.charges = charges;
            Refresh(status, status.effect.elapsedSeconds, float.PositiveInfinity);
        }
        public void Tick()
        {
            _deadHandlers.Clear();
            foreach (var pair in _handlers)
            {
                bool lost = pair.Key.target == null || pair.Key.factory == null;
                foreach (StatusKey key in pair.Value)
                    lost |= !_statuses.TryGetValue(key, out Status status) || !status.effect;
                if (lost) _deadHandlers.Add(pair.Key);
            }
            foreach (HandlerKey key in _deadHandlers) Remove(key.target, key.factory);
            _dead.Clear();
            foreach (var pair in _statuses)
            {
                if (!pair.Key.target || !pair.Value.effect) _dead.Add(pair.Key);
                else Refit(pair.Key.target, pair.Value);
            }
            foreach (StatusKey key in _dead)
            {
                if (_statuses[key].effect) SpellEffect.Dispose(_statuses[key].effect.gameObject);
                _statuses.Remove(key);
            }
        }
        public void Clear()
        {
            foreach (Status status in _statuses.Values)
                if (status.effect) SpellEffect.Dispose(status.effect.gameObject);
            _statuses.Clear(); _handlers.Clear();
        }
        Status Open(StatusKey key, EffectRecipe recipe)
        {
            SpellEffect effect = SpellEffect.Create(recipe, _parent, _meshes, _material, key.target);
            if (!effect) return null;
            Status status = new Status { effect = effect };
            Refit(key.target, status, true);
            effect.BindGround(_ground, key.target);
            _statuses[key] = status;
            return status;
        }
        // A rig increments its revision when it recomposes; ordinary motion retains that revision.
        // Refit keeps the existing effect, stacks and clocks, and avoids sampling head overlap every frame.
        static void Refit(GameObject target, Status status, bool force = false)
        {
            Transform anchor = RenderTargets.Anchor(target);
            ARigHost host = status.host;
            if (!host)
            {
                Entity entity = target.GetComponent<Entity>();
                host = entity && entity.model ? entity.model.GetComponentInChildren<ARigHost>()
                    : target.GetComponentInChildren<ARigHost>();
            }
            CreatureRig rig = host ? host.rig : null;
            if (rig != null && !force && host == status.host && rig == status.rig
                && rig.revision == status.revision && anchor == status.anchor) return;

            EffectAnchors anchors = EffectPlacement.Anchors(target);
            EffectAnchors local = anchors;
            if (anchor)
            {
                local.bodyCentre = anchor.InverseTransformPoint(anchors.bodyCentre);
                local.headCentre = anchor.InverseTransformPoint(anchors.headCentre);
                local.neck = anchor.InverseTransformPoint(anchors.neck);
                local.foot = anchor.InverseTransformPoint(anchors.foot);
                float scale = Mathf.Max(.0001f, Mathf.Abs(anchor.lossyScale.x));
                local.bodyRadius /= scale;
                local.headRadius /= scale;
            }
            bool changed = force || rig != status.rig || anchor != status.anchor
                || (rig != null && rig.revision != status.revision) || !SameDimensions(local, status.localAnchors);
            status.host = host;
            status.rig = rig;
            status.revision = rig != null ? rig.revision : 0;
            status.anchor = anchor;
            status.localAnchors = local;
            if (changed) EffectPlacement.Place(status.effect, anchor, anchors);
        }

        static bool SameDimensions(EffectAnchors a, EffectAnchors b) =>
            Mathf.Abs(a.bodyRadius - b.bodyRadius) < .0001f && Mathf.Abs(a.headRadius - b.headRadius) < .0001f
            && (a.bodyCentre - b.bodyCentre).sqrMagnitude < .000001f
            && (a.headCentre - b.headCentre).sqrMagnitude < .000001f
            && (a.neck - b.neck).sqrMagnitude < .000001f && (a.foot - b.foot).sqrMagnitude < .000001f;

        static void Refresh(Status status, float elapsedSeconds, float durationSeconds)
        {
            int stacks = 0;
            foreach (int count in status.sources.Values) stacks += count;
            SpellEffect effect = status.effect;
            effect.SetCount(EffectComposer.Count(effect.recipe.entry, Mathf.Max(1, stacks), status.charges, 0));
            effect.SetStatus(Mathf.Max(1, stacks), elapsedSeconds, durationSeconds);
        }
        bool Close(StatusKey key, Status status)
        {
            if (status.sources.Count > 0 || status.charges > 0) return false;
            _statuses.Remove(key);
            if (status.effect)
            {
                status.effect.transform.SetParent(_parent, true);
                status.effect.BeginRemoval();
            }
            return true;
        }
    }
}
