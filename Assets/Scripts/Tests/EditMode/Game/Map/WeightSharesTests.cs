using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Game.Map
{

// Changing one share rescales the others so the shares always add up to 100%
public class WeightSharesTests
{
    static float Total(List<float> weights)
    {
        return weights.Sum();
    }

    [Test]
    public void GetShare_IsTheWeightOverTheTotal()
    {
        List<float> weights = new List<float> { 1.25f, 0.25f, 0.5f };

        Assert.AreEqual(0.625f, WeightShares.GetShare(weights, 0), 0.0001f);
        Assert.AreEqual(0.125f, WeightShares.GetShare(weights, 1), 0.0001f);
        Assert.AreEqual(0.25f, WeightShares.GetShare(weights, 2), 0.0001f);
    }

    [Test]
    public void GetShare_NoWeight_IsZero()
    {
        Assert.AreEqual(0f, WeightShares.GetShare(new List<float> { 0f, 0f }, 0));
    }

    [Test]
    public void SetShare_TheOthersKeepTheirProportionsAndTheTotalIs100Percent()
    {
        // Combat 60%, Elite 20%, Rest 10%, Event 10%
        List<float> weights = new List<float> { 0.6f, 0.2f, 0.1f, 0.1f };

        WeightShares.SetShare(weights, 0, 0.8f);

        Assert.AreEqual(0.8f, weights[0], 0.0001f);
        // The 40% left became 20%, split 2 / 1 / 1 like before
        Assert.AreEqual(0.1f, weights[1], 0.0001f);
        Assert.AreEqual(0.05f, weights[2], 0.0001f);
        Assert.AreEqual(0.05f, weights[3], 0.0001f);
        Assert.AreEqual(1f, Total(weights), 0.0001f);
    }

    [Test]
    public void SetShare_LoweringOneRaisesTheOthers()
    {
        List<float> weights = new List<float> { 0.5f, 0.25f, 0.25f };

        WeightShares.SetShare(weights, 0, 0.2f);

        Assert.AreEqual(0.2f, weights[0], 0.0001f);
        Assert.AreEqual(0.4f, weights[1], 0.0001f);
        Assert.AreEqual(0.4f, weights[2], 0.0001f);
    }

    [Test]
    public void SetShare_FromRelativeWeights_GivesShares()
    {
        // Weights not adding up to 1 yet (e.g. 1.25 / 0.25 / 0.5): they become shares
        List<float> weights = new List<float> { 1.25f, 0.25f, 0.5f };

        WeightShares.SetShare(weights, 1, 0.25f);

        Assert.AreEqual(0.25f, weights[1], 0.0001f);
        Assert.AreEqual(0.75f * 1.25f / 1.75f, weights[0], 0.0001f);
        Assert.AreEqual(0.75f * 0.5f / 1.75f, weights[2], 0.0001f);
        Assert.AreEqual(1f, Total(weights), 0.0001f);
    }

    [Test]
    public void SetShare_OthersAllZero_SplitWhatsLeftEvenly()
    {
        List<float> weights = new List<float> { 1f, 0f, 0f };

        WeightShares.SetShare(weights, 0, 0.6f);

        Assert.AreEqual(0.6f, weights[0], 0.0001f);
        Assert.AreEqual(0.2f, weights[1], 0.0001f);
        Assert.AreEqual(0.2f, weights[2], 0.0001f);
    }

    [Test]
    public void SetShare_100Percent_TheOthersDropToZero()
    {
        List<float> weights = new List<float> { 0.5f, 0.3f, 0.2f };

        WeightShares.SetShare(weights, 2, 1f);

        CollectionAssert.AreEqual(new[] { 0f, 0f, 1f }, weights);
    }

    [Test]
    public void SetShare_OutOfRange_IsClamped()
    {
        List<float> weights = new List<float> { 0.5f, 0.5f };

        WeightShares.SetShare(weights, 0, 1.5f);
        Assert.AreEqual(1f, weights[0], 0.0001f);

        WeightShares.SetShare(weights, 0, -0.5f);
        Assert.AreEqual(0f, weights[0], 0.0001f);
        Assert.AreEqual(1f, weights[1], 0.0001f);
    }

    [Test]
    public void SetShare_SingleWeight_IsAlways100Percent()
    {
        List<float> weights = new List<float> { 0.3f };

        WeightShares.SetShare(weights, 0, 0.4f);

        Assert.AreEqual(1f, weights[0]);
    }
}

}
