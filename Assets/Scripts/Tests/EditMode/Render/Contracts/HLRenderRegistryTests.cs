using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;

namespace HealerLike.Render
{
    /// <summary>Records what the registry forwarded, so a test can assert on target/value/critical.</summary>
    class RecordingHealSink : IHLHealVisualSink
    {
        public readonly List<(GameObject target, float value, bool critical)> Calls =
            new List<(GameObject, float, bool)>();

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            Calls.Add((target, value, critical));
        }
    }

    /// <summary>A sink that always fails, to prove one broken cosmetic does not silence the others.</summary>
    class ThrowingHealSink : IHLHealVisualSink
    {
        public int CallCount;

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            CallCount++;
            throw new InvalidOperationException("deliberate test failure");
        }
    }

    /// <summary>Unregisters itself the moment it is notified: the registry must survive that.</summary>
    class SelfUnregisteringHealSink : IHLHealVisualSink
    {
        readonly HLRenderRegistry _registry;
        readonly GameObject _source;
        public int CallCount;

        public SelfUnregisteringHealSink(HLRenderRegistry registry, GameObject source)
        {
            _registry = registry;
            _source = source;
        }

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            CallCount++;
            _registry.Unregister(_source, this);
        }
    }

    /// <summary>
    /// Proves the frozen IHLSpellVisualSink is implementable from outside the render assembly.
    /// It only records; nothing here touches gameplay.
    /// </summary>
    class RecordingSpellSink : IHLSpellVisualSink
    {
        public int ImpactCount;
        public int StatusCount;
        public int RemoveCount;
        public int PulseCount;
        public HLResourceKind LastResource;
        public float LastAmount;
        public HLClockKind LastClock;
        public HLZoneKind LastZoneKind;

        public void ShowImpact(GameObject source, GameObject target, HLResourceKind resource,
            float preClampAmount, bool isCritical)
        {
            ImpactCount++;
            LastResource = resource;
            LastAmount = preClampAmount;
        }

        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
            float elapsedSeconds, float durationSeconds, HLClockKind clock)
        {
            StatusCount++;
            LastClock = clock;
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
            RemoveCount++;
        }

        public void PulseArea(Vector3 center, float radius, HLZoneKind kind, float strength)
        {
            PulseCount++;
            LastZoneKind = kind;
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
            _previousCurrent = HLRenderRegistry.Current;
            _registry = new HLRenderRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            HLRenderRegistry.Current = _previousCurrent;
            foreach (GameObject go in _objects)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        [Test]
        public void ANewRegistryHasNoSpellSink()
        {
            Assert.IsNull(_registry.SpellSink);
        }

        [Test]
        public void SpellSinkRoundTripsAndClears()
        {
            RecordingSpellSink sink = new RecordingSpellSink();

            _registry.SpellSink = sink;
            Assert.AreSame(sink, _registry.SpellSink);

            _registry.SpellSink = null;
            Assert.IsNull(_registry.SpellSink, "clearing the sink silences spell visuals");
        }

        [Test]
        public void CurrentRoundTripsAndClears()
        {
            HLRenderRegistry.Current = _registry;
            Assert.AreSame(_registry, HLRenderRegistry.Current);

            HLRenderRegistry.Current = null;
            Assert.IsNull(HLRenderRegistry.Current, "the bootstrap clears Current on disable");
        }

        [Test]
        public void RegisteredSinkReceivesTheResolvedHeal()
        {
            GameObject healer = NewObject("healer");
            GameObject target = NewObject("target");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);

            _registry.NotifyHeal(healer, target, 12.5f, true);

            Assert.AreEqual(1, sink.Calls.Count);
            Assert.AreSame(target, sink.Calls[0].target);
            Assert.AreEqual(12.5f, sink.Calls[0].value);
            Assert.IsTrue(sink.Calls[0].critical);
        }

        [Test]
        public void RegisteringTheSameSinkTwiceStillNotifiesOnce()
        {
            GameObject healer = NewObject("healer");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);
            _registry.Register(healer, sink);

            _registry.NotifyHeal(healer, NewObject("target"), 1f, false);

            Assert.AreEqual(1, sink.Calls.Count, "a re-initialised component must not double its visuals");
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

            Assert.AreEqual(1, first.Calls.Count);
            Assert.AreEqual(1, second.Calls.Count);
        }

        [Test]
        public void SinksAreKeyedBySourceSoOtherHealersStaySilent()
        {
            GameObject healer = NewObject("healer");
            GameObject otherHealer = NewObject("otherHealer");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);

            _registry.NotifyHeal(otherHealer, NewObject("target"), 1f, false);

            Assert.AreEqual(0, sink.Calls.Count);
        }

        [Test]
        public void UnregisterStopsTheNotifications()
        {
            GameObject healer = NewObject("healer");
            RecordingHealSink sink = new RecordingHealSink();
            _registry.Register(healer, sink);
            _registry.Unregister(healer, sink);

            _registry.NotifyHeal(healer, NewObject("target"), 1f, false);

            Assert.AreEqual(0, sink.Calls.Count);
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

            Assert.AreEqual(1, kept.Calls.Count);
            Assert.AreEqual(0, dropped.Calls.Count);
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

            Assert.AreEqual(1, sink.Calls.Count);
            Assert.IsNull(sink.Calls[0].target);
        }

        [Test]
        public void AThrowingSinkDoesNotStopTheOthers()
        {
            GameObject healer = NewObject("healer");
            ThrowingHealSink broken = new ThrowingHealSink();
            RecordingHealSink healthy = new RecordingHealSink();
            _registry.Register(healer, broken);
            _registry.Register(healer, healthy);

            TestHelpers.WithLoggingDisabled(() =>
                _registry.NotifyHeal(healer, NewObject("target"), 1f, false));

            Assert.AreEqual(1, broken.CallCount);
            Assert.AreEqual(1, healthy.Calls.Count, "the healthy sink still ran");
        }

        [Test]
        public void ASinkThatUnregistersItselfWhileNotifiedDoesNotBreakTheSweep()
        {
            GameObject healer = NewObject("healer");
            SelfUnregisteringHealSink selfRemoving = new SelfUnregisteringHealSink(_registry, healer);
            _registry.Register(healer, selfRemoving);

            Assert.DoesNotThrow(() => _registry.NotifyHeal(healer, NewObject("target"), 1f, false));
            Assert.AreEqual(1, selfRemoving.CallCount);

            _registry.NotifyHeal(healer, NewObject("target"), 1f, false);
            Assert.AreEqual(1, selfRemoving.CallCount, "it really did leave");
        }

        [Test]
        public void TheSpellSinkContractIsImplementableFromAnotherAssembly()
        {
            RecordingSpellSink sink = new RecordingSpellSink();
            _registry.SpellSink = sink;

            _registry.SpellSink.ShowImpact(null, null, HLResourceKind.Health, -7f, true);
            _registry.SpellSink.SetStatus(null, null, null, 2, 0.5f, 3f, HLClockKind.Simulation);
            _registry.SpellSink.RemoveStatus(null, null, null);
            _registry.SpellSink.PulseArea(Vector3.zero, 1.5f, HLZoneKind.Hostile, 0.5f);

            Assert.AreEqual(1, sink.ImpactCount);
            Assert.AreEqual(HLResourceKind.Health, sink.LastResource);
            Assert.AreEqual(-7f, sink.LastAmount, "the pre-clamp delta keeps its sign");
            Assert.AreEqual(1, sink.StatusCount);
            Assert.AreEqual(HLClockKind.Simulation, sink.LastClock);
            Assert.AreEqual(1, sink.RemoveCount);
            Assert.AreEqual(1, sink.PulseCount);
            Assert.AreEqual(HLZoneKind.Hostile, sink.LastZoneKind);
        }
    }
}
