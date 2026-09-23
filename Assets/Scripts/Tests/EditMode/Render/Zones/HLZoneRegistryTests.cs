using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Zones
{
    public class HLZoneFakeUpload : IHLZoneUpload
    {
        public GraphicsBuffer Buffer => null;
        public readonly List<string> Calls = new List<string>();
        public readonly HLZone[] Data = new HLZone[64];
        public int Count;
        public void Upload(HLZone[] zones) { zones.CopyTo(Data, 0); Calls.Add("upload"); }
        public void Bind() => Calls.Add("bind");
        public void PublishCount(int count) { Count = count; Calls.Add("count:" + count); }
        public void Unbind() => Calls.Add("unbind");
        public void Dispose() => Calls.Add("dispose");
    }

    public class HLZoneRegistryTests
    {
        GameObject _go;
        HLZoneRegistry _registry;
        HLZoneFakeUpload _upload;
        [SetUp] public void SetUp()
        {
            _go = new GameObject("zone test");
            _registry = _go.AddComponent<HLZoneRegistry>();
            _upload = new HLZoneFakeUpload();
            _registry.Initialize(_upload);
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(_go);
        int Add(float x) => _registry.Add(HLZoneKind.Heal, new Vector3(x, 0, 0), 1, 1);

        [Test] public void InvalidLaunchAndMissingHealTargetAreRejected()
        {
            Assert.AreEqual(0, _registry.AddLaunch(Vector3.zero, Vector3.up));
            Assert.AreEqual(0, _registry.AddLaunch(Vector3.zero, new Vector3(float.NaN, 0, 0)));
            Assert.AreEqual(0, _registry.AddHealPulse(null, 1));
            Assert.AreEqual(0, _registry.LiveCount);
        }
        [Test] public void ActsAsTheRegistryZoneOwner()
        {
            IHLZoneOwner owner = _registry;
            int handle = owner.AddPulse(HLZoneKind.Hostile, Vector3.zero, 2, 1, 0.8f);
            Assert.That(handle, Is.GreaterThan(0));
            Assert.IsTrue(_registry.Contains(handle));
            _registry.PublishFrame(0.8f);
            Assert.IsFalse(_registry.Contains(handle));
        }

        [Test] public void UpdatesPreserveOrderAndPublishCanonicalSnapshotEveryFrame()
        {
            int first = Add(1); Add(2); Add(3);
            _registry.UpdateZone(first, HLZoneKind.Hostile, Vector3.right * 9, 2, 5);
            _upload.Calls.Clear();
            _registry.PublishFrame(0.25f);
            CollectionAssert.AreEqual(new[] { "upload", "bind", "count:3" }, _upload.Calls);
            Assert.AreEqual(9, _upload.Data[0].position.x);
            Assert.AreEqual(2, _upload.Data[1].position.x);
            Assert.AreEqual(3, _upload.Data[2].position.x);
            Assert.AreEqual(1, _upload.Data[0].strength);
            Assert.AreEqual(0.25f, _upload.Data[0].age);
            _registry.PublishFrame(0.25f);
            Assert.AreEqual(0.5f, _registry.Snapshot[0].age);
        }

        [Test] public void OverflowKeepsFirst64AndReportsOncePerEpisode()
        {
            int first = Add(0);
            for (int i = 1; i < 65; i++) Add(i);
            LogAssert.Expect(LogType.Warning, "HLZoneRegistry: cosmetic zone capacity exceeded; feedback reserved before decorative footprints; first registered wins within each kind.");
            _registry.PublishFrame(0);
            Assert.AreEqual(64, _upload.Count);
            Assert.AreEqual(1, _registry.OverflowCount);
            Assert.AreEqual(63, _upload.Data[63].position.x);
            _registry.PublishFrame(0);
            _registry.Remove(first);
            _registry.PublishFrame(0);
            Assert.AreEqual(64, _upload.Data[63].position.x);
            Assert.AreEqual(0, _registry.OverflowCount);
            Add(65);
            LogAssert.Expect(LogType.Warning, "HLZoneRegistry: cosmetic zone capacity exceeded; feedback reserved before decorative footprints; first registered wins within each kind.");
            _registry.PublishFrame(0);
        }

        [Test] public void PersistentFootprintsCannotStarveHealAndExpiredFeedbackReturnsCapacity()
        {
            for (int i = 0; i < 80; i++) _registry.Add(HLZoneKind.Trample, Vector3.right * i, 1, 1);
            int heal = _registry.AddPulse(HLZoneKind.Heal, Vector3.right * 100, 2, 1, 0.45f);
            _registry.AddPulse(HLZoneKind.Hostile, Vector3.right * 101, 2, 1, 0.8f);
            LogAssert.Expect(LogType.Warning, "HLZoneRegistry: cosmetic zone capacity exceeded; feedback reserved before decorative footprints; first registered wins within each kind.");
            _registry.PublishFrame(0.1f);
            Assert.AreEqual(64, _registry.Count); Assert.AreEqual(82, _registry.LiveCount);
            Assert.AreEqual(18, _registry.OverflowCount);
            for (int i = 0; i < 62; i++) Assert.AreEqual(i, _registry.Snapshot[i].position.x);
            Assert.AreEqual((int)HLZoneKind.Heal, _registry.Snapshot[62].kind);
            Assert.AreEqual((int)HLZoneKind.Hostile, _registry.Snapshot[63].kind);
            _registry.PublishFrame(0.4f);
            Assert.IsFalse(_registry.Contains(heal));
            Assert.AreEqual(62, _registry.Snapshot[62].position.x);
            Assert.AreEqual((int)HLZoneKind.Hostile, _registry.Snapshot[63].kind);
            _registry.PublishFrame(0.4f);
            Assert.AreEqual(63, _registry.Snapshot[63].position.x);
        }

        [Test] public void RemovedSlotsAreReusedWithoutReusingHandlesOrReordering()
        {
            int old = Add(1); Add(2);
            _registry.Remove(old);
            _registry.Remove(old);
            int replacement = Add(3);
            Assert.AreNotEqual(old, replacement);
            _registry.UpdateZone(old, HLZoneKind.Hostile, Vector3.zero, 9, 1);
            _registry.PublishFrame(0);
            Assert.AreEqual(2, _upload.Count);
            Assert.AreEqual(2, _upload.Data[0].position.x);
            Assert.AreEqual(3, _upload.Data[1].position.x);
            Assert.AreEqual(default(HLZone), _upload.Data[2]);
        }

        [Test] public void PulseFadesLinearlyAndAutoRemovesAtDuration()
        {
            int pulse = _registry.AddPulse(HLZoneKind.Hostile, Vector3.one, 2, 0.8f, 0.8f);
            _registry.PublishFrame(0.4f);
            Assert.AreEqual(0.4f, _upload.Data[0].strength, 0.0001f);
            _registry.PublishFrame(0);
            Assert.AreEqual(0.4f, _upload.Data[0].age);
            _registry.PublishFrame(0.4f);
            Assert.IsFalse(_registry.Contains(pulse));
            Assert.AreEqual(0, _registry.LiveCount);
            Assert.AreEqual(0, _upload.Count);
            Assert.AreEqual(default(HLZone), _upload.Data[0]);
        }

        [Test] public void TeardownPublishesZeroBeforeUnbindingAndDisposingExactlyOnce()
        {
            int old = Add(1);
            _registry.PublishFrame(0);
            _upload.Calls.Clear();
            _registry.Release(); _registry.Release();
            CollectionAssert.AreEqual(new[] { "count:0", "unbind", "dispose" }, _upload.Calls);
            Assert.IsNull(HLZoneRegistry.Current);
            Assert.AreEqual(0, _registry.Count);
            Assert.AreEqual(0, Add(2));
            _registry.Initialize(new HLZoneFakeUpload());
            Assert.AreNotEqual(old, Add(3));
        }

        [Test] public void InvalidInputCannotCreateOrKeepAnActiveZone()
        {
            Assert.AreEqual(0, _registry.Add(HLZoneKind.None, Vector3.zero, 1, 1));
            Assert.AreEqual(0, _registry.Add(HLZoneKind.Heal, Vector3.zero, 0, 1));
            Assert.AreEqual(0, _registry.Add(HLZoneKind.Heal, Vector3.zero, 1, 0));
            Assert.AreEqual(0, _registry.AddPulse(HLZoneKind.Heal, Vector3.zero, 1, 1, float.NaN));
            Assert.AreEqual(0, _registry.AddPulse(HLZoneKind.Heal, Vector3.zero, 1, 1, 0));
            int handle = Add(1);
            _registry.UpdateZone(handle, HLZoneKind.Heal, Vector3.zero, 1, float.NaN);
            Assert.IsFalse(_registry.Contains(handle));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _registry.PublishFrame(-1));
        }

        [Test] public void SecondOwnerCannotPublishOrClearFirstOwnersState()
        {
            var other = new GameObject("second owner");
            try
            {
                var registry = other.AddComponent<HLZoneRegistry>();
                Assert.Throws<System.InvalidOperationException>(() => registry.Initialize(new HLZoneFakeUpload()));
                registry.Release();
                Assert.AreSame(_registry, HLZoneRegistry.Current);
                Assert.AreEqual(1, _upload.Calls.Count);
            }
            finally { Object.DestroyImmediate(other); }
        }
    }
}
