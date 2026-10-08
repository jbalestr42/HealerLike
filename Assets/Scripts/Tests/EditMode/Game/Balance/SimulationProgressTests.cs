using NUnit.Framework;

namespace Game.Balance
{

public class SimulationProgressTests
{
    [Test]
    public void GetRatio_IsThePartOfTheFightsDone()
    {
        Assert.AreEqual(0f, SimulationProgress.GetRatio(0, 40));
        Assert.AreEqual(0.25f, SimulationProgress.GetRatio(10, 40), 0.0001f);
        Assert.AreEqual(1f, SimulationProgress.GetRatio(40, 40));
    }

    [Test]
    public void GetRatio_WithoutFight_IsDone()
    {
        Assert.AreEqual(1f, SimulationProgress.GetRatio(0, 0));
    }

    [Test]
    public void EstimateRemaining_FollowsThePaceOfTheFightsDone()
    {
        // 10 fights in 100s: 30 more take 300s
        Assert.AreEqual(300f, SimulationProgress.EstimateRemaining(100f, 10, 40), 0.0001f);
        Assert.AreEqual(0f, SimulationProgress.EstimateRemaining(400f, 40, 40), 0.0001f);
    }

    [Test]
    public void EstimateRemaining_BeforeTheFirstFightIsDone_IsUnknown()
    {
        Assert.Less(SimulationProgress.EstimateRemaining(5f, 0, 40), 0f);
    }

    [TestCase(45f, "45s")]
    [TestCase(725f, "12m 05s")]
    [TestCase(3780f, "1h 03m")]
    [TestCase(-3f, "0s")]
    public void FormatDuration_IsShort(float seconds, string expected)
    {
        Assert.AreEqual(expected, SimulationProgress.FormatDuration(seconds));
    }

    [Test]
    public void DescribeShort_GivesThePercentAndTheFights()
    {
        Assert.AreEqual("3% 120/3744", SimulationProgress.DescribeShort(120, 3744));
    }

    [Test]
    public void Describe_GivesTheFightsThePercentAndTheTimes()
    {
        Assert.AreEqual("10/40 fights (25%), 1m 40s spent, ~5m 00s left", SimulationProgress.Describe(10, 40, 100f));
        StringAssert.Contains("time left unknown yet", SimulationProgress.Describe(0, 40, 5f));
    }
}

}
