using System;
using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public sealed class HLSpellVisualSink : MonoBehaviour, IHLSpellVisualSink
    {
        public const float PulseSeconds = 0.8f;
        public HLSpellStyleTable styles;
        public Material material;
        // Injected adapter wins; when null, PulseArea falls back to HLRenderRegistry.Current.ZoneOwner (contract v2).
        public Action<Vector3, float, HLZoneKind, float> AreaPulse { get; set; }
        struct HLStatusVisual
        {
            public GameObject Root;
            public HLSpellEffect[] Effects;
            public AttributeManager Attributes;
        }
        readonly Dictionary<(GameObject, ABuffHandlerFactory), HLStatusVisual> _statuses = new Dictionary<(GameObject, ABuffHandlerFactory), HLStatusVisual>();
        readonly List<(GameObject,ABuffHandlerFactory)> _dead = new List<(GameObject,ABuffHandlerFactory)>();
        readonly List<HLSpellEffect> _remainingEffects = new List<HLSpellEffect>();
        public int PresentationVersion { get; private set; }
        readonly List<GameObject> _impacts = new List<GameObject>();
        // Optional bud adapter supplied by the creature track after merge.
        public Func<GameObject, Transform> HealerAnchor { get; set; }
        public Func<GameObject, bool> IsCharacterSource { get; set; }
        public Action<Vector3, Vector3> LinkObserved { get; set; }
        readonly HashSet<(GameObject source, GameObject target)> _heals = new HashSet<(GameObject, GameObject)>();
        readonly HashSet<HLSpellSignature> _unknown = new HashSet<HLSpellSignature>();
        readonly List<GameObject> _removing = new List<GameObject>();
        void Unknown(HLSpellSignature signature)
        { if (_unknown.Add(signature)) Debug.LogWarning("HL unmapped spell signature: " + signature); }
        readonly HLSpellGrammar _grammar = new HLSpellGrammar();
        public int StatusCount => _statuses.Count;
        public int ImpactCount { get { _impacts.RemoveAll(x => !x); return _impacts.Count; } }
        public GameObject GetStatus(GameObject target, ABuffHandlerFactory factory) => _statuses.TryGetValue((target,factory), out var status) ? status.Root : null;
        static Transform Anchor(GameObject target)
        { var entity = target.GetComponent<Entity>(); return entity && entity.targetPoint ? entity.targetPoint.transform : target.transform; }
        public void ShowImpact(GameObject source, GameObject target, HLResourceKind resource, float preClampAmount, bool isCritical)
        {
            if (!isActiveAndEnabled || !target || !HLSpellGrammar.Finite(preClampAmount) || preClampAmount == 0) return;
            var kind = resource == HLResourceKind.Mana ? HLSpellEffectKind.Mana : preClampAmount > 0 ? HLSpellEffectKind.Heal : HLSpellEffectKind.Impact;
            var prefab = styles ? kind == HLSpellEffectKind.Heal ? styles.heal : kind == HLSpellEffectKind.Impact ? styles.impact : null : null;
            var signature = new HLSpellSignature { operation = HLOperation.Resource, sign = HLSpellGrammar.Sign(preClampAmount), hasAttribute = true, attribute = resource == HLResourceKind.Health ? AttributeType.HealthMax : AttributeType.ManaMax, topology = HLTopology.Single, duration = HLDurationShape.Instant, tempo = HLTempo.Immediate };
            if (styles)
            {
                if (!styles.TryGet(signature, out var mapped)) { Unknown(signature); return; }
                prefab = mapped;
            }
            if (resource == HLResourceKind.Health && preClampAmount > 0 && source &&
                (IsCharacterSource != null ? IsCharacterSource(source) : source.GetComponent<Character>() != null)) _heals.Add((source,target));
            var effect = Spawn(prefab, kind, transform); effect.transform.position = Anchor(target).position;
            var owner = source ? source.GetComponent<Entity>() : null;
            effect.SetSide(owner ? owner.entityType : Entity.EntityType.None);
            var entity = target.GetComponent<Entity>(); float maximum = entity && entity.health ? entity.health.Max : 100;
            float radius = Mathf.Lerp(.10f,.26f,Mathf.Sqrt(Mathf.Clamp01(Mathf.Abs(preClampAmount)/Mathf.Max(maximum,1))));
            effect.transform.localScale = Vector3.one * (radius/.1f);
            if (isCritical) HLSpellPrimitives.AddCritical(effect);
            _impacts.Add(effect.gameObject);
            if (_impacts.Count > 128) { Dispose(_impacts[0]); _impacts.RemoveAt(0); }
        }
        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks, float elapsedSeconds, float durationSeconds, HLClockKind clock)
        {
            if (!isActiveAndEnabled || !target || !factory) return;
            if (stacks <= 0) { RemoveStatus(source,target,factory); return; }
            if (!HLSpellGrammar.Finite(elapsedSeconds) || float.IsNaN(durationSeconds)) return;
            var key = (target,factory);
            if (!_statuses.TryGetValue(key,out var status) || !status.Root)
            {
                var recipe = _grammar.Describe(factory,source,target);
                if (!recipe.IsValid) { Unknown(recipe.Signature); return; }
                var root = new GameObject("HLStatus"); root.transform.SetParent(Anchor(target),false);
                root.transform.localScale = Vector3.one * 1.35f;
                AddStatusAtoms(root.transform, recipe);
                var effects = root.GetComponentsInChildren<HLSpellEffect>();
                if (effects.Length == 0) { Dispose(root); return; }
                var entity = target.GetComponent<Entity>();
                status = new HLStatusVisual { Root = root, Effects = effects, Attributes = entity ? entity.attributeManager : null };
                _statuses[key]=status;
            }
            foreach(var effect in status.Effects)
            {
                effect.SetStatus(stacks,elapsedSeconds,durationSeconds,clock,effect.Signature);
                var attributes = status.Attributes;
                effect.SetShieldState(attributes && attributes.Has(AttributeType.HitArmor) ? attributes.Get(AttributeType.HitArmor).Value : null);
                var caster = source ? source.GetComponent<Entity>() : null;
                effect.SetSide(caster ? caster.entityType : Entity.EntityType.None);
            }
            RefreshBodyTint(target);
        }
        void RefreshBodyTint(GameObject target)
        {
            if (!target) return;
            Color tint = Color.white;
            foreach (var pair in _statuses)
                if (pair.Key.Item1 == target)
                    foreach (var effect in pair.Value.Effects)
                        if (effect && effect.Tint != Color.white) tint = effect.Tint;
            var state = target.GetComponent<HLBodyTintState>();
            if (!state && tint != Color.white) state = target.AddComponent<HLBodyTintState>();
            if (state) state.Set(tint);
        }
        void AddStatusAtoms(Transform root, HLVisualRecipe recipe)
        {
            if (!recipe.IsValid) { Unknown(recipe.Signature); return; }
            if (recipe.Children.Count > 0) { foreach(var child in recipe.Children) AddStatusAtoms(root,child); return; }
            var signature=recipe.Signature;
            GameObject prefab = null;
            if (styles && !styles.TryGet(signature, out prefab)) { Unknown(signature); return; }
            var effect=Spawn(prefab,HLSpellPrimitives.Kind(signature),root);
            effect.PeriodSeconds = recipe.PeriodSeconds;
            effect.SetStatus(1,0,recipe.DurationSeconds,recipe.Clock,signature);
            if(signature.hasAttribute && signature.attribute==AttributeType.Speed) effect.transform.localPosition=Vector3.down*.35f;
        }
        HLSpellEffect Spawn(GameObject prefab, HLSpellEffectKind kind, Transform parent)
        {
            var go=prefab ? Instantiate(prefab,parent,false) : new GameObject("HL"+kind);
            go.transform.SetParent(parent,false);
            var effect=go.GetComponent<HLSpellEffect>(); if(!effect) effect=go.AddComponent<HLSpellEffect>();
            effect.kind=kind; if(material) effect.material=material; effect.Initialize(); return effect;
        }
        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            if (ReferenceEquals(target,null)||ReferenceEquals(factory,null)) return;
            if(_statuses.Remove((target,factory),out var status) && status.Root)
            {
                var root = status.Root;
                root.transform.SetParent(transform,true);
                foreach (var effect in status.Effects) effect.BeginRemoval();
                _removing.Add(root);
                RefreshBodyTint(target);
            }
        }
        public void PulseArea(Vector3 center,float radius,HLZoneKind kind,float strength)
        {
            if(!HLZonePacker.TryCreate(center,radius,kind,strength,0,out var zone)) return;
            if (isActiveAndEnabled)
            {
                var ring = Spawn(null,kind == HLZoneKind.Hostile ? HLSpellEffectKind.Litter : HLSpellEffectKind.Area,transform);
                ring.lifetime = PulseSeconds;
                ring.transform.position = center; ring.transform.localScale = Vector3.one * radius;
                ring.SetSide(kind == HLZoneKind.Hostile ? Entity.EntityType.Computer : Entity.EntityType.Player);
                _impacts.Add(ring.gameObject);
                if (_impacts.Count > 128) { Dispose(_impacts[0]); _impacts.RemoveAt(0); }
            }
            // The zone owner has an independent lifetime; forwarding does not create sink children.
            if(AreaPulse!=null) AreaPulse(center,radius,kind,zone.strength);
            else HLRenderRegistry.Current?.ZoneOwner?.AddPulse(kind,center,radius,zone.strength,PulseSeconds);
        }
        public HLSpellEffect ShowLink(Vector3 start, Vector3 end)
        {
            if (!isActiveAndEnabled || !HLSpellGrammar.Finite(start.x) || !HLSpellGrammar.Finite(start.y) || !HLSpellGrammar.Finite(start.z) || !HLSpellGrammar.Finite(end.x) || !HLSpellGrammar.Finite(end.y) || !HLSpellGrammar.Finite(end.z)) return null;
            var effect=Spawn(styles ? styles.chain : null,HLSpellEffectKind.Chain,transform);effect.lifetime=.6f;effect.SetEndpoints(start,end);_impacts.Add(effect.gameObject);
            if (_impacts.Count > 128) { Dispose(_impacts[0]); _impacts.RemoveAt(0); }
            return effect;
        }
        /// <summary>Call only with the previous and current confirmed lightning contacts.</summary>
        public HLSpellEffect ShowContactLink(Vector3 previousContact, Vector3 contact)
        {
            var effect = ShowLink(previousContact,contact);
            if (effect) { effect.ContactThread = true; effect.SetEndpoints(previousContact,contact); }
            return effect;
        }
        public void FlushHealLinks()
        {
            if (!isActiveAndEnabled) { _heals.Clear(); return; }
            foreach (var pair in _heals)
            {
                if (!pair.source || !pair.target) continue;
                var anchor = HealerAnchor?.Invoke(pair.source);
                if (!anchor) anchor = pair.source.GetComponentInChildren<HealerLike.Render.Creatures.HLCharacterView>()?.bud0;
                var start = anchor ? anchor.position : pair.source.transform.position;
                var end = Anchor(pair.target).position;
                ShowLink(start,end); LinkObserved?.Invoke(start,end);
            }
            _heals.Clear();
        }
        void LateUpdate()
        {
            FlushHealLinks();
            for (int i = _removing.Count-1; i >= 0; i--)
            {
                _remainingEffects.Clear();
                if (_removing[i]) _removing[i].GetComponentsInChildren(false,_remainingEffects);
                if (_remainingEffects.Count == 0) { Dispose(_removing[i]); _removing.RemoveAt(i); }
            }
            _dead.Clear();
            foreach(var pair in _statuses)
            {
                if(!pair.Key.Item1||!pair.Key.Item2||!pair.Value.Root) { _dead.Add(pair.Key); continue; }
                var attributes = pair.Value.Attributes;
                foreach (var effect in pair.Value.Effects)
                    effect.SetShieldState(attributes && attributes.Has(AttributeType.HitArmor) ? attributes.Get(AttributeType.HitArmor).Value : null);
            }
            if (_dead.Count > 0) PresentationVersion++;
            foreach(var key in _dead) { Dispose(_statuses[key].Root);_statuses.Remove(key); }
            _impacts.RemoveAll(x=>!x);
        }
        bool _ownsPrimitives;
        void RetainPrimitives()
        {
            if (_ownsPrimitives) return;
            HLSpellPrimitives.Retain(); _ownsPrimitives = true;
        }
        void OnEnable()
        {
            RetainPrimitives();
            // The registry may retain this sink across disable/enable.
            Clear();
        }
        void OnDisable() => Clear();
        void OnDestroy()
        {
            Clear();
            if (_ownsPrimitives) { _ownsPrimitives = false; HLSpellPrimitives.ReleaseUser(); }
        }
        public void Clear()
        {
            PresentationVersion++;
            _heals.Clear(); foreach(var root in _removing) Dispose(root); _removing.Clear();
            foreach(var pair in _statuses) { var state = pair.Key.Item1 ? pair.Key.Item1.GetComponent<HLBodyTintState>() : null; if (state) state.Set(Color.white); Dispose(pair.Value.Root); } _statuses.Clear();
            foreach(var root in _impacts) Dispose(root);_impacts.Clear();
        }
        static void Dispose(GameObject go)
        {
            if(!go)return;
            go.SetActive(false);
            if(Application.isPlaying) Destroy(go);
            else
            {
                foreach(var effect in go.GetComponentsInChildren<HLSpellEffect>(true)) effect.ReleaseResources();
                DestroyImmediate(go);
            }
        }
    }
}
