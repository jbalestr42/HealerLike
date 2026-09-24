using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Spells
{
    // Builds every status, impact, link and area pulse from the effect vocabulary, one element per target and element
    public class SpellVisualSink : MonoBehaviour, ISpellVisualSink
    {
        public static readonly float PulseSeconds = 0.8f;
        public static readonly int MaxImpacts = 128;

        // Every handler that resolved to one element on one target, summed into that element
        class Status
        {
            public SpellEffect effect;
            public Dictionary<ABuffHandlerFactory, int> sources = new Dictionary<ABuffHandlerFactory, int>();
            public float charges;
        }

        [SerializeField] SpellLooks _looks;
        [SerializeField] EffectVocabulary _vocabulary;
        [SerializeField] Material _material;

        readonly Dictionary<(GameObject, EffectElement), Status> _statuses =
            new Dictionary<(GameObject, EffectElement), Status>();
        // The element each handler fed on each target, so a handler's data is read once
        readonly Dictionary<(GameObject, ABuffHandlerFactory), EffectElement> _elements =
            new Dictionary<(GameObject, ABuffHandlerFactory), EffectElement>();
        readonly List<(GameObject, EffectElement)> _dead = new List<(GameObject, EffectElement)>();
        readonly List<(GameObject, ABuffHandlerFactory)> _deadSources = new List<(GameObject, ABuffHandlerFactory)>();
        readonly List<SpellEffect> _remainingEffects = new List<SpellEffect>();
        readonly List<GameObject> _impacts = new List<GameObject>();
        readonly Dictionary<GameObject, List<(GameObject, EffectFamily)>> _groups =
            new Dictionary<GameObject, List<(GameObject, EffectFamily)>>();
        readonly List<GameObject> _removing = new List<GameObject>();

        public SpellLooks looks { get { return _looks; } set { _looks = value; } }
        public EffectVocabulary vocabulary { get { return _vocabulary; } set { _vocabulary = value; } }
        public Material material { get { return _material; } set { _material = value; } }

        PrimitiveMeshes _meshes;
        public PrimitiveMeshes meshes { get { return _meshes; } set { _meshes = value; } }

        // Set by Init to reach the zone owner
        public Action<Vector3, float, ZoneKind, float> areaPulse { get; set; }

        // Optional bud adapter supplied by the creature view
        public Func<GameObject, Transform> healerAnchor { get; set; }

        public Func<GameObject, bool> isCharacterSource { get; set; }

        public Action<Vector3, Vector3> linkObserved { get; set; }

        int _presentationVersion;
        public int presentationVersion { get { return _presentationVersion; } }

        public int statusCount { get { return _statuses.Count; } }

        // Counts until the next LateUpdate sweeps the impacts that ended
        public int impactCount { get { return _impacts.Count; } }

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

            _meshes = manager.meshes;
            ZoneRegistry zones = manager.zones;
            areaPulse = (center, radius, kind, strength) =>
            {
                zones.AddPulse(kind, center, radius, strength, PulseSeconds);
            };
        }

        void LateUpdate()
        {
            FlushLinks();
            for (int i = _removing.Count - 1; i >= 0; i--)
            {
                _remainingEffects.Clear();
                if (_removing[i] != null)
                {
                    _removing[i].GetComponentsInChildren(false, _remainingEffects);
                }

                if (_remainingEffects.Count == 0)
                {
                    SpellEffect.Dispose(_removing[i]);
                    _removing.RemoveAt(i);
                }
            }

            _deadSources.Clear();
            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), EffectElement> pair in _elements)
            {
                if (pair.Key.Item2 == null && pair.Key.Item1 != null)
                {
                    _deadSources.Add(pair.Key);
                }
            }

            foreach ((GameObject, ABuffHandlerFactory) key in _deadSources)
            {
                Drop(key.Item1, key.Item2);
            }

            _dead.Clear();
            foreach (KeyValuePair<(GameObject, EffectElement), Status> pair in _statuses)
            {
                if (pair.Key.Item1 == null || pair.Value.effect == null)
                {
                    _dead.Add(pair.Key);
                }
            }

            if (_dead.Count > 0)
            {
                _presentationVersion++;
            }

            foreach ((GameObject, EffectElement) key in _dead)
            {
                SpellEffect effect = _statuses[key].effect;
                if (effect != null)
                {
                    SpellEffect.Dispose(effect.gameObject);
                }
                _statuses.Remove(key);
            }

            if (_dead.Count > 0)
            {
                ForgetDeadTargets();
            }
            _impacts.RemoveAll(impact => impact == null);
        }

        public GameObject GetStatus(GameObject target, ABuffHandlerFactory factory)
        {
            if (!_elements.TryGetValue((target, factory), out EffectElement element))
            {
                return null;
            }

            SpellEffect effect = GetElement(target, element);
            if (effect == null)
            {
                return null;
            }
            return effect.gameObject;
        }

        public SpellEffect GetElement(GameObject target, EffectElement element)
        {
            if (_statuses.TryGetValue((target, element), out Status status) && status.effect != null)
            {
                return status.effect;
            }
            return null;
        }

        // HitArmor charges feed the plates of their target like one more source, the last charge takes them away
        public void SetCharges(GameObject target, float charges)
        {
            if (target == null)
            {
                return;
            }

            bool hasCharges = isActiveAndEnabled && float.IsFinite(charges) && charges > 0f;
            _statuses.TryGetValue((target, EffectElement.Plates), out Status status);
            if (!hasCharges)
            {
                if (status != null)
                {
                    status.charges = 0f;
                    Close(target, EffectElement.Plates, status);
                }
                return;
            }

            if (status == null || status.effect == null)
            {
                EffectRecipe recipe = EffectComposer.Compose(_vocabulary, EffectElement.Plates, EffectFamily.Boon,
                                                             EffectTempo.ForDuration, 0f, 1, charges, 0f);
                status = Open(target, EffectElement.Plates, recipe);
                if (status == null)
                {
                    return;
                }
            }

            if (status.charges != charges)
            {
                status.charges = charges;
                Refresh(status, status.effect.elapsedSeconds, float.PositiveInfinity, ClockKind.Simulation);
            }
        }

        public SpellEffect ShowContactLink(Vector3 previousContact, Vector3 contact)
        {
            return ShowLink(previousContact, contact, EffectFamily.Damage, true);
        }

        // A beam in the family's accent, lime for a heal so gold stays with Boon
        public SpellEffect ShowLink(Vector3 start, Vector3 end, EffectFamily family, bool isContactThread)
        {
            if (!isActiveAndEnabled || !IsFinite(start) || !IsFinite(end))
            {
                return null;
            }

            EffectRecipe recipe = EffectComposer.Compose(_vocabulary, EffectElement.Beam, family, EffectTempo.Once, 0f,
                                                         1, 0f, 0f);
            SpellEffect effect = Create(recipe);
            if (effect == null)
            {
                return null;
            }

            effect.transform.SetParent(transform, false);
            effect.SetEndpoints(start, end, isContactThread);
            AddImpact(effect.gameObject);
            return effect;
        }

        // A character that reached two or more recipients of one family in one frame cast on a group, each gets a beam
        public void FlushLinks()
        {
            if (!isActiveAndEnabled)
            {
                ClearGroups();
                return;
            }

            foreach (KeyValuePair<GameObject, List<(GameObject, EffectFamily)>> group in _groups)
            {
                if (group.Key == null)
                {
                    continue;
                }

                Transform anchor = healerAnchor != null ? healerAnchor(group.Key) : null;
                if (anchor == null)
                {
                    CharacterView view = group.Key.GetComponentInChildren<CharacterView>();
                    anchor = view != null ? view.bud0 : null;
                }

                Vector3 start = anchor != null ? anchor.position : group.Key.transform.position;
                foreach ((GameObject target, EffectFamily family) recipient in group.Value)
                {
                    if (recipient.target == null || Recipients(group.Value, recipient.family) < 2)
                    {
                        continue;
                    }

                    Vector3 end = EffectPlacement.Anchors(recipient.target).bodyCentre;
                    ShowLink(start, end, recipient.family, false);
                    if (linkObserved != null)
                    {
                        linkObserved(start, end);
                    }
                }
            }
            ClearGroups();
        }

        public void Clear()
        {
            _presentationVersion++;
            ClearGroups();
            foreach (GameObject root in _removing)
            {
                SpellEffect.Dispose(root);
            }
            _removing.Clear();

            foreach (KeyValuePair<(GameObject, EffectElement), Status> pair in _statuses)
            {
                if (pair.Value.effect != null)
                {
                    SpellEffect.Dispose(pair.Value.effect.gameObject);
                }
            }
            _statuses.Clear();
            _elements.Clear();

            foreach (GameObject impact in _impacts)
            {
                SpellEffect.Dispose(impact);
            }
            _impacts.Clear();
        }

        // Reads the handler once: an authored row wins, else its channels pick the element
        Status Open(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            if (_vocabulary == null)
            {
                return null;
            }

            EffectRecipe recipe;
            SpellLook row = _looks != null ? _looks.GetLook(factory) : null;
            if (row != null)
            {
                float period = EffectDerivation.Period(factory);
                recipe = EffectComposer.Compose(_vocabulary, row.element, row.family, row.tempo, period, 1, 0f, 0f);
            }
            else
            {
                EffectChannels channels = EffectDerivation.Channels(factory, IsSameSide(source, target));
                recipe = EffectComposer.Compose(_vocabulary, channels, 1, 0f);
            }

            if (recipe == null)
            {
                return null;
            }

            EffectElement element = recipe.element;
            _elements[(target, factory)] = element;
            if (_statuses.TryGetValue((target, element), out Status status) && status.effect != null)
            {
                return status;
            }
            return Open(target, element, recipe);
        }

        Status Open(GameObject target, EffectElement element, EffectRecipe recipe)
        {
            SpellEffect effect = Create(recipe);
            if (effect == null)
            {
                return null;
            }

            EffectPlacement.Place(effect, Parent(target), EffectPlacement.Anchors(target));
            Status status = new Status();
            status.effect = effect;
            _statuses[(target, element)] = status;
            return status;
        }

        void Refresh(Status status, float elapsedSeconds, float durationSeconds, ClockKind clock)
        {
            int stacks = 0;
            foreach (KeyValuePair<ABuffHandlerFactory, int> source in status.sources)
            {
                stacks += source.Value;
            }

            SpellEffect effect = status.effect;
            effect.SetCount(EffectComposer.Count(effect.recipe.entry, Mathf.Max(1, stacks), status.charges, 0f));
            effect.SetStatus(Mathf.Max(1, stacks), elapsedSeconds, durationSeconds, clock);
        }

        void Drop(GameObject target, ABuffHandlerFactory factory)
        {
            if (!_elements.TryGetValue((target, factory), out EffectElement element))
            {
                return;
            }

            _elements.Remove((target, factory));
            if (!_statuses.TryGetValue((target, element), out Status status))
            {
                return;
            }

            status.sources.Remove(factory);
            if (!Close(target, element, status) && status.effect != null)
            {
                Refresh(status, status.effect.elapsedSeconds, status.effect.durationSeconds, status.effect.clock);
            }
        }

        // The element leaves with its last source and keeps only its cosmetic tail
        bool Close(GameObject target, EffectElement element, Status status)
        {
            if (status.sources.Count > 0 || status.charges > 0f)
            {
                return false;
            }

            _statuses.Remove((target, element));
            if (status.effect != null)
            {
                status.effect.transform.SetParent(transform, true);
                status.effect.BeginRemoval();
                _removing.Add(status.effect.gameObject);
            }
            return true;
        }

        void ForgetDeadTargets()
        {
            _deadSources.Clear();
            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), EffectElement> pair in _elements)
            {
                if (pair.Key.Item1 == null)
                {
                    _deadSources.Add(pair.Key);
                }
            }

            foreach ((GameObject, ABuffHandlerFactory) key in _deadSources)
            {
                _elements.Remove(key);
            }
        }

        SpellEffect Create(EffectRecipe recipe)
        {
            if (recipe == null || _meshes == null)
            {
                return null;
            }

            GameObject effectGo = new GameObject(recipe.element.ToString());
            effectGo.transform.SetParent(transform, false);
            SpellEffect effect = effectGo.AddComponent<SpellEffect>();
            effect.Init(recipe, _meshes, _material);
            return effect;
        }

        void AddGroupRecipient(GameObject source, GameObject target, EffectFamily family)
        {
            if (!_groups.TryGetValue(source, out List<(GameObject, EffectFamily)> recipients))
            {
                recipients = new List<(GameObject, EffectFamily)>();
                _groups[source] = recipients;
            }

            foreach ((GameObject, EffectFamily) recipient in recipients)
            {
                if (recipient.Item1 == target)
                {
                    return;
                }
            }
            recipients.Add((target, family));
        }

        static int Recipients(List<(GameObject, EffectFamily)> recipients, EffectFamily family)
        {
            int count = 0;
            foreach ((GameObject, EffectFamily) recipient in recipients)
            {
                if (recipient.Item2 == family)
                {
                    count++;
                }
            }
            return count;
        }

        void ClearGroups()
        {
            if (_groups.Count > 0)
            {
                _groups.Clear();
            }
        }

        void AddImpact(GameObject impact)
        {
            _impacts.Add(impact);
            if (_impacts.Count > MaxImpacts)
            {
                SpellEffect.Dispose(_impacts[0]);
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

        // The caster's side against the target's: the healer's Character, which is not an Entity, plays for the
        // player, and a status without a caster is taken as its target's own
        static bool IsSameSide(GameObject source, GameObject target)
        {
            if (source == null)
            {
                return true;
            }

            Entity caster = source.GetComponent<Entity>();
            Entity recipient = target.GetComponent<Entity>();
            Entity.EntityType casterSide = caster != null ? caster.entityType : Entity.EntityType.Player;
            Entity.EntityType recipientSide = recipient != null ? recipient.entityType : Entity.EntityType.Player;
            return casterSide == recipientSide;
        }

        // A status follows its unit, so it hangs under the target point
        static Transform Parent(GameObject target)
        {
            Entity entity = target.GetComponent<Entity>();
            if (entity != null && entity.targetPoint != null)
            {
                return entity.targetPoint.transform;
            }
            return target.transform;
        }

        static bool IsFinite(Vector3 point)
        {
            return float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
        }

        #region ISpellVisualSink

        public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float preClampAmount,
                               bool isCritical)
        {
            if (!isActiveAndEnabled || target == null || !float.IsFinite(preClampAmount) || preClampAmount == 0f)
            {
                return;
            }

            bool isGain = preClampAmount > 0f;
            EffectFamily family = isGain ? EffectFamily.Heal : EffectFamily.Damage;
            EffectElement element = isGain ? EffectElement.Rise : EffectElement.Burst;
            if (resource == ResourceKind.Mana)
            {
                element = EffectComposer.Mana(isGain);
            }

            Entity entity = target.GetComponent<Entity>();
            float maximum = 100f;
            if (entity != null && entity.health != null)
            {
                maximum = entity.health.Max;
            }
            float amount = Mathf.Clamp01(Mathf.Abs(preClampAmount) / Mathf.Max(maximum, 1f));
            EffectRecipe recipe = EffectComposer.Compose(_vocabulary, element, family, EffectTempo.Once, 0f, 1, 0f,
                amount);
            SpellEffect effect = Create(recipe);
            if (effect == null)
            {
                return;
            }

            if (resource == ResourceKind.Health && source != null && IsCharacter(source))
            {
                AddGroupRecipient(source, target, family);
            }

            EffectPlacement.Place(effect, transform, EffectPlacement.Anchors(target));
            if (element == EffectElement.Burst)
            {
                // A bigger hit draws a bigger star, turned to the camera because it is flat
                effect.transform.localScale *= Mathf.Lerp(0.8f, 1.6f, Mathf.Sqrt(amount));
                if (Camera.main != null)
                {
                    effect.transform.rotation = Camera.main.transform.rotation;
                }
            }

            // The flat star turns to the camera, a rim under it would read as a bar
            if (element != EffectElement.Burst)
            {
                Entity owner = source != null ? source.GetComponent<Entity>() : null;
                Entity.EntityType side = Entity.EntityType.None;
                if (owner != null)
                {
                    side = owner.entityType;
                }
                effect.SetSide(side);
            }

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

            Status status = null;
            if (_elements.TryGetValue((target, factory), out EffectElement known))
            {
                _statuses.TryGetValue((target, known), out status);
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
            Refresh(status, elapsedSeconds, durationSeconds, clock);
            Entity caster = source != null ? source.GetComponent<Entity>() : null;
            if (caster != null)
            {
                status.effect.SetSide(caster.entityType);
            }
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            if (ReferenceEquals(target, null) || ReferenceEquals(factory, null))
            {
                return;
            }

            Drop(target, factory);
        }

        public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
        {
            if (!ZonePacker.TryCreate(center, radius, kind, strength, 0f, out Zone zone))
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                bool isHostile = kind == ZoneKind.Hostile;
                EffectElement element = EffectElement.Ring;
                EffectFamily family = EffectFamily.Heal;
                Entity.EntityType side = Entity.EntityType.Player;
                if (isHostile)
                {
                    element = EffectElement.Litter;
                    family = EffectFamily.Bane;
                    side = Entity.EntityType.Computer;
                }
                EffectRecipe recipe = EffectComposer.Compose(_vocabulary, element, family, EffectTempo.Once, 0f, 1, 0f,
                                                             0f);
                SpellEffect ring = Create(recipe);
                if (ring != null)
                {
                    recipe.cycleSeconds = PulseSeconds;
                    ring.transform.SetParent(transform, false);
                    ring.transform.position = center;
                    ring.transform.localScale = Vector3.one * radius;
                    ring.SetSide(side);
                    ring.Advance(0f);
                    AddImpact(ring.gameObject);
                }
            }

            // The zone owner has its own lifetime, forwarding creates no child here
            if (areaPulse != null)
            {
                areaPulse(center, radius, kind, zone.strength);
            }
        }

        #endregion
    }
}
