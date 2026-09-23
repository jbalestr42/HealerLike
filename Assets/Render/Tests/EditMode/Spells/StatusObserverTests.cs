using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class StatusObserverTests
{
    static void DestroyHost(GameObject go)
    {
        if (!go)
        {
            return;
        }
        foreach (SpellVisualSink sink in go.GetComponentsInChildren<SpellVisualSink>(true))
        {
            TestHelpers.InvokePrivate(sink, "OnDestroy");
        }
        Object.DestroyImmediate(go);
    }

    class SinkSpy : ISpellVisualSink
    {
        public int calls;

        public void SetStatus(
            GameObject source,
            GameObject target,
            ABuffHandlerFactory factory,
            int stacks,
            float elapsed,
            float duration,
            ClockKind clock
        )
        {
            calls++;
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory) { }

        public void ShowImpact(
            GameObject source,
            GameObject target,
            ResourceKind resource,
            float amount,
            bool critical
        ) { }

        public void PulseArea(
            Vector3 center,
            float radius,
            HealerLike.Render.Zones.ZoneKind kind,
            float strength
        ) { }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IdleReconciliationAllocatesNothingAndDoesNotRepublish(bool populated)
    {
        GameObject go = new GameObject("Observer");
        BuffHandlerFactory factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        try
        {
            factory.data = new BuffHandlerData { durationType = DurationType.Infinite };
            BuffManager manager = go.AddComponent<BuffManager>();
            StatusObserver observer = go.AddComponent<StatusObserver>();
            SinkSpy sink = new SinkSpy();
            observer.Bind(manager, sink);
            BuffManager.BuffHandlerData data = new BuffManager.BuffHandlerData
            {
                target = go,
                buffHandlerFactory = factory,
                currentStacks = 1
            };
            if (populated)
            {
                manager.OnBuffHandlerStarted.Invoke(data);
            }
            for (int i = 0; i < 32; i++)
            {
                observer.Reconcile();
            }
            int calls = sink.calls;
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 32; i++)
            {
                observer.Reconcile();
            }
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, allocated);
            Assert.AreEqual(calls, sink.calls);
            if (populated)
            {
                data.currentStacks = 2;
                observer.Reconcile();
                Assert.AreEqual(calls + 1, sink.calls);
            }
        }
        finally
        {
            DestroyHost(go);
            Object.DestroyImmediate(factory);
        }
    }

    [Test]
    public void UnchangedStatusReturnsWhenTheSinkIsReenabled()
    {
        GameObject go = new GameObject("Observer");
        GameObject host = new GameObject("Sink");
        BuffHandlerFactory factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
        try
        {
            modifier.data = new FlatModifierData { value = 1f };
            factory.data = new BuffHandlerData
            {
                durationType = DurationType.Infinite,
                buffFactoryList = new List<ABuffFactory> { modifier }
            };
            BuffManager manager = go.AddComponent<BuffManager>();
            StatusObserver observer = go.AddComponent<StatusObserver>();
            SpellVisualSink sink = host.AddComponent<SpellVisualSink>();
            sink.looks = UnityEditor.AssetDatabase.LoadAssetAtPath<SpellLooks>(
                "Assets/Render/Spells/Data/SpellLooks.asset"
            );
            observer.Bind(manager, sink);
            manager.OnBuffHandlerStarted.Invoke(
                new BuffManager.BuffHandlerData
                {
                    target = go,
                    buffHandlerFactory = factory,
                    currentStacks = 1
                }
            );
            Assert.AreEqual(1, sink.statusCount);
            sink.enabled = false;
            TestHelpers.InvokePrivate(sink, "OnDisable");
            observer.Reconcile();
            Assert.AreEqual(0, sink.statusCount);
            sink.enabled = true;
            TestHelpers.InvokePrivate(sink, "OnEnable");
            observer.Reconcile();
            Assert.AreEqual(1, sink.statusCount);
            sink.Clear();
            observer.Reconcile();
            Assert.AreEqual(1, sink.statusCount);
        }
        finally
        {
            DestroyHost(go);
            DestroyHost(host);
            Object.DestroyImmediate(factory);
            Object.DestroyImmediate(modifier);
        }
    }

    [Test]
    public void EventsReconcilePreStartStacksRefreshAndIndependentSourceGroups()
    {
        GameObject go = new GameObject("Observer");
        GameObject host = new GameObject("Sink");
        BuffHandlerFactory f = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        FlatModifierFactory m = ScriptableObject.CreateInstance<FlatModifierFactory>();
        try
        {
            m.data = new FlatModifierData { value = 1f };
            f.data = new BuffHandlerData
            {
                durationType = DurationType.Duration,
                duration = 4f,
                buffFactoryList = new List<ABuffFactory> { m }
            };
            BuffManager manager = go.AddComponent<BuffManager>();
            StatusObserver observer = go.AddComponent<StatusObserver>();
            SpellVisualSink sink = host.AddComponent<SpellVisualSink>();
            sink.looks = UnityEditor.AssetDatabase.LoadAssetAtPath<SpellLooks>(
                "Assets/Render/Spells/Data/SpellLooks.asset"
            );
            observer.Bind(manager, sink);
            BuffManager.BuffHandlerData data = new BuffManager.BuffHandlerData
            {
                target = go,
                buffHandlerFactory = f,
                buffHandler = new BuffHandler { data = f.data },
                refreshStacks = 1
            };
            manager.OnBuffHandlerStarted.Invoke(data);
            Assert.AreEqual(1, sink.GetStatus(go, f).GetComponent<SpellEffect>().stacks);
            data.currentStacks = 3;
            data.refreshStacks = 0;
            ((BuffHandler)data.buffHandler).durationTimer = 2f;
            observer.Reconcile();
            Assert.AreEqual(3, sink.GetStatus(go, f).GetComponent<SpellEffect>().stacks);
            BuffManager.BuffHandlerData second = new BuffManager.BuffHandlerData
            {
                target = go,
                buffHandlerFactory = f,
                buffHandler = new BuffHandler { data = f.data },
                currentStacks = 2
            };
            manager.OnBuffHandlerStarted.Invoke(second);
            Assert.AreEqual(5, sink.GetStatus(go, f).GetComponent<SpellEffect>().stacks);
            manager.OnBuffHandlerStopped.Invoke(data);
            Assert.AreEqual(1, sink.statusCount);
            manager.OnBuffHandlerStopped.Invoke(second);
            Assert.AreEqual(0, sink.statusCount);
            observer.Detach();
            manager.OnBuffHandlerStarted.Invoke(data);
            Assert.AreEqual(0, sink.statusCount);
        }
        finally
        {
            DestroyHost(go);
            DestroyHost(host);
            Object.DestroyImmediate(f);
            Object.DestroyImmediate(m);
        }
    }

    [Test]
    public void Init_Manager_PublishesToTheManagerSink()
    {
        GameObject go = new GameObject("Observed");
        GameObject managerGo = new GameObject("RenderManager");
        GameObject sinkGo = new GameObject("Sink");
        BuffHandlerFactory factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        try
        {
            factory.data = new BuffHandlerData { durationType = DurationType.Infinite };
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
            BuffManager manager = go.GetComponent<BuffManager>();
            if (manager == null)
            {
                manager = go.AddComponent<BuffManager>();
            }
            HealerLike.Render.Stage.RenderManager renderManager =
                managerGo.AddComponent<HealerLike.Render.Stage.RenderManager>();
            sinkGo.transform.SetParent(managerGo.transform);
            SpellVisualSink sink = sinkGo.AddComponent<SpellVisualSink>();
            sink.looks = UnityEditor.AssetDatabase.LoadAssetAtPath<SpellLooks>(
                "Assets/Render/Spells/Data/SpellLooks.asset"
            );
            TestHelpers.SetPrivateField(renderManager, "_spellSink", sink);
            StatusObserver observer = go.AddComponent<StatusObserver>();

            observer.Init(entity, renderManager);
            manager.OnBuffHandlerStarted.Invoke(
                new BuffManager.BuffHandlerData { target = go, buffHandlerFactory = factory, currentStacks = 1 }
            );

            Assert.AreEqual(1, sink.statusCount);
            Assert.IsNotNull(go.GetComponent<AttributeShieldView>());
        }
        finally
        {
            DestroyHost(go);
            DestroyHost(managerGo);
            Object.DestroyImmediate(factory);
        }
    }
}

}
