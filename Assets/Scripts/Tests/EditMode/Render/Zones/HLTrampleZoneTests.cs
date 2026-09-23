using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLTrampleZoneTests
    {
        [Test]
        public void FollowsObstacleUpdatesOneHandleAndRemovesInvalidOrDisabledFootprints()
        {
            GameObject root = new GameObject("zones");
            GameObject obstacle = new GameObject("obstacle");
            try
            {
                HLZoneRegistry registry = root.AddComponent<HLZoneRegistry>();
                registry.Initialize(new HLZoneFakeUpload());
                HLTrampleZone zone = obstacle.AddComponent<HLTrampleZone>();
                zone.Refresh();
                registry.PublishFrame(1f);
                Assert.AreEqual(6, registry.snapshot[0].kind);

                obstacle.transform.position = Vector3.forward;
                zone.radius = 2f;
                for (int i = 0; i < 50; i++)
                {
                    zone.Refresh();
                }

                registry.PublishFrame(1f);
                Assert.AreEqual(1, registry.count);
                Assert.AreEqual(Vector3.forward, registry.snapshot[0].position);
                Assert.AreEqual(2, registry.snapshot[0].radius);

                zone.radius = float.NaN;
                zone.Refresh();
                Assert.AreEqual(0, registry.liveCount);

                zone.radius = 1f;
                zone.Refresh();
                Assert.AreEqual(1, registry.liveCount);

                zone.enabled = false;
                TestHelpers.InvokePrivate(zone, "OnDisable");
                Assert.AreEqual(0, registry.liveCount);

                zone.enabled = true;
                zone.Refresh();
                Assert.AreEqual(1, registry.liveCount);

                TestHelpers.InvokePrivate(zone, "OnDestroy");
                Assert.AreEqual(0, registry.liveCount);
            }
            finally
            {
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SteadyRefreshAllocatesNoManagedBytes()
        {
            GameObject root = new GameObject("zones");
            GameObject obstacle = new GameObject("obstacle");
            try
            {
                HLZoneRegistry registry = root.AddComponent<HLZoneRegistry>();
                registry.Initialize(new HLZoneFakeUpload());
                HLTrampleZone zone = obstacle.AddComponent<HLTrampleZone>();
                for (int i = 0; i < 100; i++)
                {
                    zone.Refresh();
                }

                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++)
                {
                    zone.Refresh();
                }

                long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

                Assert.AreEqual(0, allocated);
            }
            finally
            {
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReacquiresRegistryAfterOwnerRecreation()
        {
            GameObject root = new GameObject("zones");
            GameObject obstacle = new GameObject("obstacle");
            try
            {
                HLZoneRegistry registry = root.AddComponent<HLZoneRegistry>();
                registry.Initialize(new HLZoneFakeUpload());
                HLTrampleZone zone = obstacle.AddComponent<HLTrampleZone>();
                zone.Refresh();
                Object.DestroyImmediate(root);
                zone.Refresh();
                root = new GameObject("replacement");
                registry = root.AddComponent<HLZoneRegistry>();
                registry.Initialize(new HLZoneFakeUpload());

                zone.Refresh();

                Assert.AreEqual(1, registry.liveCount);
            }
            finally
            {
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(root);
            }
        }
    }
}
