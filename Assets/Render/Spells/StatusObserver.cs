using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Spells
{
    // Groups the running buff handlers of one entity by target and factory and publishes them to the sink.
    // It is also the one place that wires the unit's other outcome observers: the health or mana outcomes on
    // the unit, and the hit armor plates beside this view.
    public class StatusObserver : MonoBehaviour, IEntityView
    {
        struct StatusState
        {
            public int stacks;
            public float elapsed;
            public float duration;
            public GameObject source;
        }

        readonly List<BuffManager.BuffHandlerData> _observed = new List<BuffManager.BuffHandlerData>();
        Dictionary<(GameObject, ABuffHandlerFactory), StatusState> _groups =
            new Dictionary<(GameObject, ABuffHandlerFactory), StatusState>();
        Dictionary<(GameObject, ABuffHandlerFactory), StatusState> _published =
            new Dictionary<(GameObject, ABuffHandlerFactory), StatusState>();
        int _sinkVersion;
        BuffManager _manager;
        ISpellVisualSink _injected;
        ISpellVisualSink _lastSink;
        GameObject _outcomesOwner;
        ResourceOutcomeObserver _outcomes;
        AttributeShieldView _shield;

        public void Init(Entity entity, RenderManager manager)
        {
            if (entity == null || manager == null)
            {
                Debug.LogError("[StatusObserver] Init needs an entity and the RenderManager.");
                return;
            }

            Init(entity, manager.spellSink, manager.registry);
        }

        // A unit's statuses, its health outcomes and its hit armor plates; a null entity stops observing
        public void Init(Entity entity, ISpellVisualSink sink, RenderRegistry registry)
        {
            if (entity == null)
            {
                Init((BuffManager)null, sink);
                InitOutcomes(null, null, null, sink, registry);
                return;
            }

            BuffManager buffManager = entity.buffManager;
            if (buffManager == null)
            {
                buffManager = entity.GetComponent<BuffManager>();
            }

            if (_manager != buffManager || !ReferenceEquals(_injected, sink))
            {
                Init(buffManager, sink);
            }

            InitOutcomes(entity.gameObject, entity.health, null, sink, registry);
            if (_shield == null)
            {
                _shield = GetComponent<AttributeShieldView>();
            }

            if (_shield == null)
            {
                _shield = gameObject.AddComponent<AttributeShieldView>();
            }
            _shield.Init(entity.attributeManager, entity.gameObject, sink as SpellVisualSink);
        }

        // A character's statuses and its mana outcomes, called every frame by its view; a null character
        // stops observing
        public void Init(Character character, ISpellVisualSink sink, RenderRegistry registry)
        {
            if (character == null)
            {
                Init((BuffManager)null, sink);
                InitOutcomes(null, null, null, sink, registry);
                return;
            }

            InitOutcomes(character.gameObject, null, character.mana, sink, registry);
            if (character.buffManager != null && (_manager != character.buffManager || !ReferenceEquals(_injected, sink)))
            {
                Init(character.buffManager, sink);
            }
        }

        // Statuses only
        public void Init(BuffManager manager, ISpellVisualSink sink)
        {
            Detach();
            _manager = manager;
            _injected = sink;
            if (_manager != null)
            {
                _manager.OnBuffHandlerStarted.AddListener(OnBuffHandlerStarted);
                _manager.OnBuffHandlerStopped.AddListener(OnBuffHandlerStopped);
            }
        }

        void OnEnable()
        {
            if (_outcomes != null)
            {
                _outcomes.enabled = true;
            }
            Reconcile();
        }

        void OnDisable()
        {
            if (_outcomes != null)
            {
                _outcomes.enabled = false;
            }

            RemovePublished();
            _lastSink = null;
            _published.Clear();
        }

        void OnDestroy()
        {
            Detach();
        }

        void LateUpdate()
        {
            Reconcile();
        }

        public void Reconcile()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ISpellVisualSink sink = _injected;
            SpellVisualSink visualSink = sink as SpellVisualSink;
            int version = visualSink != null ? visualSink.presentationVersion : 0;
            if (!ReferenceEquals(sink, _lastSink) || version != _sinkVersion)
            {
                if (!ReferenceEquals(sink, _lastSink))
                {
                    RemovePublished();
                }
                _lastSink = sink;
                _sinkVersion = version;
                _published.Clear();
            }

            if (sink == null || (_observed.Count == 0 && _published.Count == 0))
            {
                return;
            }

            _groups.Clear();
            foreach (BuffManager.BuffHandlerData data in _observed)
            {
                if (data.target == null || data.buffHandlerFactory == null)
                {
                    continue;
                }

                (GameObject, ABuffHandlerFactory) key = (data.target, data.buffHandlerFactory);
                StatusState state = new StatusState();
                state.stacks = Mathf.Max(0, data.currentStacks + data.refreshStacks);
                state.source = data.source;
                BuffHandler handler = data.buffHandler as BuffHandler;
                state.elapsed = handler != null ? handler.durationTimer : 0f;
                state.duration = data.buffHandlerFactory.durationType == DurationType.Infinite
                    ? float.PositiveInfinity
                    : data.buffHandlerFactory.duration;
                if (_groups.TryGetValue(key, out StatusState old))
                {
                    state.stacks += old.stacks;
                    state.elapsed = Mathf.Min(old.elapsed, state.elapsed);
                    state.duration = Mathf.Max(old.duration, state.duration);
                    state.source = old.source;
                }
                _groups[key] = state;
            }

            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), StatusState> previous in _published)
            {
                if (!_groups.ContainsKey(previous.Key))
                {
                    sink.RemoveStatus(null, previous.Key.Item1, previous.Key.Item2);
                }
            }

            foreach (KeyValuePair<(GameObject, ABuffHandlerFactory), StatusState> group in _groups)
            {
                if (!_published.TryGetValue(group.Key, out StatusState old) || !IsSame(old, group.Value))
                {
                    sink.SetStatus(group.Value.source, group.Key.Item1, group.Key.Item2, group.Value.stacks, group.Value.elapsed,
                                   group.Value.duration);
                }
            }

            Dictionary<(GameObject, ABuffHandlerFactory), StatusState> swap = _published;
            _published = _groups;
            _groups = swap;
        }

        // Subscriptions stay while disabled so stop and start events cannot leave stale statuses
        public void Detach()
        {
            if (_manager != null)
            {
                _manager.OnBuffHandlerStarted.RemoveListener(OnBuffHandlerStarted);
                _manager.OnBuffHandlerStopped.RemoveListener(OnBuffHandlerStopped);
            }

            RemovePublished();
            _observed.Clear();
            _groups.Clear();
            _published.Clear();
            _manager = null;
            _lastSink = null;
        }

        void OnBuffHandlerStarted(BuffManager.BuffHandlerData data)
        {
            if (data == null || data.target == null || data.buffHandlerFactory == null)
            {
                return;
            }

            if (!_observed.Contains(data))
            {
                _observed.Add(data);
            }
            Reconcile();
        }

        void OnBuffHandlerStopped(BuffManager.BuffHandlerData data)
        {
            if (data == null)
            {
                return;
            }

            _observed.Remove(data);

            // Several source groups share one target and factory status
            bool remains = false;
            foreach (BuffManager.BuffHandlerData observed in _observed)
            {
                if (observed.target == data.target && observed.buffHandlerFactory == data.buffHandlerFactory)
                {
                    remains = true;
                }
            }

            if (!remains)
            {
                ISpellVisualSink sink = _injected;
                if (sink != null)
                {
                    sink.RemoveStatus(null, data.target, data.buffHandlerFactory);
                }
                _published.Remove((data.target, data.buffHandlerFactory));
            }
            Reconcile();
        }

        void RemovePublished()
        {
            if (_lastSink == null)
            {
                return;
            }

            foreach (BuffManager.BuffHandlerData data in _observed)
            {
                _lastSink.RemoveStatus(null, data.target, data.buffHandlerFactory);
            }
        }

        static bool IsSame(StatusState a, StatusState b)
        {
            return a.stacks == b.stacks && a.elapsed == b.elapsed && a.duration == b.duration;
        }

        // The outcome observer sits on the unit itself, where the resources raise their events
        void InitOutcomes(GameObject owner, ResourceAttribute health, ResourceAttribute mana, ISpellVisualSink sink,
                          RenderRegistry registry)
        {
            if (_outcomesOwner != owner)
            {
                if (_outcomes != null)
                {
                    _outcomes.Init(null, null, sink, registry);
                }

                _outcomesOwner = owner;
                _outcomes = null;
                if (owner != null)
                {
                    _outcomes = owner.GetComponent<ResourceOutcomeObserver>();
                    if (_outcomes == null)
                    {
                        _outcomes = owner.AddComponent<ResourceOutcomeObserver>();
                    }
                    _outcomes.enabled = isActiveAndEnabled;
                }
            }

            if (_outcomes != null)
            {
                _outcomes.Init(health, mana, sink, registry);
            }
        }
    }
}
