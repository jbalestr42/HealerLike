using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneGroundRingTests
    {
        [Test]
        public void PublicRadiusCoversScaledClumpAndReconfigureReusesDisc()
        {
            GameObject root = new GameObject("HLRing");
            HLStoneGroundRing ring = root.AddComponent<HLStoneGroundRing>();
            try
            {
                ring.Configure(new Bounds(Vector3.up, new Vector3(2f, 2f, 1f)));
                Assert.AreEqual(1.18f, ring.radius, 0.001f);

                root.transform.localScale = new Vector3(2f, 1f, 3f);
                Assert.AreEqual(3.54f, ring.radius, 0.001f);

                ring.Configure(new Bounds(Vector3.up, new Vector3(2f, 2f, 1f)));
                Assert.AreEqual(1, root.transform.childCount);
                Assert.AreEqual(0.006f, ring.center.y, 0.0001f);
                Assert.AreEqual(0, root.GetComponentsInChildren<Collider>().Length);
            }
            finally
            {
                TestHelpers.InvokePrivate(ring, "OnDestroy");
                Object.DestroyImmediate(root);
            }
        }
    }
}
