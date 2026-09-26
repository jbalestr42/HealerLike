using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class ZoneUpdateTests
    {
        GameObject _go;
        ZoneRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("zone update test");
            _registry = _go.AddComponent<ZoneRegistry>();
            _registry.Init(new ZoneFakeUpload());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void UpdateZone_Pulse_KeepsAgeOrderAndRemainingFade()
        {
            _registry.Add(ZoneKind.Heal, Vector3.left, 1f, 1f);
            int pulse = _registry.AddPulse(ZoneKind.Heal, Vector3.zero, 1f, 1f, 0.4f);
            _registry.Add(ZoneKind.Heal, Vector3.right, 1f, 1f);
            _registry.PublishFrame(0.1f);

            _registry.UpdateZone(pulse, ZoneKind.Heal, Vector3.back, 3f, 0.8f);
            _registry.PublishFrame(0f);

            Assert.AreEqual(3, _registry.count);
            Assert.AreEqual(Vector3.left, _registry.snapshot[0].position);
            Assert.AreEqual(Vector3.back, _registry.snapshot[1].position);
            Assert.AreEqual(Vector3.right, _registry.snapshot[2].position);
            Assert.AreEqual(0u, _registry.snapshot[1].reserved);
            Assert.AreEqual(0.1f, _registry.snapshot[1].age);
            Assert.AreEqual(3f, _registry.snapshot[1].radius);
            Assert.AreEqual(0.6f, _registry.snapshot[1].strength, 0.0001f);
        }

        [Test]
        public void UpdateZone_KindChanges_PreservesAgeWithoutReservedData()
        {
            int handle = _registry.AddPulse(ZoneKind.Heal, Vector3.zero, 1f, 1f, 0.4f);
            _registry.PublishFrame(0.1f);

            _registry.UpdateZone(handle, ZoneKind.Hostile, Vector3.one, 2f, 1f);
            _registry.PublishFrame(0f);

            Assert.AreEqual((int)ZoneKind.Hostile, _registry.snapshot[0].kind);
            Assert.AreEqual(0u, _registry.snapshot[0].reserved);
            Assert.AreEqual(0.1f, _registry.snapshot[0].age);
            _registry.UpdateZone(handle, ZoneKind.Heal, Vector3.one, 2f, 1f);
            _registry.PublishFrame(0f);
            Assert.AreEqual((int)ZoneKind.Heal, _registry.snapshot[0].kind);
            Assert.AreEqual(0u, _registry.snapshot[0].reserved);
        }
    }

}
