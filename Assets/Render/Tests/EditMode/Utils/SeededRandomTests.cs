using NUnit.Framework;

namespace HealerLike.Render
{

public class SeededRandomTests
{
    [Test]
    public void Next_ZeroSeed_ReturnsPinnedSequenceInRange()
    {
        SeededRandom random = new SeededRandom(0);

        Assert.AreEqual(1013904223u, random.Next());
        Assert.AreEqual(1196435762u, random.Next());
        for (int i = 0; i < 1000; i++)
        {
            Assert.That(random.Next01(), Is.GreaterThanOrEqualTo(0).And.LessThan(1));
        }
    }

    [Test]
    public void ForPart_SameSeedAndSalt_MatchesPinnedFold()
    {
        uint seed = SeededRandom.ForPart(2166136261u, 1);

        Assert.AreEqual(67918732u, seed);
    }

    [Test]
    public void ForPart_OtherSalt_GivesAnotherSeed()
    {
        uint first = SeededRandom.ForPart(3, 1);

        uint second = SeededRandom.ForPart(3, 2);

        Assert.AreNotEqual(first, second);
    }
}

}
