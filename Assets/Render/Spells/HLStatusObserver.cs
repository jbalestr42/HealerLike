using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLStatusObserver : MonoBehaviour, IVisualBehaviour
    {
        readonly List<BuffManager.BuffHandlerData> _observed = new List<BuffManager.BuffHandlerData>();
        Dictionary<(GameObject, ABuffHandlerFactory), (int stacks, float elapsed, float duration)> _groups =
            new Dictionary<(GameObject, ABuffHandlerFactory), (int, float, float)>();
        Dictionary<(GameObject, ABuffHandlerFactory), (int stacks, float elapsed, float duration)> _published =
            new Dictionary<(GameObject, ABuffHandlerFactory), (int, float, float)>();
        int _sinkVersion;
        BuffManager _manager;
        IHLSpellVisualSink _injected;
        IHLSpellVisualSink _lastSink;

        public void Init(Entity entity)
        {
            Bind(entity ? entity.buffManager ?? entity.GetComponent<BuffManager>() : null, _injected);
            if (!entity)
            {
                return;
            }
            HLResourceOutcomeObserver.Ensure(entity);
            HLAttributeShieldView shield = GetComponent<HLAttributeShieldView>();
            if (!shield)
            {
                shield = gameObject.AddComponent<HLAttributeShieldView>();
            }
            shield.Init(entity);
        }

        void OnEnable()
        {
            Reconcile();
        }

        void OnDisable()
        {
            foreach (BuffManager.BuffHandlerData data in _observed)
            {
                _lastSink?.RemoveStatus(null, data.target, data.buffHandlerFactory);
            }
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
            if (_manager)
            {
                _manager.OnBuffHandlerStarted.AddListener(Started);
                _manager.OnBuffHandlerStopped.AddListener(Stopped);
            }
        }

        public void Reconcile()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            IHLSpellVisualSink sink = _injected ?? HLRenderRegistry.current?.spellSink;
            int version = sink is HLSpellVisualSink visual ? visual.presentationVersion : 0;
            if (!ReferenceEquals(sink, _lastSink) || version != _sinkVersion)
            {
                if (!ReferenceEquals(sink, _lastSink))
                {
                    foreach (BuffManager.BuffHandlerData data in _observed)
                    {
                        _lastSink?.RemoveStatus(null, data.target, data.buffHandlerFactory);
                    }
                }
                _lastSink = sink;
                _sinkVersion = version;
                _published.Clear();
            }
            if (sink == null)
            {
                return;
            }
            if (_observed.Count == 0 && _published.Count == 0)
            {
                return;
            }
            _groups.Clear();
            foreach (BuffManager.BuffHandlerData data in _observed)
            {
                if (!data.target || !data.buffHandlerFactory)
                {
                    continue;
                }
                (GameObject, ABuffHandlerFactory) key = (data.target, data.buffHandlerFactory);
                int stacks = Mathf.Max(0, data.currentStacks + data.refreshStacks);
                float elapsed = data.buffHandler is BuffHandler bh ? bh.durationTimer : 0f;
                float duration =
                    data.buffHandlerFactory.durationType == DurationType.Infinite
                        ? float.PositiveInfinity
                        : data.buffHandlerFactory.duration;
                if (_groups.TryGetValue(key, out (int stacks, float elapsed, float duration) old))
                {
                    _groups[key] = (
                        old.stacks + stacks,
                        Mathf.Min(old.elapsed, elapsed),
                        Mathf.Max(old.duration, duration)
                    );
                }
                else
                {
                    _groups[key] = (stacks, elapsed, duration);
                }
            }
            foreach (
                KeyValuePair<(GameObject, ABuffHandlerFactory), (int stacks, float elapsed, float duration)> previous
                    in _published
            )
            {
                if (!_groups.ContainsKey(previous.Key))
                {
                    sink.RemoveStatus(null, previous.Key.Item1, previous.Key.Item2);
                }
            }
            foreach (
                KeyValuePair<(GameObject, ABuffHandlerFactory), (int stacks, float elapsed, float duration)> group
                    in _groups
            )
            {
                if (
                    !_published.TryGetValue(
                        group.Key,
                        out (int stacks, float elapsed, float duration) old
                    )
                    || !old.Equals(group.Value)
                )
                {
                    sink.SetStatus(
                        null,
                        group.Key.Item1,
                        group.Key.Item2,
                        group.Value.stacks,
                        group.Value.elapsed,
                        group.Value.duration,
                        HLClockKind.Simulation
                    );
                }
            }
            Dictionary<(GameObject, ABuffHandlerFactory), (int stacks, float elapsed, float duration)> swap =
                _published;
            _published = _groups;
            _groups = swap;
        }

        // Keep subscriptions while disabled so stop/start events cannot leave stale status records.
        public void Detach()
        {
            if (_manager)
            {
                _manager.OnBuffHandlerStarted.RemoveListener(Started);
                _manager.OnBuffHandlerStopped.RemoveListener(Stopped);
            }
            foreach (BuffManager.BuffHandlerData data in _observed)
            {
                _lastSink?.RemoveStatus(null, data.target, data.buffHandlerFactory);
            }
            _observed.Clear();
            _groups.Clear();
            _published.Clear();
            _manager = null;
            _lastSink = null;
        }

        void Started(BuffManager.BuffHandlerData data)
        {
            if (data == null || !data.target || !data.buffHandlerFactory)
            {
                return;
            }
            if (!_observed.Contains(data))
            {
                _observed.Add(data);
            }
            Reconcile();
        }

        void Stopped(BuffManager.BuffHandlerData data)
        {
            if (data == null)
            {
                return;
            }
            _observed.Remove(data);
            // Several gameplay source groups share one (target, factory) visual key.
            bool remains = _observed.Exists(x =>
                x.target == data.target && x.buffHandlerFactory == data.buffHandlerFactory
            );
            if (!remains)
            {
                (_injected ?? HLRenderRegistry.current?.spellSink)?.RemoveStatus(
                    null,
                    data.target,
                    data.buffHandlerFactory
                );
                _published.Remove((data.target, data.buffHandlerFactory));
            }
            Reconcile();
        }
    }
}
