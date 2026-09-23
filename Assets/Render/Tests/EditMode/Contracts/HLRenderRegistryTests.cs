using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;

namespace HealerLike.Render
{
    public class RecordingHealSink : IHLHealVisualSink
    {
        public readonly List<(GameObject target, float value, bool critical)> calls =
            new List<(GameObject, float, bool)>();

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            calls.Add((target, value, critical));
        }
    }

    public class SelfUnregisteringHealSink : IHLHealVisualSink
    {
        readonly HLRenderRegistry _registry;
        readonly GameObject _source;
        public int callCount;

        public SelfUnregisteringHealSink(HLRenderRegistry registry, GameObject source)
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

    public class RecordingSpellSink : IHLSpellVisualSink
    {
        public int impactCount;
        public int statusCount;
        public int removeCount;
        public int pulseCount;
        public HLResourceKind lastResource;
        public float lastAmount;
        public HLClockKind lastClock;
        public HLZoneKind lastZoneKind;

        public void ShowImpact(GameObject source, GameObject target, HLResourceKind resource,
            float preClampAmount, bool isCritical)
        {
            impactCount++;
            lastResource = resource;
            lastAmount = preClampAmount;
        }

        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
            float elapsedSeconds, float durationSeconds, HLClockKind clock)
        {
            statusCount++;
            lastClock = clock;
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            removeCount++;
        }

        public void PulseArea(Vector3 center, float radius, HLZoneKind kind, float strength)
        {
            pulseCount++;
            lastZoneKind = kind;
        }
    }

    public class HLRenderRegistryTests
    {
        HLRenderRegistry _registry;
        readonly List<GameObject> _objects = new List<GameObject>();
        HLRenderRegistry _previousCurrent;

        GameObject NewObject(string name)
        {
            GameObject go = new GameObject(name);
            _objects.Add(go);
            return go;
        }

        [SetUp]
        public void SetUp()
        {
            _previousCurrent = HLRenderRegistry.current;
            _registry = new HLRenderRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            HLRenderRegistry.current = _previousCurrent;
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
        public void ANewRegistryHasNoSpellSink()
        {
            Assert.IsNull(_registry.spellSink);
        }

        [Test]
        public void SpellSinkRoundTripsAndClears()
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
        public void CurrentRoundTripsAndClears()
        {
            HLRenderRegistry.current = _registry;
            Assert.AreSame(_registry, HLRenderRegistry.current);

            HLRenderRegistry.current = null;
            Assert.IsNull(HLRenderRegistry.current, "the bootstrap clears Current on disable");
        }

        [Test]
        public void RegisteredSinkReceivesTheResolvedHeal()
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
        public void RegisteringTheSameSinkTwiceStillNotifiesOnce()
        {
            GameObject healer = NewObject("healer");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);
            _registry.Register(healer, sink);

            _registry.NotifyHeal(healer, NewObject("target"), 1f, false);

            Assert.AreEqual(1, sink.calls.Count, "a re-initialised component must not double its visuals");
        }

        [Test]
        public void EverySinkOfTheSameSourceIsNotified()
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
        public void SinksAreKeyedBySourceSoOtherHealersStaySilent()
        {
            GameObject healer = NewObject("healer");
            GameObject otherHealer = NewObject("otherHealer");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);

            _registry.NotifyHeal(otherHealer, NewObject("target"), 1f, false);

            Assert.AreEqual(0, sink.calls.Count);
        }

        [Test]
        public void UnregisterStopsTheNotifications()
        {
            GameObject healer = NewObject("healer");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);
            _registry.Unregister(healer, sink);

            _registry.NotifyHeal(healer, NewObject("target"), 1f, false);

            Assert.AreEqual(0, sink.calls.Count);
        }

        [Test]
        public void UnregisterLeavesTheOtherSinksOfTheSameSource()
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
        public void NullArgumentsAreIgnoredRatherThanThrowing()
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
        public void NotifyHealForwardsANullTargetUntouched()
        {
            GameObject healer = NewObject("healer");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);

            _registry.NotifyHeal(healer, null, 2f, false);

            Assert.AreEqual(1, sink.calls.Count);
            Assert.IsNull(sink.calls[0].target);
        }

        [Test]
        public void ASinkThatUnregistersItselfWhileNotifiedDoesNotBreakTheSweep()
        {
            GameObject healer = NewObject("healer");
            SelfUnregisteringHealSink selfRemoving = new SelfUnregisteringHealSink(_registry, healer);
            _registry.Register(healer, selfRemoving);

            Assert.DoesNotThrow(() => _registry.NotifyHeal(healer, NewObject("target"), 1f, false));
            Assert.AreEqual(1, selfRemoving.callCount);

            _registry.NotifyHeal(healer, NewObject("target"), 1f, false);
            Assert.AreEqual(1, selfRemoving.callCount, "it really did leave");
        }

        class HLCallbackSink : IHLHealVisualSink
        {
            public Action callback;

            public void OnHealResolved(GameObject target, float value, bool critical)
            {
                callback();
            }
        }

        [Test]
        public void RemovingEarlierSinkUsesStableNewestFirstSnapshot()
        {
            GameObject source = NewObject("HLSource");
            List<string> calls = new List<string>();
            HLCallbackSink a = new HLCallbackSink { callback = () => calls.Add("A") };
            HLCallbackSink b = new HLCallbackSink { callback = () => calls.Add("B") };
            HLCallbackSink c = new HLCallbackSink();
            c.callback = () =>
            {
                calls.Add("C");
                _registry.Unregister(source, a);
            };
            _registry.Register(source, a);
            _registry.Register(source, b);
            _registry.Register(source, c);

            _registry.NotifyHeal(source, null, 1, false);

            CollectionAssert.AreEqual(new[] { "C", "B", "A" }, calls);

            calls.Clear();
            _registry.NotifyHeal(source, null, 1, false);
            CollectionAssert.AreEqual(new[] { "C", "B" }, calls);
        }

        [Test]
        public void NestedNotificationHasIndependentSnapshot()
        {
            GameObject source = NewObject("HLSource");
            List<string> calls = new List<string>();
            bool nested = false;
            HLCallbackSink a = new HLCallbackSink { callback = () => calls.Add("A") };
            HLCallbackSink b = new HLCallbackSink();
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
            _registry.Register(source, a);
            _registry.Register(source, b);

            _registry.NotifyHeal(source, null, 1, false);

            CollectionAssert.AreEqual(new[] { "B", "B", "A" }, calls);
        }

        [Test]
        public void DestroyedSourceCanRemoveItsLastRegistration()
        {
            GameObject source = NewObject("HLSource");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(source, sink);
            UnityEngine.Object.DestroyImmediate(source);

            _registry.Unregister(source, sink);

            System.Reflection.FieldInfo field = typeof(HLRenderRegistry).GetField("_healSinks",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.AreEqual(0, ((System.Collections.IDictionary)field.GetValue(_registry)).Count);
        }

        [Test]
        public void TheSpellSinkContractIsImplementableFromAnotherAssembly()
        {
            RecordingSpellSink sink = new RecordingSpellSink();
            _registry.spellSink = sink;

            _registry.spellSink.ShowImpact(null, null, HLResourceKind.Health, -7f, true);
            _registry.spellSink.SetStatus(null, null, null, 2, 0.5f, 3f, HLClockKind.Simulation);
            _registry.spellSink.RemoveStatus(null, null, null);
            _registry.spellSink.PulseArea(Vector3.zero, 1.5f, HLZoneKind.Hostile, 0.5f);

            Assert.AreEqual(1, sink.impactCount);
            Assert.AreEqual(HLResourceKind.Health, sink.lastResource);
            Assert.AreEqual(-7f, sink.lastAmount, "the pre-clamp delta keeps its sign");
            Assert.AreEqual(1, sink.statusCount);
            Assert.AreEqual(HLClockKind.Simulation, sink.lastClock);
            Assert.AreEqual(1, sink.removeCount);
            Assert.AreEqual(1, sink.pulseCount);
            Assert.AreEqual(HLZoneKind.Hostile, sink.lastZoneKind);
        }

        class RecordingZoneOwner : IHLZoneOwner
        {
            public int calls;
            public float lastSeconds;

            public int AddPulse(HLZoneKind kind, Vector3 center, float radius, float strength, float seconds)
            {
                calls++;
                lastSeconds = seconds;
                return calls;
            }
        }

        [Test]
        public void ZoneOwner_DefaultsToNullAndRoundTrips()
        {
            HLRenderRegistry registry = new HLRenderRegistry();
            Assert.IsNull(registry.zoneOwner);
            RecordingZoneOwner owner = new RecordingZoneOwner();

            registry.zoneOwner = owner;

            Assert.AreSame(owner, registry.zoneOwner);
            Assert.AreEqual(1, registry.zoneOwner.AddPulse(HLZoneKind.Heal, Vector3.zero, 1, 1, 0.8f));

            registry.zoneOwner = null;
            Assert.IsNull(registry.zoneOwner);
        }
    }
}
