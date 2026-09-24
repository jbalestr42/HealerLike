using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render
{

public class RecordingHealSink : IHealthVisualSink
{
    public readonly List<(GameObject target, float value, bool critical)> calls =
        new List<(GameObject, float, bool)>();

    public void OnHealthResolved(GameObject target, float value, bool critical)
    {
        calls.Add((target, value, critical));
    }
}

public class SelfUnregisteringHealSink : IHealthVisualSink
{
    readonly RenderRegistry _registry;
    readonly GameObject _source;
    public int callCount;

    public SelfUnregisteringHealSink(RenderRegistry registry, GameObject source)
    {
        _registry = registry;
        _source = source;
    }

    public void OnHealthResolved(GameObject target, float value, bool critical)
    {
        callCount++;
        _registry.Unregister(_source, this);
    }
}

public class RenderRegistryTests
{
    class CallbackSink : IHealthVisualSink
    {
        public Action callback;

        public void OnHealthResolved(GameObject target, float value, bool critical)
        {
            callback();
        }
    }

    RenderRegistry _registry;
    readonly List<GameObject> _objects = new List<GameObject>();

    GameObject NewObject(string name)
    {
        GameObject go = new GameObject(name);
        _objects.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        _registry = new RenderRegistry();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        _objects.Clear();
    }

    [Test]
    public void NotifyHealth_RegisteredSink_ReceivesResolvedHeal()
    {
        GameObject healer = NewObject("healer");
        GameObject target = NewObject("target");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);

        _registry.NotifyHealth(healer, target, 12.5f, true);

        Assert.AreEqual(1, sink.calls.Count);
        Assert.AreSame(target, sink.calls[0].target);
        Assert.AreEqual(12.5f, sink.calls[0].value);
        Assert.IsTrue(sink.calls[0].critical);
    }

    [Test]
    public void Register_SameSinkTwice_NotifiesOnce()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);
        _registry.Register(healer, sink);

        _registry.NotifyHealth(healer, NewObject("target"), 1f, false);

        Assert.AreEqual(1, sink.calls.Count, "a re-initialised component must not double its visuals");
    }

    [Test]
    public void NotifyHealth_TwoSinksOnSource_NotifiesBoth()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink first = new RecordingHealSink();
        RecordingHealSink second = new RecordingHealSink();
        _registry.Register(healer, first);
        _registry.Register(healer, second);

        _registry.NotifyHealth(healer, NewObject("target"), 3f, false);

        Assert.AreEqual(1, first.calls.Count);
        Assert.AreEqual(1, second.calls.Count);
    }

    [Test]
    public void NotifyHealth_OtherSource_LeavesSinkSilent()
    {
        GameObject healer = NewObject("healer");
        GameObject otherHealer = NewObject("otherHealer");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);

        _registry.NotifyHealth(otherHealer, NewObject("target"), 1f, false);

        Assert.AreEqual(0, sink.calls.Count);
    }

    [Test]
    public void Unregister_RegisteredSink_StopsNotifications()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);
        _registry.Unregister(healer, sink);

        _registry.NotifyHealth(healer, NewObject("target"), 1f, false);

        Assert.AreEqual(0, sink.calls.Count);
    }

    [Test]
    public void Unregister_OneOfTwoSinks_KeepsTheOther()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink kept = new RecordingHealSink();
        RecordingHealSink dropped = new RecordingHealSink();
        _registry.Register(healer, kept);
        _registry.Register(healer, dropped);

        _registry.Unregister(healer, dropped);
        _registry.NotifyHealth(healer, NewObject("target"), 1f, false);

        Assert.AreEqual(1, kept.calls.Count);
        Assert.AreEqual(0, dropped.calls.Count);
    }

    [Test]
    public void RegisterAndNotify_NullArguments_DoNotThrow()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink sink = new RecordingHealSink();

        Assert.DoesNotThrow(() => _registry.Register(null, sink));
        Assert.DoesNotThrow(() => _registry.Register(healer, null));
        Assert.DoesNotThrow(() => _registry.Unregister(null, sink));
        Assert.DoesNotThrow(() => _registry.Unregister(healer, null));
        Assert.DoesNotThrow(() => _registry.Unregister(healer, sink), "never registered");
        Assert.DoesNotThrow(() => _registry.NotifyHealth(null, null, 1f, false));
        Assert.DoesNotThrow(() => _registry.NotifyHealth(healer, null, 1f, false), "no sink for this source");
    }

    [Test]
    public void NotifyHealth_NullTarget_ForwardsNull()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);

        _registry.NotifyHealth(healer, null, 2f, false);

        Assert.AreEqual(1, sink.calls.Count);
        Assert.IsNull(sink.calls[0].target);
    }

    [Test]
    public void NotifyHealth_SinkUnregistersItself_CompletesSweep()
    {
        GameObject healer = NewObject("healer");
        SelfUnregisteringHealSink selfRemoving = new SelfUnregisteringHealSink(_registry, healer);
        _registry.Register(healer, selfRemoving);

        Assert.DoesNotThrow(() => _registry.NotifyHealth(healer, NewObject("target"), 1f, false));
        Assert.AreEqual(1, selfRemoving.callCount);

        _registry.NotifyHealth(healer, NewObject("target"), 1f, false);
        Assert.AreEqual(1, selfRemoving.callCount, "it really did leave");
    }

    [Test]
    public void NotifyHealth_Nested_UsesIndependentSnapshot()
    {
        GameObject source = NewObject("Source");
        List<string> calls = new List<string>();
        bool nested = false;
        CallbackSink a = new CallbackSink { callback = () => calls.Add("A") };
        CallbackSink b = new CallbackSink();
        b.callback = () =>
        {
            calls.Add("B");
            if (nested)
            {
                return;
            }

            nested = true;
            _registry.Unregister(source, a);
            _registry.NotifyHealth(source, null, 1, false);
        };
        _registry.Register(source, b);
        _registry.Register(source, a);

        _registry.NotifyHealth(source, null, 1, false);

        CollectionAssert.AreEqual(new[] { "B", "B", "A" }, calls);
    }

    [Test]
    public void Unregister_DestroyedSource_LaterNotifyReachesNothing()
    {
        GameObject source = NewObject("Source");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(source, sink);
        UnityEngine.Object.DestroyImmediate(source);

        _registry.Unregister(source, sink);
        _registry.NotifyHealth(source, NewObject("target"), 1f, false);

        Assert.AreEqual(0, sink.calls.Count);
    }
}

}
