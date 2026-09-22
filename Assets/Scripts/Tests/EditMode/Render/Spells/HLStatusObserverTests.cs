using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLStatusObserverTests
    {
        static void DestroyHost(GameObject go)
        {
            if (!go) return;
            foreach(var effect in go.GetComponentsInChildren<HLSpellEffect>(true)) TestHelpers.InvokePrivate(effect,"OnDestroy");
            foreach(var sink in go.GetComponentsInChildren<HLSpellVisualSink>(true)) TestHelpers.InvokePrivate(sink,"OnDestroy");
            Object.DestroyImmediate(go);
        }
        sealed class HLSinkSpy : IHLSpellVisualSink
        {
            public int Calls;
            public void SetStatus(GameObject source,GameObject target,ABuffHandlerFactory factory,int stacks,float elapsed,float duration,HLClockKind clock) => Calls++;
            public void RemoveStatus(GameObject source,GameObject target,ABuffHandlerFactory factory) { }
            public void ShowImpact(GameObject source,GameObject target,HLResourceKind resource,float amount,bool critical) { }
            public void PulseArea(Vector3 center,float radius,HealerLike.Render.Zones.HLZoneKind kind,float strength) { }
        }
        [TestCase(false)] [TestCase(true)]
        public void IdleReconciliationAllocatesNothingAndDoesNotRepublish(bool populated)
        {
            var go = new GameObject("HLObserver");
            var factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            try
            {
                factory.data = new BuffHandlerData { durationType=DurationType.Infinite };
                var manager = go.AddComponent<BuffManager>();
                var observer = go.AddComponent<HLStatusObserver>(); var sink = new HLSinkSpy();
                observer.Bind(manager,sink);
                var data = new BuffManager.BuffHandlerData { target=go,buffHandlerFactory=factory,currentStacks=1 };
                if (populated) manager.OnBuffHandlerStarted.Invoke(data);
                for(int i=0;i<32;i++) observer.Reconcile();
                int calls = sink.Calls;
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<32;i++) observer.Reconcile();
                long allocated = System.GC.GetAllocatedBytesForCurrentThread()-before;
                Assert.AreEqual(0,allocated);
                Assert.AreEqual(calls,sink.Calls);
                if(populated) { data.currentStacks=2; observer.Reconcile(); Assert.AreEqual(calls+1,sink.Calls); }
            }
            finally { DestroyHost(go); Object.DestroyImmediate(factory); }
        }
        [Test] public void UnchangedStatusReturnsWhenRegistrySinkIsReenabled()
        {
            var go=new GameObject("HLObserver"); var host=new GameObject("HLSink");
            var factory=ScriptableObject.CreateInstance<BuffHandlerFactory>(); var modifier=ScriptableObject.CreateInstance<FlatModifierFactory>();
            var previous=HLRenderRegistry.Current;
            try
            {
                modifier.data=new FlatModifierData {value=1};
                factory.data=new BuffHandlerData {durationType=DurationType.Infinite,buffFactoryList=new List<ABuffFactory>{modifier}};
                var manager=go.AddComponent<BuffManager>(); var observer=go.AddComponent<HLStatusObserver>();
                var sink=host.AddComponent<HLSpellVisualSink>();
                HLRenderRegistry.Current=new HLRenderRegistry {SpellSink=sink}; observer.Bind(manager,null);
                manager.OnBuffHandlerStarted.Invoke(new BuffManager.BuffHandlerData {target=go,buffHandlerFactory=factory,currentStacks=1});
                Assert.AreEqual(1,sink.StatusCount);
                sink.enabled=false; TestHelpers.InvokePrivate(sink,"OnDisable"); observer.Reconcile(); Assert.AreEqual(0,sink.StatusCount);
                sink.enabled=true; TestHelpers.InvokePrivate(sink,"OnEnable"); observer.Reconcile(); Assert.AreEqual(1,sink.StatusCount);
                sink.Clear(); observer.Reconcile(); Assert.AreEqual(1,sink.StatusCount);
            }
            finally { HLRenderRegistry.Current=previous; DestroyHost(go); DestroyHost(host); Object.DestroyImmediate(factory); Object.DestroyImmediate(modifier); }
        }
        [Test] public void EventsReconcilePreStartStacksRefreshAndIndependentSourceGroups()
        {
            var go=new GameObject("HLObserver");var host=new GameObject("HLSink");var f=ScriptableObject.CreateInstance<BuffHandlerFactory>();var m=ScriptableObject.CreateInstance<FlatModifierFactory>();
            try
            {
                m.data=new FlatModifierData {value=1};f.data=new BuffHandlerData {durationType=DurationType.Duration,duration=4,buffFactoryList=new List<ABuffFactory>{m}};
                var manager=go.AddComponent<BuffManager>();var observer=go.AddComponent<HLStatusObserver>();var sink=host.AddComponent<HLSpellVisualSink>();observer.Bind(manager,sink);
                var data=new BuffManager.BuffHandlerData {target=go,buffHandlerFactory=f,buffHandler=new BuffHandler {data=f.data},refreshStacks=1};
                manager.OnBuffHandlerStarted.Invoke(data);Assert.AreEqual(1,sink.GetStatus(go,f).GetComponentInChildren<HLSpellEffect>().Stacks);
                data.currentStacks=3;data.refreshStacks=0;((BuffHandler)data.buffHandler).durationTimer=2;observer.Reconcile();Assert.AreEqual(3,sink.GetStatus(go,f).GetComponentInChildren<HLSpellEffect>().Stacks);
                var second=new BuffManager.BuffHandlerData {target=go,buffHandlerFactory=f,buffHandler=new BuffHandler {data=f.data},currentStacks=2};manager.OnBuffHandlerStarted.Invoke(second);Assert.AreEqual(5,sink.GetStatus(go,f).GetComponentInChildren<HLSpellEffect>().Stacks);
                manager.OnBuffHandlerStopped.Invoke(data);Assert.AreEqual(1,sink.StatusCount);manager.OnBuffHandlerStopped.Invoke(second);Assert.AreEqual(0,sink.StatusCount);
                observer.Detach();manager.OnBuffHandlerStarted.Invoke(data);Assert.AreEqual(0,sink.StatusCount);
            }finally{DestroyHost(go);DestroyHost(host);Object.DestroyImmediate(f);Object.DestroyImmediate(m);}
        }
    }
}
