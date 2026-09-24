using NUnit.Framework;

namespace UI.Toolkit
{

public class ToolkitPresentationTests
{
    [TestCase(10f, 0f, 0f)]
    [TestCase(10f, -1f, 0f)]
    [TestCase(-5f, 100f, 0f)]
    [TestCase(50f, 100f, 50f)]
    [TestCase(150f, 100f, 100f)]
    public void Percentage_InvalidOrOutOfRange_ClampsToPercent(float value, float maximum, float expected)
    {
        float percentage = ToolkitPresentation.Percentage(value, maximum);

        Assert.AreEqual(expected, percentage);
    }

    [Test]
    public void Resource_FractionalValues_RoundsToWholeNumbers()
    {
        string text = ToolkitPresentation.Resource(12.4f, 30f);

        Assert.AreEqual("12 / 30", text);
    }

    [Test]
    public void SkillStatus_ReadyWithCost_ShowsManaAndReady()
    {
        string status = ToolkitPresentation.SkillStatus(true, "25", false, null);

        Assert.AreEqual("25 mana · Ready", status);
    }

    [Test]
    public void SkillStatus_NoCost_ShowsFree()
    {
        string status = ToolkitPresentation.SkillStatus(false, null, false, null);

        Assert.AreEqual("Free · Ready", status);
    }

    [Test]
    public void SkillStatus_CoolingDown_ShowsCooldownInsteadOfReady()
    {
        string status = ToolkitPresentation.SkillStatus(true, "25", true, "2s");

        Assert.AreEqual("25 mana · 2s", status);
    }

    [Test]
    public void SkillStatus_EmptyCooldown_ShowsReady()
    {
        string status = ToolkitPresentation.SkillStatus(true, "25", true, "");

        Assert.AreEqual("25 mana · Ready", status);
    }
}
}
