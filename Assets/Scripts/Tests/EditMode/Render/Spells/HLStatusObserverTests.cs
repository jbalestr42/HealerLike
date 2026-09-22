using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLStatusObserverTests
    {
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
            }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(host);Object.DestroyImmediate(f);Object.DestroyImmediate(m);}
        }
    }
}
