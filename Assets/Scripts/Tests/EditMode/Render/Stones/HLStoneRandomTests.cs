using NUnit.Framework;
namespace HealerLike.Render.Stones
{
    public class HLStoneRandomTests
    {
        [Test] public void ZeroSeedIsValidAndSequencePinned()
        {
            var r=new HLStoneRandom(0); Assert.AreEqual(1013904223u,r.Next()); Assert.AreEqual(1196435762u,r.Next());
            for(int i=0;i<1000;i++) Assert.That(r.Next01(),Is.GreaterThanOrEqualTo(0).And.LessThan(1));
        }
    }
}
