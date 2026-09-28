using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    // A plain boot opens the Toolkit menu, so each capture session names the scene it boots
    public class StagePlayTests
    {
        [TestCase("portrait")]
        [TestCase("landscape")]
        [TestCase("smoke")]
        [TestCase("mobile-interface")]
        [TestCase("expedition-map")]
        [TestCase("creature-presentation")]
        [TestCase("compact-ui")]
        [TestCase("spell-polish")]
        [TestCase("units")]
        [TestCase("grassbattle")]
        [TestCase("class-select")]
        [TestCase("sandbox-menu")]
        public void BootTarget_RunsThatPlayMain_SelectMainExplicitly(string mode)
        {
            Assert.That(StagePlay.BootTarget(mode), Is.EqualTo(StageTarget.MainPath));
        }

        [TestCase("sandbox")]
        [TestCase("sandbox-back")]
        public void BootTarget_SandboxRuns_SelectTheSandbox(string mode)
        {
            Assert.That(StagePlay.BootTarget(mode), Is.EqualTo(StageTarget.SandboxPath));
        }

        // The menu boot run proves the plain boot: it selects nothing, so the stage opens the menu
        [Test]
        public void BootTarget_MenuBootRun_KeepsThePlainBoot()
        {
            Assert.That(StagePlay.BootTarget(ClassSelectCaptureRun.BootMode), Is.Null);
            Assert.That(StageTarget.Resolve(""), Is.EqualTo(StageTarget.MenuPath));
        }

        [Test]
        public void BootTarget_ModesAreTheOnesTheRunsRegister()
        {
            Assert.That(ClassSelectCaptureRun.BootMode, Is.EqualTo("menu-boot"));
            Assert.That(ClassSelectCaptureRun.BootMode, Is.Not.EqualTo(ClassSelectCaptureRun.Mode));
        }
    }
}
