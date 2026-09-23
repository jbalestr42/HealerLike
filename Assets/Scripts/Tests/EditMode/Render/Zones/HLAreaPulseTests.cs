using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLAreaPulseTests
    {
        [Test]
        public void StartReadsInitializedRadiusAndPositionNotTransformScaleAndDisableRemoves()
        {
            GameObject ownerGo = new GameObject("zones");
            GameObject areaGo = new GameObject("area");
            try
            {
                HLZoneRegistry owner = ownerGo.AddComponent<HLZoneRegistry>();
                owner.Initialize(new HLZoneFakeUpload());
                HLAreaPulse pulse = areaGo.AddComponent<HLAreaPulse>();
                AreaOfEffect area = areaGo.GetComponent<AreaOfEffect>();
                area.radius = 3.25f;
                areaGo.transform.position = new Vector3(2f, 3f, 4f);
                areaGo.transform.localScale = Vector3.one * 99f;

                TestHelpers.InvokePrivate(pulse, "Start");
                owner.PublishFrame(0f);

                Assert.AreEqual((int)HLZoneKind.Hostile, owner.snapshot[0].kind);
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
                HLZoneRegistry owner = ownerGo.AddComponent<HLZoneRegistry>();
                owner.Initialize(new HLZoneFakeUpload());
                HLAreaPulse pulse = areaGo.AddComponent<HLAreaPulse>();
                pulse.kind = HLZoneKind.Heal;

                TestHelpers.InvokePrivate(pulse, "Start");
                owner.PublishFrame(0f);

                Assert.AreEqual((int)HLZoneKind.Heal, owner.snapshot[0].kind);

                owner.PublishFrame(0.8f);
                Assert.AreEqual(0, owner.liveCount);
            }
            finally
            {
                Object.DestroyImmediate(areaGo);
                Object.DestroyImmediate(ownerGo);
            }
        }
    }
}
