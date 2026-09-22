using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Zones
{
    public class HLTrampleZoneTests
    {
        [Test] public void FollowsObstacleUpdatesOneHandleAndRemovesInvalidOrDisabledFootprints()
        {
            var root = new GameObject("zones"); var obstacle = new GameObject("obstacle");
            try
            {
                var registry = root.AddComponent<HLZoneRegistry>(); registry.Initialize(new HLZoneFakeUpload());
                var zone = obstacle.AddComponent<HLTrampleZone>(); zone.Refresh(); registry.PublishFrame(1);
                Assert.AreEqual(6, registry.Snapshot[0].kind);
                obstacle.transform.position = Vector3.forward; zone.Radius = 2;
                for (int i = 0; i < 50; i++) zone.Refresh();
                registry.PublishFrame(1); Assert.AreEqual(1, registry.Count);
                Assert.AreEqual(Vector3.forward, registry.Snapshot[0].position); Assert.AreEqual(2, registry.Snapshot[0].radius);
                zone.Radius = float.NaN; zone.Refresh(); Assert.AreEqual(0, registry.LiveCount);
                zone.Radius = 1; zone.Refresh(); Assert.AreEqual(1, registry.LiveCount);
                zone.enabled = false; TestHelpers.InvokePrivate(zone, "OnDisable"); Assert.AreEqual(0, registry.LiveCount);
                zone.enabled = true; zone.Refresh(); Assert.AreEqual(1, registry.LiveCount);
                TestHelpers.InvokePrivate(zone, "OnDestroy"); Assert.AreEqual(0, registry.LiveCount);
            }
            finally { Object.DestroyImmediate(obstacle); Object.DestroyImmediate(root); }
        }
        [Test] public void ReacquiresRegistryAfterOwnerRecreation()
        {
            var root = new GameObject("zones"); var obstacle = new GameObject("obstacle");
            try
            {
                var registry = root.AddComponent<HLZoneRegistry>(); registry.Initialize(new HLZoneFakeUpload());
                var zone = obstacle.AddComponent<HLTrampleZone>(); zone.Refresh();
                Object.DestroyImmediate(root); zone.Refresh();
                root = new GameObject("replacement"); registry = root.AddComponent<HLZoneRegistry>(); registry.Initialize(new HLZoneFakeUpload());
                zone.Refresh(); Assert.AreEqual(1, registry.LiveCount);
            }
            finally { Object.DestroyImmediate(obstacle); Object.DestroyImmediate(root); }
        }
    }
}
