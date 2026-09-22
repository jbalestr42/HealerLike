using System;
using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public sealed class HLSpellVisualSink : MonoBehaviour, IHLSpellVisualSink
    {
        public HLSpellStyleTable styles;
        public Material material;
        // Frozen HLRenderRegistry has no zone slot. Stage injects the zone owner's AddPulse adapter here.
        public Action<Vector3, float, HLZoneKind, float> AreaPulse { get; set; }
        readonly Dictionary<(GameObject, ABuffHandlerFactory), GameObject> _statuses = new Dictionary<(GameObject, ABuffHandlerFactory), GameObject>();
        readonly List<GameObject> _impacts = new List<GameObject>();
        readonly HLSpellGrammar _grammar = new HLSpellGrammar();
        public int StatusCount => _statuses.Count;
        public int ImpactCount { get { _impacts.RemoveAll(x => !x); return _impacts.Count; } }
        public GameObject GetStatus(GameObject target, ABuffHandlerFactory factory) => _statuses.TryGetValue((target,factory), out var go) ? go : null;
        static Transform Anchor(GameObject target)
        { var entity = target.GetComponent<Entity>(); return entity && entity.targetPoint ? entity.targetPoint.transform : target.transform; }
        public void ShowImpact(GameObject source, GameObject target, HLResourceKind resource, float preClampAmount, bool isCritical)
        {
            if (!target || !HLSpellGrammar.Finite(preClampAmount) || preClampAmount == 0) return;
            var kind = resource == HLResourceKind.Mana ? HLSpellEffectKind.Mana : preClampAmount > 0 ? HLSpellEffectKind.Heal : HLSpellEffectKind.Impact;
            var prefab = styles ? kind == HLSpellEffectKind.Heal ? styles.heal : kind == HLSpellEffectKind.Impact ? styles.impact : null : null;
            var signature = new HLSpellSignature { operation = HLOperation.Resource, sign = HLSpellGrammar.Sign(preClampAmount), hasAttribute = true, attribute = resource == HLResourceKind.Health ? AttributeType.HealthMax : AttributeType.ManaMax, topology = HLTopology.Single, duration = HLDurationShape.Instant, tempo = HLTempo.Immediate };
            if (styles && styles.TryGet(signature, out var mapped)) prefab = mapped;
            var effect = Spawn(prefab, kind, transform); effect.transform.position = Anchor(target).position;
            var owner = source ? source.GetComponent<Entity>() : null;
            effect.SetSide(owner ? owner.entityType : Entity.EntityType.None);
            var entity = target.GetComponent<Entity>(); float maximum = entity && entity.health ? entity.health.Max : 100;
            float radius = Mathf.Lerp(.035f,.18f,Mathf.Sqrt(Mathf.Clamp01(Mathf.Abs(preClampAmount)/Mathf.Max(maximum,1))));
            effect.transform.localScale = Vector3.one * (radius/.1f);
            if (isCritical) HLSpellPrimitives.AddCritical(effect);
            _impacts.Add(effect.gameObject);
            if (_impacts.Count > 128) { Dispose(_impacts[0]); _impacts.RemoveAt(0); }
        }
        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks, float elapsedSeconds, float durationSeconds, HLClockKind clock)
        {
            if (!target || !factory) return;
            if (stacks <= 0) { RemoveStatus(source,target,factory); return; }
            if (!HLSpellGrammar.Finite(elapsedSeconds) || float.IsNaN(durationSeconds)) return;
            var key = (target,factory);
            if (!_statuses.TryGetValue(key,out var root) || !root)
            {
                var recipe = _grammar.Describe(factory,source,target);
                if (!recipe.IsValid) return;
                root = new GameObject("HLStatus"); root.transform.SetParent(Anchor(target),false);
                AddStatusAtoms(root.transform, recipe);
                _statuses[key]=root;
            }
            foreach(var effect in root.GetComponentsInChildren<HLSpellEffect>())
            {
                effect.SetStatus(stacks,elapsedSeconds,durationSeconds,clock,effect.Signature);
                var entity = target.GetComponent<Entity>();
                var attributes = entity ? entity.attributeManager : null;
                effect.SetShieldState(attributes && attributes.Has(AttributeType.HitArmor) ? attributes.Get(AttributeType.HitArmor).Value : null);
                effect.SetSide(entity ? entity.entityType : Entity.EntityType.None);
            }
        }
        void AddStatusAtoms(Transform root, HLVisualRecipe recipe)
        {
            if (!recipe.IsValid) return;
            if (recipe.Children.Count > 0) { foreach(var child in recipe.Children) AddStatusAtoms(root,child); return; }
            var signature=recipe.Signature;
            bool shield=signature.operation==HLOperation.Prevention || signature.operation==HLOperation.Attribute && (signature.attribute==AttributeType.HitArmor || signature.attribute==AttributeType.PercentArmor);
            var effect=Spawn(styles ? styles.StatusPrefab(signature) : null,shield ? HLSpellEffectKind.Shield : HLSpellEffectKind.Buff,root);
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
            if(_statuses.Remove((target,factory),out var root)) Dispose(root);
        }
        public void PulseArea(Vector3 center,float radius,HLZoneKind kind,float strength)
        {
            if(HLZonePacker.TryCreate(center,radius,kind,strength,0,out var zone)) AreaPulse?.Invoke(center,radius,kind,zone.strength);
        }
        public HLSpellEffect ShowLink(Vector3 start, Vector3 end)
        {
            if (!HLSpellGrammar.Finite(start.x) || !HLSpellGrammar.Finite(start.y) || !HLSpellGrammar.Finite(start.z) || !HLSpellGrammar.Finite(end.x) || !HLSpellGrammar.Finite(end.y) || !HLSpellGrammar.Finite(end.z)) return null;
            var effect=Spawn(styles ? styles.chain : null,HLSpellEffectKind.Chain,transform);effect.SetEndpoints(start,end);_impacts.Add(effect.gameObject);
            if (_impacts.Count > 128) { Dispose(_impacts[0]); _impacts.RemoveAt(0); }
            return effect;
        }
        void LateUpdate()
        {
            var dead=new List<(GameObject,ABuffHandlerFactory)>();
            foreach(var pair in _statuses) if(!pair.Key.Item1||!pair.Key.Item2||!pair.Value) dead.Add(pair.Key);
            foreach(var key in dead) { Dispose(_statuses[key]);_statuses.Remove(key); }
            _impacts.RemoveAll(x=>!x);
        }
        void OnDisable() => Clear();
        public void Clear()
        {
            foreach(var root in _statuses.Values) Dispose(root);_statuses.Clear();
            foreach(var root in _impacts) Dispose(root);_impacts.Clear();
        }
        static void Dispose(GameObject go) { if(!go)return;go.SetActive(false);if(Application.isPlaying) Destroy(go);else DestroyImmediate(go); }
    }
}
