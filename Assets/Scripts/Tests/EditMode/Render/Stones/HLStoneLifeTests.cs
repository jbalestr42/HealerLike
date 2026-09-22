using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneLifeTests
    {
        sealed class HLUpload : HealerLike.Render.Zones.IHLZoneUpload
        {
            public GraphicsBuffer Buffer=>null;
            public void Upload(HealerLike.Render.Zones.HLZone[] zones) { }
            public void Bind() { } public void PublishCount(int count) { } public void Unbind() { } public void Dispose() { }
        }
        [Test] public void TerrainReadsPublishedRegistryAndEmitsOncePerHostilePulse()
        {
            var root=new GameObject("HLTerrainLife"); var fxRoot=new GameObject("HLFX"); var zoneRoot=new GameObject("HLZones");
            var fx=fxRoot.AddComponent<HLStoneEffects>(); var life=root.AddComponent<HLStoneLife>();
            var zones=zoneRoot.AddComponent<HealerLike.Render.Zones.HLZoneRegistry>();
            try
            {
                zones.Initialize(new HLUpload()); life.Configure(fx,1,.5f,true);
                zones.AddPulse(HealerLike.Render.Zones.HLZoneKind.Heal,Vector3.zero,1,1,1);
                zones.PublishFrame(.01f); life.Advance(.01f); Assert.AreEqual(0,fx.LiveCount);
                zones.AddPulse(HealerLike.Render.Zones.HLZoneKind.Hostile,Vector3.zero,1,1,1);
                zones.PublishFrame(.01f); life.Advance(.01f); Assert.AreEqual(5,fx.LiveCount);
                zones.PublishFrame(.01f); life.Advance(.01f); Assert.AreEqual(5,fx.LiveCount);
                zones.Release(); life.Advance(.01f); Assert.AreEqual(5,fx.LiveCount);
            }
            finally { zones.Release(); TestHelpers.InvokePrivate(life,"OnDestroy"); TestHelpers.InvokePrivate(fx,"OnDestroy"); Object.DestroyImmediate(root); Object.DestroyImmediate(fxRoot); Object.DestroyImmediate(zoneRoot); }
        }
        [Test] public void NearbyImpactWobblesTopAndDisableRestoresIt()
        {
            var root=new GameObject("HLLife"); var top=new GameObject("HLTop"); var fxRoot=new GameObject("HLFX");
            var fx=fxRoot.AddComponent<HLStoneEffects>(); var life=root.AddComponent<HLStoneLife>();
            top.transform.SetParent(root.transform); var rest=Quaternion.Euler(0,30,0); top.transform.localRotation=rest;
            try
            {
                life.Configure(fx,1,.5f,false,top.transform);
                fx.RecordImpact(Vector3.right*20,1); life.Advance(.05f); Assert.Less(Quaternion.Angle(rest,top.transform.localRotation),.03f);
                fx.RecordImpact(Vector3.zero,1); life.Advance(.05f); Assert.Greater(Quaternion.Angle(rest,top.transform.localRotation),3);
                life.Advance(2); Assert.Less(Quaternion.Angle(rest,top.transform.localRotation),.03f);
                fx.RecordImpact(Vector3.zero,1); life.Advance(.05f); life.enabled=false; TestHelpers.InvokePrivate(life,"OnDisable");
                Assert.Less(Quaternion.Angle(rest,top.transform.localRotation),.03f);
                life.PollHealth(.3f,.1f,Vector3.up); Assert.AreEqual(15,fx.LiveCount);
            }
            finally { TestHelpers.InvokePrivate(life,"OnDestroy"); TestHelpers.InvokePrivate(fx,"OnDestroy"); Object.DestroyImmediate(root); Object.DestroyImmediate(fxRoot); }
        }
    }
}
