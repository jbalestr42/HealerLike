using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneMotionSamplerTests
    {
        [Test]
        public void StationarySpawnDragTeleportAndVerticalMotionDoNothing()
        {
            HLStoneMotionSampler sampler = new HLStoneMotionSampler();
            Assert.AreEqual(Vector3.zero, sampler.Sample(Vector3.one * 10f, 0.02f, false));
            Assert.AreEqual(Vector3.zero, sampler.Sample(Vector3.one * 10f, 0.02f, false));
            Assert.AreEqual(Vector3.zero, sampler.Sample(Vector3.one * 20f, 0.02f, false));
            Assert.AreEqual(Vector3.zero, sampler.Sample(Vector3.one * 20f + Vector3.up, 0.02f, false));
            Assert.AreEqual(Vector3.zero, sampler.Sample(Vector3.one * 20f + Vector3.right * 0.1f, 0.02f, true));

            sampler.Reset();
            Assert.AreEqual(Vector3.zero, sampler.Sample(Vector3.zero, 0.02f, false));
            Assert.AreEqual(Vector3.right, sampler.Sample(Vector3.right * 0.1f, 0.1f, false));
        }
    }
}
