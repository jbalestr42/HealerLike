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
        [SetUp] public void Setup() { previous = HLRenderRegistry.current; HLRenderRegistry.current = null; root = new GameObject("HLTest"); root.SetActive(false); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); HLRenderRegistry.current = previous; }
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
            Assert.That(HLRenderRegistry.current, Is.SameAs(bootstrap.Registry));
            HLRenderRegistry.current.spellSink.PulseArea(Vector3.zero, 1, HLZoneKind.Heal, 1);
            Assert.That(fake.pulses, Is.EqualTo(1)); Assert.That(look.enabled && zones.enabled && bridge.enabled && grass.enabled, Is.True);
            Assert.Throws<System.InvalidOperationException>(() => bootstrap.Configure(fake, look, zones));
            TestHelpers.InvokePrivate(bootstrap, "OnDisable");
            Assert.That(HLRenderRegistry.current, Is.Null); Assert.That(look.enabled || zones.enabled || bridge.enabled || grass.enabled, Is.False);
            TestHelpers.InvokePrivate(bootstrap, "OnEnable");
            Assert.That(HLRenderRegistry.current.spellSink, Is.SameAs(fake));
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
                Assert.That(HLRenderRegistry.current.zoneOwner, Is.SameAs(zones));
                Assert.That(sink.areaPulse, Is.Not.Null);
                sink.PulseArea(Vector3.zero, 2, HLZoneKind.Hostile, 1);
                Assert.That(zones.liveCount, Is.EqualTo(1));
                TestHelpers.InvokePrivate(bootstrap, "OnDisable");
                Assert.That(sink.areaPulse, Is.Null);
            }
            finally { zones.Release(); }
        }
        [Test] public void InitializesTheHealerPulseForItsSourceAndReleasesIt()
        {
            var bootstrap = root.AddComponent<HLRenderBootstrap>();
            var pulse = root.AddComponent<HLHealPulse>();
            var healer = new GameObject("HLHealer");
            var source = typeof(HLHealPulse).GetField("_source", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            try
            {
                TestHelpers.SetPrivateField(bootstrap, "healPulse", pulse);
                TestHelpers.SetPrivateField(bootstrap, "healSource", healer);
                bootstrap.Configure(new HLFakeSink(), null, null);
                TestHelpers.InvokePrivate(bootstrap, "OnEnable");
                Assert.That(source.GetValue(pulse), Is.SameAs(healer), "a Character is not walked by EntityModel.Init");
                TestHelpers.InvokePrivate(bootstrap, "OnDisable");
                Assert.That(source.GetValue(pulse), Is.Null);
            }
            finally { Object.DestroyImmediate(healer); }
        }
        [Test] public void DuplicateCannotReplaceOrClearOwner()
        {
            var first = root.AddComponent<HLRenderBootstrap>(); TestHelpers.InvokePrivate(first, "OnEnable");
            var other = new GameObject("HLOther"); other.SetActive(false);
            try {
                var second = other.AddComponent<HLRenderBootstrap>();
                TestHelpers.WithLoggingDisabled(() => TestHelpers.InvokePrivate(second, "OnEnable"));
                TestHelpers.InvokePrivate(second, "OnDisable");
                Assert.That(HLRenderRegistry.current, Is.SameAs(first.Registry));
            } finally { Object.DestroyImmediate(other); }
        }
        [Test] public void DisableDoesNotClearAReplacementRegistry()
        {
            var bootstrap = root.AddComponent<HLRenderBootstrap>(); TestHelpers.InvokePrivate(bootstrap, "OnEnable");
            var replacement = new HLRenderRegistry(); HLRenderRegistry.current = replacement;
            TestHelpers.InvokePrivate(bootstrap, "OnDisable");
            Assert.That(HLRenderRegistry.current, Is.SameAs(replacement));
        }
        [Test] public void AppliesPortraitByDefaultAndLandscapeOnRequest()
        {
            var bootstrap = root.AddComponent<HLRenderBootstrap>();
            var cameraGo = new GameObject("HLFramingCamera");
            try {
                var camera = cameraGo.AddComponent<Camera>();
                var portrait = new Pose(new Vector3(0,40,-12), Quaternion.Euler(73.7f,0,0)); var landscape = new Pose(new Vector3(0,24,-20), Quaternion.Euler(50,0,0));
                bootstrap.ConfigureFraming(camera, portrait, landscape);
                Assert.AreEqual(HLRenderBootstrap.Framing.Portrait, bootstrap.CameraFraming);
                Assert.AreEqual(portrait.position, camera.transform.position);
                bootstrap.CameraFraming = HLRenderBootstrap.Framing.Landscape;
                Assert.AreEqual(landscape.position, camera.transform.position);
                Assert.That(Quaternion.Angle(landscape.rotation, camera.transform.rotation), Is.LessThan(.01f));
            } finally { Object.DestroyImmediate(cameraGo); }
        }
    }
}
