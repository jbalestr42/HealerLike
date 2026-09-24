using NUnit.Framework;

namespace HealerLike.Render.Stones
{

public class StoneSeedTests
{
    [Test]
    public void ForPart_SameSeedAndSalt_MatchesPinnedFold()
    {
        uint seed = StoneSeed.ForPart(2166136261u, 1);

        Assert.AreEqual(67918732u, seed);
    }

    [Test]
    public void ForPart_OtherSalt_GivesAnotherSeed()
    {
        uint first = StoneSeed.ForPart(3, 1);

        uint second = StoneSeed.ForPart(3, 2);

        Assert.AreNotEqual(first, second);
    }
}

}
