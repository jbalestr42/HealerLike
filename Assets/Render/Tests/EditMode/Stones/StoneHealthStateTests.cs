using NUnit.Framework;

namespace HealerLike.Render.Stones
{

public class StoneHealthStateTests
{
    [Test]
    public void ThresholdOnceWithHealingAndReset()
    {
        StoneHealthState state = new StoneHealthState();
        state.Reset(0.5f);
        state.RecordProcessedDelta(-49f);
        Assert.AreEqual(StoneHealthAction.None, state.CompleteBatch(51f, 100f));

        state.RecordProcessedDelta(-1f);
        Assert.AreEqual(StoneHealthAction.ShedPart, state.CompleteBatch(50f, 100f));

        state.RecordProcessedDelta(50f);
        state.CompleteBatch(100f, 100f);
        state.RecordProcessedDelta(-70f);
        Assert.AreEqual(StoneHealthAction.None, state.CompleteBatch(30f, 100f));

        state.Reset(0.5f);
        state.RecordProcessedDelta(-50f);
        Assert.AreEqual(StoneHealthAction.ShedPart, state.CompleteBatch(50f, 100f));
    }

    [Test]
    public void FinalBatchWinsAndMaxChangeAloneDoesNotShed()
    {
        StoneHealthState state = new StoneHealthState();
        state.Reset(0.5f);
        Assert.AreEqual(StoneHealthAction.None, state.CompleteBatch(40f, 100f));

        state.RecordProcessedDelta(-80f);
        state.RecordProcessedDelta(80f);
        Assert.AreEqual(StoneHealthAction.None, state.CompleteBatch(100f, 100f));
        Assert.AreEqual(StoneHealthAction.None, state.CompleteBatch(40f, 100f));

        state.RecordProcessedDelta(-10f);
        Assert.AreEqual(StoneHealthAction.ShedPart, state.CompleteBatch(40f, 100f));
    }

    [Test]
    public void DeathPrecedesShedAndIsIdempotent()
    {
        StoneHealthState state = new StoneHealthState();
        state.RecordProcessedDelta(-100f);
        Assert.AreEqual(StoneHealthAction.Collapse, state.CompleteBatch(0f, 100f));
        Assert.IsFalse(state.TryBeginCollapse());
        Assert.AreEqual(StoneHealthAction.None, state.CompleteBatch(0f, 100f));

        state.Reset(0.5f);
        Assert.IsTrue(state.TryBeginCollapse());
        Assert.IsFalse(state.TryBeginCollapse());
    }
}

}
