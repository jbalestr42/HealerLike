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
        readonly List<BuffManager.BuffHandlerData> _observed = new List<BuffManager.BuffHandlerData>();
        Dictionary<HandlerKey, StatusGroup> _groups = new Dictionary<HandlerKey, StatusGroup>();
        Dictionary<HandlerKey, StatusGroup> _published = new Dictionary<HandlerKey, StatusGroup>();
        BuffManager _manager;
        ISpellVisualSink _injected;
        // The render sink, which can drop a status on its own; another sink keeps whatever it was sent
        SpellVisualSink _visualSink;
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
            _shield.Init(entity.attributeManager, entity.gameObject, _visualSink);
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
            if (character.buffManager != null
                && (_manager != character.buffManager || !ReferenceEquals(_injected, sink)))
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
            _visualSink = sink as SpellVisualSink;
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

        // Publishes before the RenderManager ticks the sink in its LateUpdate
        void Update()
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
            if (!ReferenceEquals(sink, _lastSink))
            {
                RemovePublished();
                _lastSink = sink;
                _published.Clear();
            }

            if (sink == null || (_observed.Count == 0 && _published.Count == 0))
            {
                return;
            }

            StatusGroup.Collect(_observed, _groups);
            foreach (KeyValuePair<HandlerKey, StatusGroup> previous in _published)
            {
                if (!_groups.ContainsKey(previous.Key))
                {
                    sink.RemoveStatus(null, previous.Key.target, previous.Key.factory);
                }
            }

            foreach (KeyValuePair<HandlerKey, StatusGroup> group in _groups)
            {
                if (!_published.TryGetValue(group.Key, out StatusGroup old) || !old.IsSame(group.Value)
                    || IsDropped(group.Key, group.Value))
                {
                    sink.SetStatus(group.Value.source, group.Key.target, group.Key.factory, group.Value.stacks,
                                   group.Value.elapsed, group.Value.duration);
                }
            }

            Dictionary<HandlerKey, StatusGroup> swap = _published;
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
                _published.Remove(new HandlerKey(data.target, data.buffHandlerFactory));
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

        // A status the render sink dropped on its own, after a clear or a sweep, is published again
        bool IsDropped(HandlerKey key, StatusGroup state)
        {
            return _visualSink != null && state.stacks > 0 && !_visualSink.IsShowing(key.target, key.factory);
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
