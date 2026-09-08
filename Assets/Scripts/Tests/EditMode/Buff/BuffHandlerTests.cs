using NUnit.Framework;

namespace Buff
{

public class BuffHandlerTests
{
    static BuffHandler CreateHandler(DurationType durationType, float duration = 0f, bool isPeriodic = false, float periodDuration = 0f)
    {
        BuffHandler handler = new BuffHandler
        {
            data = new BuffHandlerData
            {
                durationType = durationType,
                duration = duration,
                isPeriodic = isPeriodic,
                periodDuration = periodDuration,
            }
        };
        handler.Start(null, null);
        return handler;
    }

    [Test]
    public void Update_DurationType_AccumulatesDurationTimer()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 10f);

        handler.Update(3f);

        Assert.AreEqual(3f, handler.durationTimer);
    }

    [Test]
    public void Update_DurationType_ClampsDurationTimerAtDuration()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 5f);

        handler.Update(20f);

        Assert.AreEqual(5f, handler.durationTimer);
    }

    [Test]
    public void IsDone_DurationType_BecomesTrueOnceDurationElapsed()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 5f);

        handler.Update(4f);
        Assert.IsFalse(handler.isDone);

        handler.Update(1f);
        Assert.IsTrue(handler.isDone);
    }

    [Test]
    public void IsDone_InfiniteType_NeverBecomesTrue()
    {
        BuffHandler handler = CreateHandler(DurationType.Infinite);

        handler.Update(1000f);

        Assert.IsFalse(handler.isDone);
    }

    [Test]
    public void IsDone_InstantType_IsAlwaysTrue()
    {
        BuffHandler handler = CreateHandler(DurationType.Instant);

        Assert.IsTrue(handler.isDone);
    }

    [Test]
    public void HasDuration_InstantType_IsFalse()
    {
        BuffHandler handler = CreateHandler(DurationType.Instant);

        Assert.IsFalse(handler.hasDuration);
    }

    [Test]
    public void HasDuration_DurationAndInfiniteTypes_AreTrue()
    {
        Assert.IsTrue(CreateHandler(DurationType.Duration, duration: 1f).hasDuration);
        Assert.IsTrue(CreateHandler(DurationType.Infinite).hasDuration);
    }

    [Test]
    public void Update_Periodic_AccumulatesAndClampsPeriodDurationTimer()
    {
        BuffHandler handler = CreateHandler(DurationType.Infinite, isPeriodic: true, periodDuration: 2f);

        handler.Update(1f);
        Assert.AreEqual(1f, handler.periodDurationTimer);

        handler.Update(5f);
        Assert.AreEqual(2f, handler.periodDurationTimer);
    }

    [Test]
    public void IsPeriodDone_BecomesTrueOncePeriodDurationElapsed()
    {
        BuffHandler handler = CreateHandler(DurationType.Infinite, isPeriodic: true, periodDuration: 2f);

        handler.Update(1f);
        Assert.IsFalse(handler.isPeriodDone);

        handler.Update(1f);
        Assert.IsTrue(handler.isPeriodDone);
    }

    [Test]
    public void ResetPeriodDuration_ResetsPeriodTimerButNotDurationTimer()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 10f, isPeriodic: true, periodDuration: 2f);
        handler.Update(3f);

        handler.ResetPeriodDuration();

        Assert.AreEqual(0f, handler.periodDurationTimer);
        Assert.AreEqual(3f, handler.durationTimer);
    }

    [Test]
    public void Refresh_ResetsDurationTimer()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 10f);
        handler.Update(4f);

        handler.Refresh(null, null);

        Assert.AreEqual(0f, handler.durationTimer);
    }

    [Test]
    public void Start_ResetsBothTimers()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 10f, isPeriodic: true, periodDuration: 2f);
        handler.Update(4f);

        handler.Start(null, null);

        Assert.AreEqual(0f, handler.durationTimer);
        Assert.AreEqual(0f, handler.periodDurationTimer);
    }
}

}
