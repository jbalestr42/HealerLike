using NUnit.Framework;

namespace HealerLike.Render.Studio.Editor
{

public class StudioTimelineTests
{
    StudioTimeline _timeline;

    [SetUp]
    public void SetUp()
    {
        _timeline = new StudioTimeline();
        _timeline.Init();
    }

    [Test]
    public void Toggle_Playing_Pauses()
    {
        _timeline.Toggle(2f);

        Assert.IsFalse(_timeline.isPlaying);
    }

    [Test]
    public void Toggle_StoppedAtTheEnd_PlaysFromTheStart()
    {
        _timeline.Toggle(2f);
        _timeline.time = 2f;

        _timeline.Toggle(2f);

        Assert.IsTrue(_timeline.isPlaying);
        Assert.AreEqual(0f, _timeline.time);
    }

    [Test]
    public void Toggle_StoppedMidway_ResumesWhereItStopped()
    {
        _timeline.Toggle(2f);
        _timeline.time = 1.2f;

        _timeline.Toggle(2f);

        Assert.AreEqual(1.2f, _timeline.time);
    }

    [Test]
    public void Tick_NoSubject_KeepsTheTime()
    {
        _timeline.time = 0.5f;

        bool isAdvanced = _timeline.Tick(2f, false);

        Assert.IsFalse(isAdvanced);
        Assert.AreEqual(0.5f, _timeline.time);
    }

    [Test]
    public void Readout_HalfwayThroughTwoSeconds_ShowsBothTimes()
    {
        _timeline.time = 1f;

        string readout = _timeline.Readout(2f);

        StringAssert.StartsWith("1.00 / 2.00 s", readout);
    }
}

}
