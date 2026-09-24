using NUnit.Framework;

namespace HealerLike.Render.Stage
{

public class GrassBenchRunTests
{
    [Test]
    public void PercentileIndex_SampleFrames_IsTheSampleAtNinetyFivePercent()
    {
        int index = GrassBenchRun.PercentileIndex(GrassBenchRun.SampleFrames, GrassBenchRun.Percentile);

        Assert.AreEqual(284, index); // the 285th of 300 sorted samples
    }

    [Test]
    public void PercentileIndex_FewSamples_StaysInsideTheSamples()
    {
        Assert.AreEqual(0, GrassBenchRun.PercentileIndex(1, 0.95f));
        Assert.AreEqual(18, GrassBenchRun.PercentileIndex(20, 0.95f)); // the 19th of 20
        Assert.AreEqual(0, GrassBenchRun.PercentileIndex(20, 0f));
    }
}

}
