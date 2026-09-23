using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Spells
{
    public class SpellVisualSink : MonoBehaviour, ISpellVisualSink
    {
        public static readonly float PulseSeconds = 0.8f;
        public static readonly int MaxImpacts = 128;

        [SerializeField] SpellLooks _looks;

        readonly Dictionary<(GameObject, ABuffHandlerFactory), SpellEffect> _statuses =
            new Dictionary<(GameObject, ABuffHandlerFactory), SpellEffect>();
        readonly List<(GameObject, ABuffHandlerFactory)> _dead = new List<(GameObject, ABuffHandlerFactory)>();
        readonly List<SpellEffect> _remainingEffects = new List<SpellEffect>();
        readonly List<GameObject> _impacts = new List<GameObject>();
        readonly HashSet<(GameObject source, GameObject target)> _heals = new HashSet<(GameObject, GameObject)>();
        readonly List<GameObject> _removing = new List<GameObject>();

        public SpellLooks looks { get { return _looks; } set { _looks = value; } }

        // Set by Init to reach the zone owner
        public Action<Vector3, float, ZoneKind, float> areaPulse { get; set; }

        // Optional bud adapter supplied by the creature view
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
                _impacts.RemoveAll(impact => impact == null);
                return _impacts.Count;
            }
        }

        void OnEnable()
        {
            // The registry may keep this sink across disable and enable
            Clear();
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }

        public void Init(RenderManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("[SpellVisualSink] Init needs the RenderManager.");
                return;
            }

            if (manager.spellLooks != null)
            {
                _looks = manager.spellLooks;
            }

            ZoneRegistry zones = manager.zones;
            areaPulse = (center, radius, kind, strength) => zones.AddPulse(kind, center, radius, strength, PulseSeconds);
        }

        void LateUpdate()
        {
            FlushHealLinks();
            for (int i = _removing.Count - 1; i >= 0; i--)
            {
                _remainingEffects.Clear();
                if (_removing[i] != null)
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
            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), SpellEffect> pair in _statuses)
            {
                if (pair.Key.Item1 == null || pair.Key.Item2 == null || pair.Value == null)
                {
                    _dead.Add(pair.Key);
                }
            }

            if (_dead.Count > 0)
            {
                _presentationVersion++;
            }

            foreach ((GameObject, ABuffHandlerFactory) key in _dead)
            {
                Dispose(_statuses[key] != null ? _statuses[key].gameObject : null);
                _statuses.Remove(key);
            }
            _impacts.RemoveAll(impact => impact == null);
        }

        public GameObject GetStatus(GameObject target, ABuffHandlerFactory factory)
        {
            if (_statuses.TryGetValue((target, factory), out SpellEffect effect) && effect != null)
            {
                return effect.gameObject;
            }
            return null;
        }

        public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float preClampAmount,
                               bool isCritical)
        {
            if (!isActiveAndEnabled || target == null || !float.IsFinite(preClampAmount) || preClampAmount == 0f)
            {
                return;
            }

            SpellLook look = ImpactLook(resource, preClampAmount > 0f);
            if (look == null || look.effectPrefab == null)
            {
                return;
            }

            if (resource == ResourceKind.Health && preClampAmount > 0f && source != null && IsCharacter(source))
            {
                _heals.Add((source, target));
            }

            SpellEffect effect = Spawn(look, transform);
            effect.transform.position = Anchor(target).position + look.offset;
            Entity owner = source != null ? source.GetComponent<Entity>() : null;
            effect.SetSide(owner != null ? owner.entityType : Entity.EntityType.None);
            Entity entity = target.GetComponent<Entity>();
            float maximum = entity != null && entity.health != null ? entity.health.Max : 100f;
            float amount = Mathf.Clamp01(Mathf.Abs(preClampAmount) / Mathf.Max(maximum, 1f));
            float radius = Mathf.Lerp(0.1f, 0.26f, Mathf.Sqrt(amount));
            effect.transform.localScale = Vector3.one * (radius / 0.1f);
            if (isCritical)
            {
                effect.ShowCritical();
            }
            AddImpact(effect.gameObject);
        }

        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
                              float elapsedSeconds, float durationSeconds, ClockKind clock)
        {
            if (!isActiveAndEnabled || target == null || factory == null)
            {
                return;
            }

            if (stacks <= 0)
            {
                RemoveStatus(source, target, factory);
                return;
            }

            if (!float.IsFinite(elapsedSeconds) || float.IsNaN(durationSeconds))
            {
                return;
            }

            (GameObject, ABuffHandlerFactory) key = (target, factory);
            if (!_statuses.TryGetValue(key, out SpellEffect effect) || effect == null)
            {
                SpellLook look = StatusLook(factory, source, target);
                if (look == null || look.effectPrefab == null)
                {
                    return;
                }

                effect = Spawn(look, Anchor(target));
                effect.transform.localPosition = look.offset;
                effect.transform.localScale = Vector3.one * 1.35f;

                // The handler the factory builds says whether the status ticks, every ticking handler is a BuffHandler
                ABuffHandler handler = factory.GetBuffHandler();
                BuffHandler buffHandler = handler as BuffHandler;
                effect.SetPeriod(handler != null && handler.isPeriodic,
                                 buffHandler != null ? buffHandler.data.periodDuration : 0f);
                _statuses[key] = effect;
            }

            effect.SetStatus(stacks, elapsedSeconds, durationSeconds, clock);
            Entity caster = source != null ? source.GetComponent<Entity>() : null;
            effect.SetSide(caster != null ? caster.entityType : Entity.EntityType.None);
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            if (ReferenceEquals(target, null) || ReferenceEquals(factory, null))
            {
                return;
            }

            if (_statuses.Remove((target, factory), out SpellEffect effect) && effect != null)
            {
                effect.transform.SetParent(transform, true);
                effect.BeginRemoval();
                _removing.Add(effect.gameObject);
            }
        }

        public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
        {
            if (!ZonePacker.TryCreate(center, radius, kind, strength, 0f, out Zone zone))
            {
                return;
            }

            SpellLook look = null;
            if (_looks != null)
            {
                look = kind == ZoneKind.Hostile ? _looks.hostileArea : _looks.area;
            }

            if (isActiveAndEnabled && look != null && look.effectPrefab != null)
            {
                SpellEffect ring = Spawn(look, transform);
                ring.lifetime = PulseSeconds;
                ring.transform.position = center + look.offset;
                ring.transform.localScale = Vector3.one * radius;
                ring.SetSide(kind == ZoneKind.Hostile ? Entity.EntityType.Computer : Entity.EntityType.Player);
                AddImpact(ring.gameObject);
            }

            // The zone owner has its own lifetime, forwarding creates no child here
            if (areaPulse != null)
            {
                areaPulse(center, radius, kind, zone.strength);
            }
        }

        public SpellEffect ShowLink(Vector3 start, Vector3 end)
        {
            if (!isActiveAndEnabled || _looks == null || _looks.chain == null || _looks.chain.effectPrefab == null)
            {
                return null;
            }

            if (!float.IsFinite(start.x) || !float.IsFinite(start.y) || !float.IsFinite(start.z)
                || !float.IsFinite(end.x) || !float.IsFinite(end.y) || !float.IsFinite(end.z))
            {
                return null;
            }

            SpellEffect effect = Spawn(_looks.chain, transform);
            effect.lifetime = 0.6f;
            effect.SetEndpoints(start, end);
            AddImpact(effect.gameObject);
            return effect;
        }

        public SpellEffect ShowContactLink(Vector3 previousContact, Vector3 contact)
        {
            SpellEffect effect = ShowLink(previousContact, contact);
            if (effect != null)
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
                if (pair.source == null || pair.target == null)
                {
                    continue;
                }

                Transform anchor = healerAnchor != null ? healerAnchor(pair.source) : null;
                if (anchor == null)
                {
                    CharacterView view = pair.source.GetComponentInChildren<CharacterView>();
                    anchor = view != null ? view.bud0 : null;
                }

                Vector3 start = anchor != null ? anchor.position : pair.source.transform.position;
                Vector3 end = Anchor(pair.target).position;
                ShowLink(start, end);
                if (linkObserved != null)
                {
                    linkObserved(start, end);
                }
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

            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), SpellEffect> pair in _statuses)
            {
                Dispose(pair.Value != null ? pair.Value.gameObject : null);
            }
            _statuses.Clear();

            foreach (GameObject impact in _impacts)
            {
                Dispose(impact);
            }
            _impacts.Clear();
        }

        SpellLook ImpactLook(ResourceKind resource, bool isGain)
        {
            if (_looks == null)
            {
                return null;
            }

            if (resource == ResourceKind.Mana)
            {
                return isGain ? _looks.manaGain : _looks.manaLoss;
            }
            return isGain ? _looks.heal : _looks.impact;
        }

        SpellLook StatusLook(ABuffHandlerFactory factory, GameObject source, GameObject target)
        {
            if (_looks == null)
            {
                return null;
            }

            // TODO: read the caster from BuffHandlerData.source once it exists (S4), until then only a caller that
            // knows the caster picks bane, and the healer's Character, which is not an Entity, plays for the player
            bool isSameSide = true;
            if (source != null)
            {
                Entity caster = source.GetComponent<Entity>();
                Entity recipient = target.GetComponent<Entity>();
                Entity.EntityType casterSide = caster != null ? caster.entityType : Entity.EntityType.Player;
                Entity.EntityType recipientSide = recipient != null ? recipient.entityType : Entity.EntityType.Player;
                isSameSide = casterSide == recipientSide;
            }
            return _looks.GetLook(factory, isSameSide);
        }

        SpellEffect Spawn(SpellLook look, Transform parent)
        {
            SpellEffect effect = Instantiate(look.effectPrefab, parent, false);
            effect.Init();
            effect.SetColor(look.tint);
            return effect;
        }

        void AddImpact(GameObject impact)
        {
            _impacts.Add(impact);
            if (_impacts.Count > MaxImpacts)
            {
                Dispose(_impacts[0]);
                _impacts.RemoveAt(0);
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
            return entity != null && entity.targetPoint != null ? entity.targetPoint.transform : target.transform;
        }

        // Also runs from edit mode tests, where Destroy is not allowed
        static void Dispose(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(effect);
            }
            else
            {
                DestroyImmediate(effect);
            }
        }
    }
}
