using System;
using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellVisualSink : MonoBehaviour, IHLSpellVisualSink
    {
        public static readonly float PulseSeconds = 0.8f;

        struct HLStatusVisual
        {
            public GameObject root;
            public HLSpellEffect[] effects;
            public AttributeManager attributes;
        }

        public HLSpellStyleTable styles;
        public Material material;

        readonly HLSpellGrammar _grammar = new HLSpellGrammar();
        readonly Dictionary<(GameObject, ABuffHandlerFactory), HLStatusVisual> _statuses =
            new Dictionary<(GameObject, ABuffHandlerFactory), HLStatusVisual>();
        readonly List<(GameObject, ABuffHandlerFactory)> _dead = new List<(GameObject, ABuffHandlerFactory)>();
        readonly List<HLSpellEffect> _remainingEffects = new List<HLSpellEffect>();
        readonly List<GameObject> _impacts = new List<GameObject>();
        readonly HashSet<(GameObject source, GameObject target)> _heals = new HashSet<(GameObject, GameObject)>();
        readonly HashSet<HLSpellSignature> _unknown = new HashSet<HLSpellSignature>();
        readonly List<GameObject> _removing = new List<GameObject>();
        bool _ownsPrimitives;

        // Injected adapter wins; when null, PulseArea falls back to HLRenderRegistry.Current.ZoneOwner.
        public Action<Vector3, float, HLZoneKind, float> areaPulse { get; set; }

        // Optional bud adapter supplied by the creature view.
        public Func<GameObject, Transform> healerAnchor { get; set; }

        public Func<GameObject, bool> isCharacterSource { get; set; }

        public Action<Vector3, Vector3> linkObserved { get; set; }

        int _presentationVersion;
        public int presentationVersion { get { return _presentationVersion; } }

        public int statusCount { get { return _statuses.Count; } }

        public int impactCount
        {
            get
            {
                _impacts.RemoveAll(x => !x);
                return _impacts.Count;
            }
        }

        void OnEnable()
        {
            RetainPrimitives();
            // The registry may retain this sink across disable/enable.
            Clear();
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
            if (_ownsPrimitives)
            {
                _ownsPrimitives = false;
                HLSpellPrimitives.ReleaseUser();
            }
        }

        void LateUpdate()
        {
            FlushHealLinks();
            for (int i = _removing.Count - 1; i >= 0; i--)
            {
                _remainingEffects.Clear();
                if (_removing[i])
                {
                    _removing[i].GetComponentsInChildren(false, _remainingEffects);
                }
                if (_remainingEffects.Count == 0)
                {
                    Dispose(_removing[i]);
                    _removing.RemoveAt(i);
                }
            }
            _dead.Clear();
            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), HLStatusVisual> pair in _statuses)
            {
                if (!pair.Key.Item1 || !pair.Key.Item2 || !pair.Value.root)
                {
                    _dead.Add(pair.Key);
                    continue;
                }
                AttributeManager attributes = pair.Value.attributes;
                foreach (HLSpellEffect effect in pair.Value.effects)
                {
                    effect.SetShieldState(
                        attributes && attributes.Has(AttributeType.HitArmor)
                            ? attributes.Get(AttributeType.HitArmor).Value
                            : null
                    );
                }
            }
            if (_dead.Count > 0)
            {
                _presentationVersion++;
            }
            foreach ((GameObject, ABuffHandlerFactory) key in _dead)
            {
                Dispose(_statuses[key].root);
                _statuses.Remove(key);
            }
            _impacts.RemoveAll(x => !x);
        }

        public GameObject GetStatus(GameObject target, ABuffHandlerFactory factory)
        {
            return _statuses.TryGetValue((target, factory), out HLStatusVisual status) ? status.root : null;
        }

        public void ShowImpact(
            GameObject source,
            GameObject target,
            HLResourceKind resource,
            float preClampAmount,
            bool isCritical
        )
        {
            if (!isActiveAndEnabled || !target || !HLSpellGrammar.Finite(preClampAmount) || preClampAmount == 0f)
            {
                return;
            }
            HLSpellEffectKind kind = HLSpellEffectKind.Impact;
            if (resource == HLResourceKind.Mana)
            {
                kind = HLSpellEffectKind.Mana;
            }
            else if (preClampAmount > 0f)
            {
                kind = HLSpellEffectKind.Heal;
            }
            GameObject prefab = null;
            if (styles && kind == HLSpellEffectKind.Heal)
            {
                prefab = styles.heal;
            }
            else if (styles && kind == HLSpellEffectKind.Impact)
            {
                prefab = styles.impact;
            }
            HLSpellSignature signature = new HLSpellSignature
            {
                operation = HLOperation.Resource,
                sign = HLSpellGrammar.Sign(preClampAmount),
                hasAttribute = true,
                attribute = resource == HLResourceKind.Health ? AttributeType.HealthMax : AttributeType.ManaMax,
                topology = HLTopology.Single,
                duration = HLDurationShape.Instant,
                tempo = HLTempo.Immediate
            };
            if (styles)
            {
                if (!styles.TryGet(signature, out GameObject mapped))
                {
                    Unknown(signature);
                    return;
                }
                prefab = mapped;
            }
            if (
                resource == HLResourceKind.Health
                && preClampAmount > 0f
                && source
                && IsCharacter(source)
            )
            {
                _heals.Add((source, target));
            }
            HLSpellEffect effect = Spawn(prefab, kind, transform);
            effect.transform.position = Anchor(target).position;
            Entity owner = source ? source.GetComponent<Entity>() : null;
            effect.SetSide(owner ? owner.entityType : Entity.EntityType.None);
            Entity entity = target.GetComponent<Entity>();
            float maximum = entity && entity.health ? entity.health.Max : 100f;
            float radius = Mathf.Lerp(
                0.10f,
                0.26f,
                Mathf.Sqrt(Mathf.Clamp01(Mathf.Abs(preClampAmount) / Mathf.Max(maximum, 1f)))
            );
            effect.transform.localScale = Vector3.one * (radius / 0.1f);
            if (isCritical)
            {
                HLSpellPrimitives.AddCritical(effect);
            }
            _impacts.Add(effect.gameObject);
            if (_impacts.Count > 128)
            {
                Dispose(_impacts[0]);
                _impacts.RemoveAt(0);
            }
        }

        public void SetStatus(
            GameObject source,
            GameObject target,
            ABuffHandlerFactory factory,
            int stacks,
            float elapsedSeconds,
            float durationSeconds,
            HLClockKind clock
        )
        {
            if (!isActiveAndEnabled || !target || !factory)
            {
                return;
            }
            if (stacks <= 0)
            {
                RemoveStatus(source, target, factory);
                return;
            }
            if (!HLSpellGrammar.Finite(elapsedSeconds) || float.IsNaN(durationSeconds))
            {
                return;
            }
            (GameObject, ABuffHandlerFactory) key = (target, factory);
            if (!_statuses.TryGetValue(key, out HLStatusVisual status) || !status.root)
            {
                HLVisualRecipe recipe = _grammar.Describe(factory, source, target);
                if (!recipe.isValid)
                {
                    Unknown(recipe.signature);
                    return;
                }
                GameObject root = new GameObject("HLStatus");
                root.transform.SetParent(Anchor(target), false);
                root.transform.localScale = Vector3.one * 1.35f;
                AddStatusAtoms(root.transform, recipe);
                HLSpellEffect[] effects = root.GetComponentsInChildren<HLSpellEffect>();
                if (effects.Length == 0)
                {
                    Dispose(root);
                    return;
                }
                Entity entity = target.GetComponent<Entity>();
                status = new HLStatusVisual
                {
                    root = root,
                    effects = effects,
                    attributes = entity ? entity.attributeManager : null
                };
                _statuses[key] = status;
            }
            foreach (HLSpellEffect effect in status.effects)
            {
                effect.SetStatus(stacks, elapsedSeconds, durationSeconds, clock, effect.signature);
                AttributeManager attributes = status.attributes;
                effect.SetShieldState(
                    attributes && attributes.Has(AttributeType.HitArmor)
                        ? attributes.Get(AttributeType.HitArmor).Value
                        : null
                );
                Entity caster = source ? source.GetComponent<Entity>() : null;
                effect.SetSide(caster ? caster.entityType : Entity.EntityType.None);
            }
            RefreshBodyTint(target);
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            if (ReferenceEquals(target, null) || ReferenceEquals(factory, null))
            {
                return;
            }
            if (_statuses.Remove((target, factory), out HLStatusVisual status) && status.root)
            {
                GameObject root = status.root;
                root.transform.SetParent(transform, true);
                foreach (HLSpellEffect effect in status.effects)
                {
                    effect.BeginRemoval();
                }
                _removing.Add(root);
                RefreshBodyTint(target);
            }
        }

        public void PulseArea(Vector3 center, float radius, HLZoneKind kind, float strength)
        {
            if (!HLZonePacker.TryCreate(center, radius, kind, strength, 0f, out HLZone zone))
            {
                return;
            }
            if (isActiveAndEnabled)
            {
                HLSpellEffect ring = Spawn(
                    null,
                    kind == HLZoneKind.Hostile ? HLSpellEffectKind.Litter : HLSpellEffectKind.Area,
                    transform
                );
                ring.lifetime = PulseSeconds;
                ring.transform.position = center;
                ring.transform.localScale = Vector3.one * radius;
                ring.SetSide(kind == HLZoneKind.Hostile ? Entity.EntityType.Computer : Entity.EntityType.Player);
                _impacts.Add(ring.gameObject);
                if (_impacts.Count > 128)
                {
                    Dispose(_impacts[0]);
                    _impacts.RemoveAt(0);
                }
            }
            // The zone owner has an independent lifetime; forwarding does not create sink children.
            if (areaPulse != null)
            {
                areaPulse(center, radius, kind, zone.strength);
            }
            else
            {
                HLRenderRegistry.Current?.ZoneOwner?.AddPulse(kind, center, radius, zone.strength, PulseSeconds);
            }
        }

        public HLSpellEffect ShowLink(Vector3 start, Vector3 end)
        {
            if (
                !isActiveAndEnabled
                || !HLSpellGrammar.Finite(start.x)
                || !HLSpellGrammar.Finite(start.y)
                || !HLSpellGrammar.Finite(start.z)
                || !HLSpellGrammar.Finite(end.x)
                || !HLSpellGrammar.Finite(end.y)
                || !HLSpellGrammar.Finite(end.z)
            )
            {
                return null;
            }
            HLSpellEffect effect = Spawn(styles ? styles.chain : null, HLSpellEffectKind.Chain, transform);
            effect.lifetime = 0.6f;
            effect.SetEndpoints(start, end);
            _impacts.Add(effect.gameObject);
            if (_impacts.Count > 128)
            {
                Dispose(_impacts[0]);
                _impacts.RemoveAt(0);
            }
            return effect;
        }

        public HLSpellEffect ShowContactLink(Vector3 previousContact, Vector3 contact)
        {
            HLSpellEffect effect = ShowLink(previousContact, contact);
            if (effect)
            {
                effect.contactThread = true;
                effect.SetEndpoints(previousContact, contact);
            }
            return effect;
        }

        public void FlushHealLinks()
        {
            if (!isActiveAndEnabled)
            {
                _heals.Clear();
                return;
            }
            foreach ((GameObject source, GameObject target) pair in _heals)
            {
                if (!pair.source || !pair.target)
                {
                    continue;
                }
                Transform anchor = healerAnchor?.Invoke(pair.source);
                if (!anchor)
                {
                    anchor = pair.source.GetComponentInChildren<HealerLike.Render.Creatures.HLCharacterView>()?.Bud0;
                }
                Vector3 start = anchor ? anchor.position : pair.source.transform.position;
                Vector3 end = Anchor(pair.target).position;
                ShowLink(start, end);
                linkObserved?.Invoke(start, end);
            }
            _heals.Clear();
        }

        public void Clear()
        {
            _presentationVersion++;
            _heals.Clear();
            foreach (GameObject root in _removing)
            {
                Dispose(root);
            }
            _removing.Clear();
            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), HLStatusVisual> pair in _statuses)
            {
                HLBodyTintState state = pair.Key.Item1 ? pair.Key.Item1.GetComponent<HLBodyTintState>() : null;
                if (state)
                {
                    state.Set(Color.white);
                }
                Dispose(pair.Value.root);
            }
            _statuses.Clear();
            foreach (GameObject root in _impacts)
            {
                Dispose(root);
            }
            _impacts.Clear();
        }

        void RefreshBodyTint(GameObject target)
        {
            if (!target)
            {
                return;
            }
            Color tint = Color.white;
            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), HLStatusVisual> pair in _statuses)
            {
                if (pair.Key.Item1 == target)
                {
                    foreach (HLSpellEffect effect in pair.Value.effects)
                    {
                        if (effect && effect.tint != Color.white)
                        {
                            tint = effect.tint;
                        }
                    }
                }
            }
            HLBodyTintState state = target.GetComponent<HLBodyTintState>();
            if (!state && tint != Color.white)
            {
                state = target.AddComponent<HLBodyTintState>();
            }
            if (state)
            {
                state.Set(tint);
            }
        }

        void AddStatusAtoms(Transform root, HLVisualRecipe recipe)
        {
            if (!recipe.isValid)
            {
                Unknown(recipe.signature);
                return;
            }
            if (recipe.children.Count > 0)
            {
                foreach (HLVisualRecipe child in recipe.children)
                {
                    AddStatusAtoms(root, child);
                }
                return;
            }
            HLSpellSignature signature = recipe.signature;
            GameObject prefab = null;
            if (styles && !styles.TryGet(signature, out prefab))
            {
                Unknown(signature);
                return;
            }
            HLSpellEffect effect = Spawn(prefab, HLSpellPrimitives.Kind(signature), root);
            effect.periodSeconds = recipe.periodSeconds;
            effect.SetStatus(1, 0f, recipe.durationSeconds, recipe.clock, signature);
            if (signature.hasAttribute && signature.attribute == AttributeType.Speed)
            {
                effect.transform.localPosition = Vector3.down * 0.35f;
            }
        }

        HLSpellEffect Spawn(GameObject prefab, HLSpellEffectKind kind, Transform parent)
        {
            GameObject go = prefab ? Instantiate(prefab, parent, false) : new GameObject("HL" + kind);
            go.transform.SetParent(parent, false);
            HLSpellEffect effect = go.GetComponent<HLSpellEffect>();
            if (!effect)
            {
                effect = go.AddComponent<HLSpellEffect>();
            }
            effect.kind = kind;
            if (material)
            {
                effect.material = material;
            }
            effect.Initialize();
            return effect;
        }

        void RetainPrimitives()
        {
            if (_ownsPrimitives)
            {
                return;
            }
            HLSpellPrimitives.Retain();
            _ownsPrimitives = true;
        }

        void Unknown(HLSpellSignature signature)
        {
            if (_unknown.Add(signature))
            {
                Debug.LogWarning("HL unmapped spell signature: " + signature);
            }
        }

        bool IsCharacter(GameObject source)
        {
            if (isCharacterSource != null)
            {
                return isCharacterSource(source);
            }
            return source.GetComponent<Character>() != null;
        }

        static Transform Anchor(GameObject target)
        {
            Entity entity = target.GetComponent<Entity>();
            return entity && entity.targetPoint ? entity.targetPoint.transform : target.transform;
        }

        static void Dispose(GameObject go)
        {
            if (!go)
            {
                return;
            }
            go.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                foreach (HLSpellEffect effect in go.GetComponentsInChildren<HLSpellEffect>(true))
                {
                    effect.ReleaseResources();
                }
                DestroyImmediate(go);
            }
        }
    }
}
