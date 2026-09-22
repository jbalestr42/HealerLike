using NUnit.Framework;
namespace HealerLike.Render.Stones
{
    public class HLStoneHealthStateTests
    {
        [Test] public void ThresholdOnceWithHealingAndReset()
        {
            var s=new HLStoneHealthState(); s.Reset(.5f); s.RecordProcessedDelta(-49);
            Assert.AreEqual(HLStoneHealthAction.None,s.CompleteBatch(51,100)); s.RecordProcessedDelta(-1);
            Assert.AreEqual(HLStoneHealthAction.ShedPart,s.CompleteBatch(50,100)); s.RecordProcessedDelta(50); s.CompleteBatch(100,100);
            s.RecordProcessedDelta(-70); Assert.AreEqual(HLStoneHealthAction.None,s.CompleteBatch(30,100));
            s.Reset(.5f); s.RecordProcessedDelta(-50); Assert.AreEqual(HLStoneHealthAction.ShedPart,s.CompleteBatch(50,100));
        }
        [Test] public void FinalBatchWinsAndMaxChangeAloneDoesNotShed()
        {
            var s=new HLStoneHealthState(); s.Reset(.5f);
            Assert.AreEqual(HLStoneHealthAction.None,s.CompleteBatch(40,100));
            s.RecordProcessedDelta(-80); s.RecordProcessedDelta(80);
            Assert.AreEqual(HLStoneHealthAction.None,s.CompleteBatch(100,100));
            Assert.AreEqual(HLStoneHealthAction.None,s.CompleteBatch(40,100));
            s.RecordProcessedDelta(-10); Assert.AreEqual(HLStoneHealthAction.ShedPart,s.CompleteBatch(40,100));
        }
        [Test] public void DeathPrecedesShedAndIsIdempotent()
        {
            var s=new HLStoneHealthState(); s.RecordProcessedDelta(-100);
            Assert.AreEqual(HLStoneHealthAction.Collapse,s.CompleteBatch(0,100)); Assert.IsFalse(s.TryBeginCollapse());
            Assert.AreEqual(HLStoneHealthAction.None,s.CompleteBatch(0,100));
            s.Reset(.5f); Assert.IsTrue(s.TryBeginCollapse()); Assert.IsFalse(s.TryBeginCollapse());
        }
    }
}
