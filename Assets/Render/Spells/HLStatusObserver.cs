using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    /// <summary>Attach beneath EntityModel before Init. No buffEffect slots or creature-builder dependency.</summary>
    public sealed class HLStatusObserver : MonoBehaviour, IVisualBehaviour
    {
        readonly List<BuffManager.BuffHandlerData> _observed = new List<BuffManager.BuffHandlerData>();
        BuffManager _manager;
        IHLSpellVisualSink _injected, _lastSink;
        public void Init(Entity entity) => Bind(entity ? entity.buffManager ?? entity.GetComponent<BuffManager>() : null, _injected);
        public void Bind(BuffManager manager,IHLSpellVisualSink sink)
        {
            Detach();_manager=manager;_injected=sink;
            if(_manager) { _manager.OnBuffHandlerStarted.AddListener(Started);_manager.OnBuffHandlerStopped.AddListener(Stopped); }
        }
        void Started(BuffManager.BuffHandlerData data)
        {
            if(data==null||!data.target||!data.buffHandlerFactory)return;
            if(!_observed.Contains(data))_observed.Add(data);
            Reconcile();
        }
        void Stopped(BuffManager.BuffHandlerData data)
        {
            if(data==null)return;_observed.Remove(data);
            // Multiple gameplay source groups collapse to the frozen (target, factory) visual key.
            bool remains=_observed.Exists(x=>x.target==data.target&&x.buffHandlerFactory==data.buffHandlerFactory);
            if(!remains)(_injected??HLRenderRegistry.Current?.SpellSink)?.RemoveStatus(null,data.target,data.buffHandlerFactory);
            else Reconcile();
        }
        void LateUpdate() => Reconcile();
        public void Reconcile()
        {
            if (!isActiveAndEnabled) return;
            var sink=_injected??HLRenderRegistry.Current?.SpellSink;
            if(!ReferenceEquals(sink,_lastSink))
            { foreach(var data in _observed)_lastSink?.RemoveStatus(null,data.target,data.buffHandlerFactory);_lastSink=sink; }
            if(sink==null)return;
            var groups=new Dictionary<(GameObject,ABuffHandlerFactory),(int stacks,float elapsed,float duration)>();
            foreach(var data in _observed)
            {
                if(!data.target||!data.buffHandlerFactory)continue;
                var key=(data.target,data.buffHandlerFactory);
                int stacks=Mathf.Max(0,data.currentStacks+data.refreshStacks);
                float elapsed=data.buffHandler is BuffHandler bh ? bh.durationTimer : 0;
                float duration=data.buffHandlerFactory.durationType==DurationType.Infinite ? float.PositiveInfinity : data.buffHandlerFactory.duration;
                if(groups.TryGetValue(key,out var old)) groups[key]=(old.stacks+stacks,Mathf.Min(old.elapsed,elapsed),Mathf.Max(old.duration,duration));
                else groups[key]=(stacks,elapsed,duration);
            }
            foreach(var group in groups) sink.SetStatus(null,group.Key.Item1,group.Key.Item2,group.Value.stacks,group.Value.elapsed,group.Value.duration,HLClockKind.Simulation);
        }
        void OnEnable() => Reconcile();
        void OnDisable()
        {
            foreach (var data in _observed) _lastSink?.RemoveStatus(null, data.target, data.buffHandlerFactory);
            _lastSink = null;
        }
        void OnDestroy()=>Detach();
        // Keep subscriptions while disabled so stop/start events cannot leave stale status records.
        public void Detach()
        {
            if(_manager){_manager.OnBuffHandlerStarted.RemoveListener(Started);_manager.OnBuffHandlerStopped.RemoveListener(Stopped);}
            foreach(var data in _observed) _lastSink?.RemoveStatus(null,data.target,data.buffHandlerFactory);
            _observed.Clear();_manager=null;_lastSink=null;
        }
    }
}
