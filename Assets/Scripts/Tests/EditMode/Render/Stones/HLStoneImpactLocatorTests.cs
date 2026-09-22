using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneImpactLocatorTests
    {
        [Test] public void ClosestFacePointUnderRotationAndNonuniformScale()
        {
            var d=new HLStoneMeshData(new[]{Vector3.zero,Vector3.right,Vector3.up},new[]{Vector3.forward,Vector3.forward,Vector3.forward},new[]{0,1,2},default);
            var m=Matrix4x4.TRS(new Vector3(1,2,3),Quaternion.Euler(21,53,17),new Vector3(2,3,.5f));
            var expected=m.MultiplyPoint3x4(new Vector3(.2f,.3f,0)); var n=m.MultiplyVector(Vector3.forward).normalized;
            Assert.IsTrue(HLStoneImpactLocator.TryClosestPoint(d,m,expected+n*2,out var p,out var normal));
            Assert.That(Vector3.Distance(expected,p),Is.LessThan(1e-5)); Assert.That(Vector3.Dot(n,normal),Is.GreaterThan(.99999f));
            Assert.IsTrue(HLStoneImpactLocator.TryClosestPoint(d,Matrix4x4.identity,new Vector3(-1,-1,0),out p,out normal)); Assert.AreEqual(Vector3.zero,p);
            Assert.IsFalse(HLStoneImpactLocator.TryClosestPoint(default,m,Vector3.zero,out p,out normal));
        }
    }
}
