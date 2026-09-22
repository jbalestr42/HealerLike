using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneGroundRingTests
    {
        [Test] public void PublicRadiusCoversScaledClumpAndReconfigureReusesDisc()
        {
            var root=new GameObject("HLRing"); var ring=root.AddComponent<HLStoneGroundRing>();
            try
            {
                ring.Configure(new Bounds(Vector3.up,new Vector3(2,2,1)));
                Assert.AreEqual(1.18f,ring.Radius,.001f);
                root.transform.localScale=new Vector3(2,1,3); Assert.AreEqual(3.54f,ring.Radius,.001f);
                ring.Configure(new Bounds(Vector3.up,new Vector3(2,2,1))); Assert.AreEqual(1,root.transform.childCount);
                Assert.AreEqual(.006f,ring.Center.y,.0001f); Assert.AreEqual(0,root.GetComponentsInChildren<Collider>().Length);
            }
            finally { TestHelpers.InvokePrivate(ring,"OnDestroy"); Object.DestroyImmediate(root); }
        }
    }
}
