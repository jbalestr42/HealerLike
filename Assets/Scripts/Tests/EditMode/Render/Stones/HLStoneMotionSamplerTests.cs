using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneMotionSamplerTests
    {
        [Test] public void StationarySpawnDragTeleportAndVerticalMotionDoNothing()
        {
            var s=new HLStoneMotionSampler(); Assert.AreEqual(Vector3.zero,s.Sample(Vector3.one*10,.02f,false));
            Assert.AreEqual(Vector3.zero,s.Sample(Vector3.one*10,.02f,false));
            Assert.AreEqual(Vector3.zero,s.Sample(Vector3.one*20,.02f,false));
            Assert.AreEqual(Vector3.zero,s.Sample(Vector3.one*20+Vector3.up,.02f,false));
            Assert.AreEqual(Vector3.zero,s.Sample(Vector3.one*20+Vector3.right*.1f,.02f,true));
            s.Reset(); Assert.AreEqual(Vector3.zero,s.Sample(Vector3.zero,.02f,false));
            Assert.AreEqual(Vector3.right,s.Sample(Vector3.right*.1f,.1f,false));
        }
    }
}
