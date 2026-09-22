using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Stage
{
    public class HLRenderBootstrapTests
    {
        sealed class HLFakeSink : IHLSpellVisualSink
        {
            public int pulses;
            public void PulseArea(Vector3 c, float r, HLZoneKind k, float s) { pulses++; }
            public void ShowImpact(GameObject s, GameObject t, HLResourceKind r, float a, bool c) { }
            public void SetStatus(GameObject s, GameObject t, ABuffHandlerFactory f, int n, float e, float d, HLClockKind c) { }
            public void RemoveStatus(GameObject s, GameObject t, ABuffHandlerFactory f) { }
        }
        GameObject root;
        HLRenderRegistry previous;
        [SetUp] public void Setup() { previous = HLRenderRegistry.Current; HLRenderRegistry.Current = null; root = new GameObject("HLTest"); root.SetActive(false); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); HLRenderRegistry.Current = previous; }
        [Test] public void PublishesInjectedSinkAndOwnsComponentLifetimes()
        {
            var bootstrap = root.AddComponent<HLRenderBootstrap>();
            var look = root.AddComponent<HLStagePlaceholder>();
            var zones = root.AddComponent<HLStagePlaceholder>();
            var bridge = root.AddComponent<HLStagePlaceholder>();
            var grass = root.AddComponent<HLStagePlaceholder>();
            TestHelpers.SetPrivateField(bootstrap,"zoneBridge",bridge);
            TestHelpers.SetPrivateField(bootstrap,"grassField",grass);
            look.enabled = zones.enabled = bridge.enabled = grass.enabled = false;
            var fake = new HLFakeSink(); bootstrap.Configure(fake, look, zones);
            TestHelpers.InvokePrivate(bootstrap, "OnEnable");
            Assert.That(HLRenderRegistry.Current, Is.SameAs(bootstrap.Registry));
            HLRenderRegistry.Current.SpellSink.PulseArea(Vector3.zero, 1, HLZoneKind.Heal, 1);
            Assert.That(fake.pulses, Is.EqualTo(1)); Assert.That(look.enabled && zones.enabled && bridge.enabled && grass.enabled, Is.True);
            Assert.Throws<System.InvalidOperationException>(() => bootstrap.Configure(fake, look, zones));
            TestHelpers.InvokePrivate(bootstrap, "OnDisable");
            Assert.That(HLRenderRegistry.Current, Is.Null); Assert.That(look.enabled || zones.enabled || bridge.enabled || grass.enabled, Is.False);
            TestHelpers.InvokePrivate(bootstrap, "OnEnable");
            Assert.That(HLRenderRegistry.Current.SpellSink, Is.SameAs(fake));
        }
        [Test] public void BindsZoneOwnerAndSinkAreaPulseThenClearsOnDisable()
        {
            var bootstrap = root.AddComponent<HLRenderBootstrap>();
            var sink = root.AddComponent<HealerLike.Render.Spells.HLSpellVisualSink>();
            var zones = root.AddComponent<HLZoneRegistry>();
            var upload = new HLZoneFakeUpload(); zones.Initialize(upload);
            try
            {
                bootstrap.Configure(sink, null, zones);
                TestHelpers.InvokePrivate(bootstrap, "OnEnable");
                Assert.That(HLRenderRegistry.Current.ZoneOwner, Is.SameAs(zones));
                Assert.That(sink.AreaPulse, Is.Not.Null);
                sink.PulseArea(Vector3.zero, 2, HLZoneKind.Hostile, 1);
                Assert.That(zones.LiveCount, Is.EqualTo(1));
                TestHelpers.InvokePrivate(bootstrap, "OnDisable");
                Assert.That(sink.AreaPulse, Is.Null);
            }
            finally { zones.Release(); }
        }
        [Test] public void DuplicateCannotReplaceOrClearOwner()
        {
            var first = root.AddComponent<HLRenderBootstrap>(); TestHelpers.InvokePrivate(first, "OnEnable");
            var other = new GameObject("HLOther"); other.SetActive(false);
            try {
                var second = other.AddComponent<HLRenderBootstrap>();
                TestHelpers.WithLoggingDisabled(() => TestHelpers.InvokePrivate(second, "OnEnable"));
                TestHelpers.InvokePrivate(second, "OnDisable");
                Assert.That(HLRenderRegistry.Current, Is.SameAs(first.Registry));
            } finally { Object.DestroyImmediate(other); }
        }
        [Test] public void DisableDoesNotClearAReplacementRegistry()
        {
            var bootstrap = root.AddComponent<HLRenderBootstrap>(); TestHelpers.InvokePrivate(bootstrap, "OnEnable");
            var replacement = new HLRenderRegistry(); HLRenderRegistry.Current = replacement;
            TestHelpers.InvokePrivate(bootstrap, "OnDisable");
            Assert.That(HLRenderRegistry.Current, Is.SameAs(replacement));
        }
    }
}
