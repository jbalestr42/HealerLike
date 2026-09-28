using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    public class ClassSelectCaptureRunTests
    {
        // Unset or blank keeps the Druid run the capture has always taken
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void PickFrom_NoVariable_IsTheDruid(string variable)
        {
            Assert.AreEqual("Druid", ClassSelectCaptureRun.PickFrom(variable));
        }

        [TestCase("Cleric", "Cleric")]
        [TestCase(" Warlock ", "Warlock")]
        public void PickFrom_Variable_IsThatClassTitle(string variable, string expected)
        {
            Assert.AreEqual(expected, ClassSelectCaptureRun.PickFrom(variable));
        }
    }
}
