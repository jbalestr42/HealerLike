using System.Collections.Generic;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class StatusObserverTests
{
    class SinkSpy : ISpellVisualSink
    {
        public int calls;
        public GameObject lastSource;

        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
            float elapsed, float duration, ClockKind clock)
        {
            calls++;
            lastSource = source;
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
        }

        public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float amount,
            bool critical)
        {
        }

        public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
        {
        }
    }

    GameObject _go;
    GameObject _sinkGo;
    GameObject _managerGo;
    BuffHandlerFactory _factory;
    FlatModifierFactory _modifier;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Observer");
        _sinkGo = new GameObject("Sink");
        _managerGo = new GameObject("RenderManager");
        _modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
        _modifier.data = new FlatModifierData { value = 1f };
        _factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
    }

    [TearDown]
    public void TearDown()
    {
        DestroyHost(_go);
        DestroyHost(_sinkGo);
        DestroyHost(_managerGo);
        Object.DestroyImmediate(_factory);
        Object.DestroyImmediate(_modifier);
    }

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

    SpellVisualSink CreateSink()
    {
        return SpellSinkFixture.Add(_sinkGo);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Reconcile_Idle_AllocatesNothingAndDoesNotRepublish(bool populated)
    {
        _factory.data = new BuffHandlerData { durationType = DurationType.Infinite };
        BuffManager manager = _go.AddComponent<BuffManager>();
        StatusObserver observer = _go.AddComponent<StatusObserver>();
        SinkSpy sink = new SinkSpy();
        observer.Bind(manager, sink);
        BuffManager.BuffHandlerData data = new BuffManager.BuffHandlerData
        {
            target = _go,
            buffHandlerFactory = _factory,
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

    [Test]
    public void Reconcile_HandlerWithASource_PassesTheCasterToTheSink()
    {
        _factory.data = new BuffHandlerData { durationType = DurationType.Infinite };
        BuffManager manager = _go.AddComponent<BuffManager>();
        StatusObserver observer = _go.AddComponent<StatusObserver>();
        SinkSpy sink = new SinkSpy();
        observer.Bind(manager, sink);
        manager.OnBuffHandlerStarted.Invoke(new BuffManager.BuffHandlerData
        {
            target = _go,
            source = _managerGo,
            buffHandlerFactory = _factory,
            currentStacks = 1
        });

        observer.Reconcile();

        Assert.AreSame(_managerGo, sink.lastSource);
    }

    [Test]
    public void Reconcile_SinkReenabled_RestoresUnchangedStatus()
    {
        _factory.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory> { _modifier }
        };
        BuffManager manager = _go.AddComponent<BuffManager>();
        StatusObserver observer = _go.AddComponent<StatusObserver>();
        SpellVisualSink sink = CreateSink();
        observer.Bind(manager, sink);
        manager.OnBuffHandlerStarted.Invoke(new BuffManager.BuffHandlerData
        {
            target = _go,
            buffHandlerFactory = _factory,
            currentStacks = 1
        });
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

    [Test]
    public void Reconcile_StackEventsFromTwoHandlers_SumIntoOneStatusUntilBothStop()
    {
        _factory.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 4f,
            buffFactoryList = new List<ABuffFactory> { _modifier }
        };
        BuffManager manager = _go.AddComponent<BuffManager>();
        StatusObserver observer = _go.AddComponent<StatusObserver>();
        SpellVisualSink sink = CreateSink();
        observer.Bind(manager, sink);
        BuffManager.BuffHandlerData data = new BuffManager.BuffHandlerData
        {
            target = _go,
            buffHandlerFactory = _factory,
            buffHandler = new BuffHandler { data = _factory.data },
            refreshStacks = 1
        };

        manager.OnBuffHandlerStarted.Invoke(data);
        Assert.AreEqual(1, sink.GetStatus(_go, _factory).GetComponent<SpellEffect>().stacks);

        data.currentStacks = 3;
        data.refreshStacks = 0;
        ((BuffHandler)data.buffHandler).durationTimer = 2f;
        observer.Reconcile();
        Assert.AreEqual(3, sink.GetStatus(_go, _factory).GetComponent<SpellEffect>().stacks);

        BuffManager.BuffHandlerData second = new BuffManager.BuffHandlerData
        {
            target = _go,
            buffHandlerFactory = _factory,
            buffHandler = new BuffHandler { data = _factory.data },
            currentStacks = 2
        };
        manager.OnBuffHandlerStarted.Invoke(second);
        Assert.AreEqual(5, sink.GetStatus(_go, _factory).GetComponent<SpellEffect>().stacks);

        manager.OnBuffHandlerStopped.Invoke(data);
        Assert.AreEqual(1, sink.statusCount);
        manager.OnBuffHandlerStopped.Invoke(second);
        Assert.AreEqual(0, sink.statusCount);

        observer.Detach();
        manager.OnBuffHandlerStarted.Invoke(data);
        Assert.AreEqual(0, sink.statusCount);
    }

    [Test]
    public void Init_Manager_PublishesToTheManagerSink()
    {
        _factory.data = new BuffHandlerData { durationType = DurationType.Infinite };
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = _go.AddComponent<Entity>());
        BuffManager manager = _go.GetComponent<BuffManager>();
        if (manager == null)
        {
            manager = _go.AddComponent<BuffManager>();
        }
        RenderManager renderManager = _managerGo.AddComponent<RenderManager>();
        _sinkGo.transform.SetParent(_managerGo.transform);
        SpellVisualSink sink = CreateSink();
        TestHelpers.SetPrivateField(renderManager, "_spellSink", sink);
        StatusObserver observer = _go.AddComponent<StatusObserver>();

        observer.Init(entity, renderManager);
        BuffManager.BuffHandlerData data = new BuffManager.BuffHandlerData
        {
            target = _go,
            buffHandlerFactory = _factory,
            currentStacks = 1
        };
        manager.OnBuffHandlerStarted.Invoke(data);

        Assert.AreEqual(1, sink.statusCount);
        Assert.IsNotNull(_go.GetComponent<AttributeShieldView>());
    }
}

}
