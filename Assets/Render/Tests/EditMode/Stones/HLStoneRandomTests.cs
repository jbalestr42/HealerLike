using NUnit.Framework;

namespace HealerLike.Render.Stones
{
    public class HLStoneRandomTests
    {
        [Test]
        public void ZeroSeedIsValidAndSequencePinned()
        {
            HLStoneRandom random = new HLStoneRandom(0);
            Assert.AreEqual(1013904223u, random.Next());
            Assert.AreEqual(1196435762u, random.Next());
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(random.Next01(), Is.GreaterThanOrEqualTo(0).And.LessThan(1));
            }
        }
    }
}
