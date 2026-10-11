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
    public void Update_Periodic_KeepsTheTimeBeyondThePeriod()
    {
        BuffHandler handler = CreateHandler(DurationType.Infinite, isPeriodic: true, periodDuration: 2f);

        handler.Update(1f);
        Assert.AreEqual(1f, handler.periodDurationTimer);

        handler.Update(1.5f);
        Assert.AreEqual(2.5f, handler.periodDurationTimer);
    }

    [Test]
    public void ConsumePeriod_KeepsTheTimeBeyondThePeriodForTheNextOne()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 10f, isPeriodic: true, periodDuration: 2f);
        handler.Update(2.5f);

        handler.ConsumePeriod();

        Assert.AreEqual(0.5f, handler.periodDurationTimer, 0.0001f);
        Assert.AreEqual(2.5f, handler.durationTimer);
        Assert.IsFalse(handler.isPeriodDone);
    }

    [Test]
    public void ConsumePeriod_WithoutPeriodDuration_RestartsThePeriod()
    {
        BuffHandler handler = CreateHandler(DurationType.Infinite, isPeriodic: true, periodDuration: 0f);
        handler.Update(0.3f);

        handler.ConsumePeriod();

        Assert.AreEqual(0f, handler.periodDurationTimer);
    }

    // Ticks of a 1s period over 6s, applied once per update like the BuffManager does
    static int CountTicks(float deltaTime)
    {
        BuffHandler handler = CreateHandler(DurationType.Infinite, isPeriodic: true, periodDuration: 1f);
        int ticks = 0;
        for (float played = 0f; played < 6f - 0.0001f; played += deltaTime)
        {
            handler.Update(deltaTime);
            if (handler.isPeriodDone)
            {
                ticks++;
                handler.ConsumePeriod();
            }
        }
        return ticks;
    }

    [Test]
    public void PeriodicTicks_AsManyWithLongUpdatesAsWithShortOnes()
    {
        Assert.AreEqual(6, CountTicks(0.125f));
        Assert.AreEqual(6, CountTicks(0.25f));
        Assert.AreEqual(6, CountTicks(0.75f));
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

    [Test]
    public void RemainingDuration_DurationType_IsTheTimeLeft()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 5f);

        handler.Update(1.5f);

        Assert.AreEqual(3.5f, handler.remainingDuration, 0.0001f);
    }

    [Test]
    public void RemainingDuration_AfterRefresh_IsTheWholeDuration()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 5f);
        handler.Update(4f);

        handler.Refresh(null, null);

        Assert.AreEqual(5f, handler.remainingDuration);
    }

    [Test]
    public void RemainingDuration_Elapsed_IsZero()
    {
        BuffHandler handler = CreateHandler(DurationType.Duration, duration: 5f);

        handler.Update(20f);

        Assert.AreEqual(0f, handler.remainingDuration);
    }

    [TestCase(DurationType.Infinite)]
    [TestCase(DurationType.Instant)]
    public void RemainingDuration_WithoutLimitedDuration_IsZero(DurationType durationType)
    {
        BuffHandler handler = CreateHandler(durationType, duration: 5f);

        handler.Update(1f);

        Assert.AreEqual(0f, handler.remainingDuration);
    }
}

}
