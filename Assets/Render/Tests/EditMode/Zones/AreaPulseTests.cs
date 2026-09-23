using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class AreaPulseTests
    {
        [Test]
        public void StartReadsInitializedRadiusAndPositionNotTransformScaleAndDisableRemoves()
        {
            GameObject ownerGo = new GameObject("zones");
            GameObject areaGo = new GameObject("area");
            try
            {
                ZoneRegistry owner = ownerGo.AddComponent<ZoneRegistry>();
                owner.Init(new ZoneFakeUpload());
                AreaPulse pulse = areaGo.AddComponent<AreaPulse>();
                AreaOfEffect area = areaGo.GetComponent<AreaOfEffect>();
                area.radius = 3.25f;
                areaGo.transform.position = new Vector3(2f, 3f, 4f);
                areaGo.transform.localScale = Vector3.one * 99f;
                pulse.Init(owner);

                TestHelpers.InvokePrivate(pulse, "Start");
                owner.PublishFrame(0f);

                Assert.AreEqual((int)ZoneKind.Hostile, owner.snapshot[0].kind);
                Assert.AreEqual(3.25f, owner.snapshot[0].radius);
                Assert.AreEqual(areaGo.transform.position, owner.snapshot[0].position);

                owner.PublishFrame(0.4f);
                Assert.AreEqual(0.5f, owner.snapshot[0].strength);

                TestHelpers.InvokePrivate(pulse, "OnDisable");
                owner.PublishFrame(0f);
                Assert.AreEqual(0, owner.count);
            }
            finally
            {
                Object.DestroyImmediate(areaGo);
                Object.DestroyImmediate(ownerGo);
            }
        }

        [Test]
        public void AuthoredKindIsHonouredAndPulseExpiresAtEightTenths()
        {
            GameObject ownerGo = new GameObject("zones");
            GameObject areaGo = new GameObject("area");
            try
            {
                ZoneRegistry owner = ownerGo.AddComponent<ZoneRegistry>();
                owner.Init(new ZoneFakeUpload());
                AreaPulse pulse = areaGo.AddComponent<AreaPulse>();
                pulse.kind = ZoneKind.Heal;
                pulse.Init(owner);

                TestHelpers.InvokePrivate(pulse, "Start");
                owner.PublishFrame(0f);

                Assert.AreEqual((int)ZoneKind.Heal, owner.snapshot[0].kind);

                owner.PublishFrame(0.8f);
                Assert.AreEqual(0, owner.liveCount);
            }
            finally
            {
                Object.DestroyImmediate(areaGo);
                Object.DestroyImmediate(ownerGo);
            }
        }


        [Test]
        public void Start_InitWithZones_PulsesOnTheGivenRegistry()
        {
            GameObject ownerGo = new GameObject("zones");
            GameObject areaGo = new GameObject("area");
            ZoneRegistry owner = ownerGo.AddComponent<ZoneRegistry>();
            owner.Init(new ZoneFakeUpload());
            AreaPulse pulse = areaGo.AddComponent<AreaPulse>();
            areaGo.GetComponent<AreaOfEffect>().radius = 2f;

            pulse.Init(owner);
            TestHelpers.InvokePrivate(pulse, "Start");

            Assert.AreEqual(1, owner.liveCount);

            Object.DestroyImmediate(areaGo);
            Object.DestroyImmediate(ownerGo);
        }

        [Test]
        public void Start_InitWithoutZones_PulsesNothing()
        {
            GameObject ownerGo = new GameObject("zones");
            GameObject areaGo = new GameObject("area");
            ZoneRegistry owner = ownerGo.AddComponent<ZoneRegistry>();
            owner.Init(new ZoneFakeUpload());
            AreaPulse pulse = areaGo.AddComponent<AreaPulse>();
            areaGo.GetComponent<AreaOfEffect>().radius = 2f;

            pulse.Init(null);
            TestHelpers.InvokePrivate(pulse, "Start");

            Assert.AreEqual(0, owner.liveCount);

            Object.DestroyImmediate(areaGo);
            Object.DestroyImmediate(ownerGo);
        }
    }
}
