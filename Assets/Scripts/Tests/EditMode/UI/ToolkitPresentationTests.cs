using NUnit.Framework;

namespace HealerLike.UI.Toolkit.Tests
{
    public class ToolkitPresentationTests
    {
        [TestCase(10f, 0f, 0f)]
        [TestCase(10f, -1f, 0f)]
        [TestCase(-5f, 100f, 0f)]
        [TestCase(50f, 100f, 50f)]
        [TestCase(150f, 100f, 100f)]
        public void PercentageHandlesInvalidAndOutOfRangeResources(float value, float maximum, float expected)
        {
            Assert.That(ToolkitPresentation.Percentage(value, maximum), Is.EqualTo(expected));
        }

        [Test]
        public void ReadySpellIncludesResourceCost()
        {
            Assert.That(ToolkitPresentation.SkillStatus(25f, 0f, true), Is.EqualTo("25 mana · Ready"));
        }

        [Test]
        public void FreeSpellDoesNotMisrepresentResourceCost()
        {
            Assert.That(ToolkitPresentation.SkillStatus(25f, 0f, false), Is.EqualTo("Free · Ready"));
        }

        [Test]
        public void CoolingDownSpellIsNotReportedReady()
        {
            StringAssert.Contains("mana", ToolkitPresentation.SkillStatus(25f, 2f, true));
            StringAssert.DoesNotContain("Ready", ToolkitPresentation.SkillStatus(25f, 2f, true));
        }
    }
}
