using NUnit.Framework;

namespace HealerLike.Render.Stones
{
    public class HLStoneHealthStateTests
    {
        [Test]
        public void ThresholdOnceWithHealingAndReset()
        {
            HLStoneHealthState state = new HLStoneHealthState();
            state.Reset(0.5f);
            state.RecordProcessedDelta(-49f);
            Assert.AreEqual(HLStoneHealthAction.None, state.CompleteBatch(51f, 100f));

            state.RecordProcessedDelta(-1f);
            Assert.AreEqual(HLStoneHealthAction.ShedPart, state.CompleteBatch(50f, 100f));

            state.RecordProcessedDelta(50f);
            state.CompleteBatch(100f, 100f);
            state.RecordProcessedDelta(-70f);
            Assert.AreEqual(HLStoneHealthAction.None, state.CompleteBatch(30f, 100f));

            state.Reset(0.5f);
            state.RecordProcessedDelta(-50f);
            Assert.AreEqual(HLStoneHealthAction.ShedPart, state.CompleteBatch(50f, 100f));
        }

        [Test]
        public void FinalBatchWinsAndMaxChangeAloneDoesNotShed()
        {
            HLStoneHealthState state = new HLStoneHealthState();
            state.Reset(0.5f);
            Assert.AreEqual(HLStoneHealthAction.None, state.CompleteBatch(40f, 100f));

            state.RecordProcessedDelta(-80f);
            state.RecordProcessedDelta(80f);
            Assert.AreEqual(HLStoneHealthAction.None, state.CompleteBatch(100f, 100f));
            Assert.AreEqual(HLStoneHealthAction.None, state.CompleteBatch(40f, 100f));

            state.RecordProcessedDelta(-10f);
            Assert.AreEqual(HLStoneHealthAction.ShedPart, state.CompleteBatch(40f, 100f));
        }

        [Test]
        public void DeathPrecedesShedAndIsIdempotent()
        {
            HLStoneHealthState state = new HLStoneHealthState();
            state.RecordProcessedDelta(-100f);
            Assert.AreEqual(HLStoneHealthAction.Collapse, state.CompleteBatch(0f, 100f));
            Assert.IsFalse(state.TryBeginCollapse());
            Assert.AreEqual(HLStoneHealthAction.None, state.CompleteBatch(0f, 100f));

            state.Reset(0.5f);
            Assert.IsTrue(state.TryBeginCollapse());
            Assert.IsFalse(state.TryBeginCollapse());
        }
    }
}
