using NUnit.Framework;

namespace Attributes.Consumers
{

public class RuntimeConsumerTests
{
    [Test]
    public void GetValue_IsTheGivenValue()
    {
        Assert.AreEqual(-12.5f, new RuntimeConsumer(-12.5f).GetValue());
    }

    [Test]
    public void Defaults_IgnoreReductionAndPreventionAndCanBeCritical()
    {
        RuntimeConsumer consumer = new RuntimeConsumer(10f);

        Assert.IsTrue(consumer.ignoreDamageReduction);
        Assert.IsTrue(consumer.ignoreConsumerPrevention);
        Assert.IsTrue(consumer.canBeCritical);
    }

    [Test]
    public void Settings_AreTheGivenOnes()
    {
        RuntimeConsumer consumer = new RuntimeConsumer(-10f, ignoreDamageReduction: false, ignoreConsumerPrevention: false, canBeCritical: false);

        Assert.IsFalse(consumer.ignoreDamageReduction);
        Assert.IsFalse(consumer.ignoreConsumerPrevention);
        Assert.IsFalse(consumer.canBeCritical);
    }

    [Test]
    public void Settings_EachOneIsIndependent()
    {
        // e.g. Soul Link: damage reduction ignored, but still preventable and never critical
        RuntimeConsumer consumer = new RuntimeConsumer(-10f, ignoreDamageReduction: true, ignoreConsumerPrevention: false, canBeCritical: false);

        Assert.IsTrue(consumer.ignoreDamageReduction);
        Assert.IsFalse(consumer.ignoreConsumerPrevention);
        Assert.IsFalse(consumer.canBeCritical);
    }
}

}
