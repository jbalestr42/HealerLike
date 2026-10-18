using NUnit.Framework;

namespace UI
{

// The arrow above the marked enemy: bobs between its rest position and the amplitude above it
public class MarkIndicatorTests
{
    [Test]
    public void GetOffset_AtRest_AtTheStartOfEachPeriod()
    {
        Assert.AreEqual(0f, MarkIndicator.GetOffset(0f, 10f, 1f), 0.0001f);
        Assert.AreEqual(0f, MarkIndicator.GetOffset(3f, 10f, 1f), 0.0001f);
    }

    [Test]
    public void GetOffset_HighestHalfwayThroughThePeriod()
    {
        Assert.AreEqual(10f, MarkIndicator.GetOffset(0.5f, 10f, 1f), 0.0001f);
        Assert.AreEqual(10f, MarkIndicator.GetOffset(1f, 10f, 2f), 0.0001f);
    }

    [Test]
    public void GetOffset_NeverBelowRestNorAboveTheAmplitude()
    {
        for (float time = 0f; time < 2f; time += 0.05f)
        {
            float offset = MarkIndicator.GetOffset(time, 10f, 0.7f);
            Assert.GreaterOrEqual(offset, 0f);
            Assert.LessOrEqual(offset, 10f + 0.0001f);
        }
    }

    [Test]
    public void GetOffset_WithoutPeriod_StaysAtRest()
    {
        Assert.AreEqual(0f, MarkIndicator.GetOffset(0.3f, 10f, 0f));
    }
}

}
