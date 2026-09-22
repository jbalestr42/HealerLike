using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLAreaPulseTests
    {
        [Test] public void StartReadsInitializedRadiusAndPositionNotTransformScaleAndDisableRemoves()
        {
            var ownerGo = new GameObject("zones");
            var areaGo = new GameObject("area");
            try
            {
                var owner = ownerGo.AddComponent<HLZoneRegistry>();
                owner.Initialize(new HLZoneFakeUpload());
                var pulse = areaGo.AddComponent<HLAreaPulse>();
                var area = areaGo.GetComponent<AreaOfEffect>();
                area.radius = 3.25f;
                areaGo.transform.position = new Vector3(2, 3, 4);
                areaGo.transform.localScale = Vector3.one * 99;
                TestHelpers.InvokePrivate(pulse, "Start");
                owner.PublishFrame(0);
                Assert.AreEqual((int)HLZoneKind.Hostile, owner.Snapshot[0].kind);
                Assert.AreEqual(3.25f, owner.Snapshot[0].radius);
                Assert.AreEqual(areaGo.transform.position, owner.Snapshot[0].position);
                owner.PublishFrame(0.4f);
                Assert.AreEqual(0.5f, owner.Snapshot[0].strength);
                TestHelpers.InvokePrivate(pulse, "OnDisable");
                owner.PublishFrame(0);
                Assert.AreEqual(0, owner.Count);
            }
            finally { Object.DestroyImmediate(areaGo); Object.DestroyImmediate(ownerGo); }
        }

        [Test] public void AuthoredKindIsHonouredAndPulseExpiresAtEightTenths()
        {
            var ownerGo = new GameObject("zones");
            var areaGo = new GameObject("area");
            try
            {
                var owner = ownerGo.AddComponent<HLZoneRegistry>();
                owner.Initialize(new HLZoneFakeUpload());
                var pulse = areaGo.AddComponent<HLAreaPulse>();
                pulse.Kind = HLZoneKind.Heal;
                TestHelpers.InvokePrivate(pulse, "Start");
                owner.PublishFrame(0);
                Assert.AreEqual((int)HLZoneKind.Heal, owner.Snapshot[0].kind);
                owner.PublishFrame(0.8f);
                Assert.AreEqual(0, owner.LiveCount);
            }
            finally { Object.DestroyImmediate(areaGo); Object.DestroyImmediate(ownerGo); }
        }
    }
}
