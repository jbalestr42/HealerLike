using NUnit.Framework;

namespace Game
{

public class SandboxTimeControlTests
{
    [Test]
    public void ByDefault_RunsAtNormalSpeed()
    {
        SandboxTimeControl control = new SandboxTimeControl();

        Assert.IsFalse(control.isPaused);
        Assert.AreEqual(1f, control.timeScale);
        Assert.AreEqual("Pause", control.pauseLabel);
        Assert.AreEqual("Speed x1", control.speedLabel);
    }

    [Test]
    public void TogglePause_StopsTheTime()
    {
        SandboxTimeControl control = new SandboxTimeControl();

        control.TogglePause();

        Assert.IsTrue(control.isPaused);
        Assert.AreEqual(0f, control.timeScale);
        Assert.AreEqual("Resume", control.pauseLabel);
    }

    [Test]
    public void TogglePauseTwice_ResumesAtTheSameSpeed()
    {
        SandboxTimeControl control = new SandboxTimeControl();
        control.NextSpeed();

        control.TogglePause();
        control.TogglePause();

        Assert.IsFalse(control.isPaused);
        Assert.AreEqual(0.5f, control.timeScale);
    }

    [Test]
    public void NextSpeed_SlowsDownThenGoesBackToNormal()
    {
        SandboxTimeControl control = new SandboxTimeControl();

        control.NextSpeed();
        Assert.AreEqual(0.5f, control.timeScale);
        Assert.AreEqual("Speed x0.5", control.speedLabel);

        control.NextSpeed();
        Assert.AreEqual(0.25f, control.timeScale);
        Assert.AreEqual("Speed x0.25", control.speedLabel);

        control.NextSpeed();
        Assert.AreEqual(1f, control.timeScale);
    }

    [Test]
    public void NextSpeed_WhilePaused_StaysPausedButKeepsTheNewSpeed()
    {
        SandboxTimeControl control = new SandboxTimeControl();
        control.TogglePause();

        control.NextSpeed();

        Assert.AreEqual(0f, control.timeScale);
        Assert.AreEqual("Speed x0.5", control.speedLabel);
    }
}

}
