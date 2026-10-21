using System.Reflection;
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

        static bool BootsIntoGame(ClassSelectCaptureRun run)
        {
            return (bool)typeof(ClassSelectCaptureRun)
                .GetProperty("bootsIntoGame", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(run);
        }

        // The class-select run boots Main and goes to the menu from its HUD, so it waits for Main as before
        [Test]
        public void ClassSelectRun_WaitsForMain()
        {
            Assert.That(BootsIntoGame(new ClassSelectCaptureRun()), Is.True);
        }

        // The menu boot run starts on the menu, where no EntityManager exists: it must not wait for Main
        [Test]
        public void MenuBootRun_DoesNotWaitForMain()
        {
            Assert.That(BootsIntoGame(new ClassSelectCaptureRun(true)), Is.False);
        }
    }
}
