using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneMotionSamplerTests
{
    [Test]
    public void Sample_StationarySpawnDragTeleportOrVertical_ReturnsZero()
    {
        StoneMotionSampler sampler = new StoneMotionSampler();
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
