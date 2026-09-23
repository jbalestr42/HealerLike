using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneLifeTests
    {
        class HLUpload : IHLZoneUpload
        {
            public GraphicsBuffer buffer { get { return null; } }

            public void Upload(HLZone[] zones)
            {
            }

            public void Bind()
            {
            }

            public void PublishCount(int count)
            {
            }

            public void Unbind()
            {
            }

            public void Dispose()
            {
            }
        }

        static HLStoneEffects CreateEffects()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
            return Object.Instantiate(prefab).GetComponent<HLStoneEffects>();
        }

        [Test]
        public void TerrainReadsPublishedRegistryAndEmitsOncePerHostilePulse()
        {
            GameObject root = new GameObject("HLTerrainLife");
            GameObject zoneRoot = new GameObject("HLZones");
            HLStoneEffects fx = CreateEffects();
            GameObject fxRoot = fx.gameObject;
            HLStoneLife life = root.AddComponent<HLStoneLife>();
            HLZoneRegistry zones = zoneRoot.AddComponent<HLZoneRegistry>();
            try
            {
                zones.Init(new HLUpload());
                life.Init(fx, zones, 1, 0.5f, true);
                zones.AddPulse(HLZoneKind.Heal, Vector3.zero, 1f, 1f, 1f);
                zones.PublishFrame(0.01f);
                life.Advance(0.01f);
                Assert.AreEqual(0, fx.liveCount);

                zones.AddPulse(HLZoneKind.Hostile, Vector3.zero, 1f, 1f, 1f);
                zones.PublishFrame(0.01f);
                life.Advance(0.01f);
                Assert.AreEqual(5, fx.liveCount);

                zones.PublishFrame(0.01f);
                life.Advance(0.01f);
                Assert.AreEqual(5, fx.liveCount);

                zones.Release();
                life.Advance(0.01f);
                Assert.AreEqual(5, fx.liveCount);
            }
            finally
            {
                zones.Release();
                TestHelpers.InvokePrivate(life, "OnDestroy");
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(fxRoot);
                Object.DestroyImmediate(zoneRoot);
            }
        }

        [Test]
        public void NearbyImpactWobblesTopAndDisableRestoresIt()
        {
            GameObject root = new GameObject("HLLife");
            GameObject top = new GameObject("HLTop");
            HLStoneEffects fx = CreateEffects();
            GameObject fxRoot = fx.gameObject;
            HLStoneLife life = root.AddComponent<HLStoneLife>();
            top.transform.SetParent(root.transform);
            Quaternion rest = Quaternion.Euler(0f, 30f, 0f);
            top.transform.localRotation = rest;
            try
            {
                life.Init(fx, null, 1, 0.5f, false, top.transform);
                fx.RecordImpact(Vector3.right * 20f, 1);
                life.Advance(0.05f);
                Assert.Less(Quaternion.Angle(rest, top.transform.localRotation), 0.03f);

                fx.RecordImpact(Vector3.zero, 1);
                life.Advance(0.05f);
                Assert.Greater(Quaternion.Angle(rest, top.transform.localRotation), 3);

                life.Advance(2f);
                Assert.Less(Quaternion.Angle(rest, top.transform.localRotation), 0.03f);

                fx.RecordImpact(Vector3.zero, 1);
                life.Advance(0.05f);
                life.enabled = false;
                TestHelpers.InvokePrivate(life, "OnDisable");
                Assert.Less(Quaternion.Angle(rest, top.transform.localRotation), 0.03f);

                life.PollHealth(0.3f, 0.1f, Vector3.up);
                Assert.AreEqual(15, fx.liveCount);
            }
            finally
            {
                TestHelpers.InvokePrivate(life, "OnDestroy");
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(fxRoot);
            }
        }
    }
}
