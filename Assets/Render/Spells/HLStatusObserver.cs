using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Spells
{
    // Groups the running buff handlers of one entity by target and factory and publishes them to the sink
    public class HLStatusObserver : MonoBehaviour, IVisualBehaviour, IEntityView
    {
        struct StatusState
        {
            public int stacks;
            public float elapsed;
            public float duration;
        }

        readonly List<BuffManager.BuffHandlerData> _observed = new List<BuffManager.BuffHandlerData>();
        Dictionary<(GameObject, ABuffHandlerFactory), StatusState> _groups =
            new Dictionary<(GameObject, ABuffHandlerFactory), StatusState>();
        Dictionary<(GameObject, ABuffHandlerFactory), StatusState> _published =
            new Dictionary<(GameObject, ABuffHandlerFactory), StatusState>();
        int _sinkVersion;
        BuffManager _manager;
        IHLSpellVisualSink _injected;
        IHLSpellVisualSink _lastSink;

        public void Init(Entity entity, RenderManager manager)
        {
            if (entity == null || manager == null)
            {
                Debug.LogError("[HLStatusObserver] Init needs an entity and the RenderManager.");
                return;
            }

            Bind(entity.buffManager != null ? entity.buffManager : entity.GetComponent<BuffManager>(), manager.spellSink);
            HLResourceOutcomeObserver.Ensure(entity).Init(entity, manager);
            GetShieldView().Init(entity, manager);
        }

        // Old path while the stage prefabs still walk IVisualBehaviour, the sink then comes from the registry
        public void Init(Entity entity)
        {
            if (entity == null)
            {
                Bind(null, _injected);
                return;
            }

            Bind(entity.buffManager != null ? entity.buffManager : entity.GetComponent<BuffManager>(), _injected);
            HLResourceOutcomeObserver.Ensure(entity);
            GetShieldView().Init(entity);
        }

        void OnEnable()
        {
            Reconcile();
        }

        void OnDisable()
        {
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

        public void Bind(BuffManager manager, IHLSpellVisualSink sink)
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

        public void Reconcile()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            IHLSpellVisualSink sink = CurrentSink();
            HLSpellVisualSink visualSink = sink as HLSpellVisualSink;
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
                    // TODO: pass the caster once BuffHandlerData carries it (S4)
                    sink.SetStatus(null, group.Key.Item1, group.Key.Item2, group.Value.stacks, group.Value.elapsed,
                                   group.Value.duration, HLClockKind.Simulation);
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
                IHLSpellVisualSink sink = CurrentSink();
                if (sink != null)
                {
                    sink.RemoveStatus(null, data.target, data.buffHandlerFactory);
                }
                _published.Remove((data.target, data.buffHandlerFactory));
            }
            Reconcile();
        }

        IHLSpellVisualSink CurrentSink()
        {
            if (_injected != null)
            {
                return _injected;
            }
            return HLRenderRegistry.current != null ? HLRenderRegistry.current.spellSink : null;
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

        HLAttributeShieldView GetShieldView()
        {
            HLAttributeShieldView shield = GetComponent<HLAttributeShieldView>();
            if (shield == null)
            {
                shield = gameObject.AddComponent<HLAttributeShieldView>();
            }
            return shield;
        }
    }
}
