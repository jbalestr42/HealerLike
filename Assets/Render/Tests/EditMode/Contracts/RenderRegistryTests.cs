using System;
using System.Collections.Generic;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render
{

public class RecordingHealSink : IHealVisualSink
{
    public readonly List<(GameObject target, float value, bool critical)> calls =
        new List<(GameObject, float, bool)>();

    public void OnHealResolved(GameObject target, float value, bool critical)
    {
        calls.Add((target, value, critical));
    }
}

public class SelfUnregisteringHealSink : IHealVisualSink
{
    readonly RenderRegistry _registry;
    readonly GameObject _source;
    public int callCount;

    public SelfUnregisteringHealSink(RenderRegistry registry, GameObject source)
    {
        _registry = registry;
        _source = source;
    }

    public void OnHealResolved(GameObject target, float value, bool critical)
    {
        callCount++;
        _registry.Unregister(_source, this);
    }
}

public class RecordingSpellSink : ISpellVisualSink
{
    public int impactCount;
    public int statusCount;
    public int removeCount;
    public int pulseCount;
    public ResourceKind lastResource;
    public float lastAmount;
    public ClockKind lastClock;
    public ZoneKind lastZoneKind;

    public void ShowImpact(GameObject source, GameObject target, ResourceKind resource,
        float preClampAmount, bool isCritical)
    {
        impactCount++;
        lastResource = resource;
        lastAmount = preClampAmount;
    }

    public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
        float elapsedSeconds, float durationSeconds, ClockKind clock)
    {
        statusCount++;
        lastClock = clock;
    }

    public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
    {
        removeCount++;
    }

    public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
    {
        pulseCount++;
        lastZoneKind = kind;
    }
}

public class RenderRegistryTests
{
    class CallbackSink : IHealVisualSink
    {
        public Action callback;

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            callback();
        }
    }

    class RecordingZoneOwner : IZoneOwner
    {
        public int calls;
        public float lastSeconds;

        public int AddPulse(ZoneKind kind, Vector3 center, float radius, float strength, float seconds)
        {
            calls++;
            lastSeconds = seconds;
            return calls;
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
    public void SpellSink_NewRegistry_IsNull()
    {
        Assert.IsNull(_registry.spellSink);
    }

    [Test]
    public void SpellSink_SetThenCleared_RoundTrips()
    {
        RecordingSpellSink sink = new RecordingSpellSink();

        _registry.spellSink = sink;
        Assert.AreSame(sink, _registry.spellSink);

        _registry.spellSink = null;
        Assert.IsNull(_registry.spellSink, "clearing the sink silences spell visuals");
    }

    [Test]
    public void Init_SinkAndZoneOwner_SetsBoth()
    {
        RecordingSpellSink sink = new RecordingSpellSink();
        RecordingZoneOwner owner = new RecordingZoneOwner();

        _registry.Init(sink, owner);

        Assert.AreSame(sink, _registry.spellSink);
        Assert.AreSame(owner, _registry.zoneOwner);
    }

    [Test]
    public void NotifyHeal_RegisteredSink_ReceivesResolvedHeal()
    {
        GameObject healer = NewObject("healer");
        GameObject target = NewObject("target");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);

        _registry.NotifyHeal(healer, target, 12.5f, true);

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

        _registry.NotifyHeal(healer, NewObject("target"), 1f, false);

        Assert.AreEqual(1, sink.calls.Count, "a re-initialised component must not double its visuals");
    }

    [Test]
    public void NotifyHeal_TwoSinksOnSource_NotifiesBoth()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink first = new RecordingHealSink();
        RecordingHealSink second = new RecordingHealSink();
        _registry.Register(healer, first);
        _registry.Register(healer, second);

        _registry.NotifyHeal(healer, NewObject("target"), 3f, false);

        Assert.AreEqual(1, first.calls.Count);
        Assert.AreEqual(1, second.calls.Count);
    }

    [Test]
    public void NotifyHeal_OtherSource_LeavesSinkSilent()
    {
        GameObject healer = NewObject("healer");
        GameObject otherHealer = NewObject("otherHealer");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);

        _registry.NotifyHeal(otherHealer, NewObject("target"), 1f, false);

        Assert.AreEqual(0, sink.calls.Count);
    }

    [Test]
    public void Unregister_RegisteredSink_StopsNotifications()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);
        _registry.Unregister(healer, sink);

        _registry.NotifyHeal(healer, NewObject("target"), 1f, false);

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
        _registry.NotifyHeal(healer, NewObject("target"), 1f, false);

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
        Assert.DoesNotThrow(() => _registry.NotifyHeal(null, null, 1f, false));
        Assert.DoesNotThrow(() => _registry.NotifyHeal(healer, null, 1f, false), "no sink for this source");
    }

    [Test]
    public void NotifyHeal_NullTarget_ForwardsNull()
    {
        GameObject healer = NewObject("healer");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(healer, sink);

        _registry.NotifyHeal(healer, null, 2f, false);

        Assert.AreEqual(1, sink.calls.Count);
        Assert.IsNull(sink.calls[0].target);
    }

    [Test]
    public void NotifyHeal_SinkUnregistersItself_CompletesSweep()
    {
        GameObject healer = NewObject("healer");
        SelfUnregisteringHealSink selfRemoving = new SelfUnregisteringHealSink(_registry, healer);
        _registry.Register(healer, selfRemoving);

        Assert.DoesNotThrow(() => _registry.NotifyHeal(healer, NewObject("target"), 1f, false));
        Assert.AreEqual(1, selfRemoving.callCount);

        _registry.NotifyHeal(healer, NewObject("target"), 1f, false);
        Assert.AreEqual(1, selfRemoving.callCount, "it really did leave");
    }

    [Test]
    public void NotifyHeal_Nested_UsesIndependentSnapshot()
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
            _registry.NotifyHeal(source, null, 1, false);
        };
        _registry.Register(source, b);
        _registry.Register(source, a);

        _registry.NotifyHeal(source, null, 1, false);

        CollectionAssert.AreEqual(new[] { "B", "B", "A" }, calls);
    }

    [Test]
    public void Unregister_DestroyedSource_RemovesLastRegistration()
    {
        GameObject source = NewObject("Source");
        RecordingHealSink sink = new RecordingHealSink();
        _registry.Register(source, sink);
        UnityEngine.Object.DestroyImmediate(source);

        _registry.Unregister(source, sink);

        System.Reflection.FieldInfo field = typeof(RenderRegistry).GetField("_healSinks",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.AreEqual(0, ((System.Collections.IDictionary)field.GetValue(_registry)).Count);
    }

    [Test]
    public void SpellSink_TestAssemblyImplementation_ReceivesEveryCall()
    {
        RecordingSpellSink sink = new RecordingSpellSink();
        _registry.spellSink = sink;

        _registry.spellSink.ShowImpact(null, null, ResourceKind.Health, -7f, true);
        _registry.spellSink.SetStatus(null, null, null, 2, 0.5f, 3f, ClockKind.Simulation);
        _registry.spellSink.RemoveStatus(null, null, null);
        _registry.spellSink.PulseArea(Vector3.zero, 1.5f, ZoneKind.Hostile, 0.5f);

        Assert.AreEqual(1, sink.impactCount);
        Assert.AreEqual(ResourceKind.Health, sink.lastResource);
        Assert.AreEqual(-7f, sink.lastAmount, "the pre-clamp delta keeps its sign");
        Assert.AreEqual(1, sink.statusCount);
        Assert.AreEqual(ClockKind.Simulation, sink.lastClock);
        Assert.AreEqual(1, sink.removeCount);
        Assert.AreEqual(1, sink.pulseCount);
        Assert.AreEqual(ZoneKind.Hostile, sink.lastZoneKind);
    }

    [Test]
    public void ZoneOwner_SetThenCleared_RoundTrips()
    {
        RenderRegistry registry = new RenderRegistry();
        Assert.IsNull(registry.zoneOwner);
        RecordingZoneOwner owner = new RecordingZoneOwner();

        registry.zoneOwner = owner;

        Assert.AreSame(owner, registry.zoneOwner);
        Assert.AreEqual(1, registry.zoneOwner.AddPulse(ZoneKind.Heal, Vector3.zero, 1, 1, 0.8f));

        registry.zoneOwner = null;
        Assert.IsNull(registry.zoneOwner);
    }
}

}
