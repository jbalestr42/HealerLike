using System;
using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    public class StageTargetTests
    {
        [SetUp]
        public void SetUp()
        {
            StageTarget.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            StageTarget.Reset();
        }

        [Test]
        public void Default_NothingSelected_BootsTheToolkitMenu()
        {
            Assert.That(StageTarget.isSelected, Is.False);
            Assert.That(StageTarget.scenePath, Is.EqualTo("Assets/Scenes/Toolkit/MenuToolkit.unity"));
        }

        [Test]
        public void BootPath_IsTheToolkitMenuTheStageAttaches()
        {
            Assert.That(StageTarget.BootPath, Is.EqualTo(StageTarget.MenuPath));
            Assert.That(StageTarget.MenuPath, Is.EqualTo(StageInterface.MenuPath));
            Assert.That(StageTarget.BootPath, Is.Not.EqualTo(StageTarget.MainPath));
        }

        [Test]
        public void Select_Main_TargetsMain()
        {
            StageTarget.Select(StageTarget.MainPath);

            Assert.That(StageTarget.isSelected, Is.True);
            Assert.That(StageTarget.scenePath, Is.EqualTo("Assets/Scenes/Main.unity"));
        }

        [Test]
        public void Select_Sandbox_TargetsSandbox()
        {
            StageTarget.Select(StageTarget.SandboxPath);

            Assert.That(StageTarget.isSelected, Is.True);
            Assert.That(StageTarget.scenePath, Is.EqualTo("Assets/Scenes/Sandbox.unity"));
        }

        [Test]
        public void Reset_AfterSandbox_BootsTheMenuAgain()
        {
            StageTarget.Select(StageTarget.SandboxPath);

            StageTarget.Reset();

            Assert.That(StageTarget.isSelected, Is.False);
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MenuPath));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Select_NoPath_IsRefusedAndKeepsTheTarget(string path)
        {
            StageTarget.Select(StageTarget.SandboxPath);

            Assert.Throws<ArgumentException>(() => StageTarget.Select(path));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.SandboxPath));
        }

        [Test]
        public void Resolve_NothingSelected_KeepsTheAuthoredScene()
        {
            Assert.That(StageTarget.Resolve("Assets/Scenes/Other.unity"), Is.EqualTo("Assets/Scenes/Other.unity"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Resolve_NothingSelectedOrAuthored_FallsBackToTheMenu(string authored)
        {
            Assert.That(StageTarget.Resolve(authored), Is.EqualTo(StageTarget.MenuPath));
        }

        [Test]
        public void Resolve_MainSelected_OverridesAnEmptyAuthoredScene()
        {
            StageTarget.Select(StageTarget.MainPath);

            Assert.That(StageTarget.Resolve(""), Is.EqualTo(StageTarget.MainPath));
        }

        [Test]
        public void Resolve_SandboxSelected_OverridesTheAuthoredScene()
        {
            StageTarget.Select(StageTarget.SandboxPath);

            Assert.That(StageTarget.Resolve(StageTarget.MainPath), Is.EqualTo(StageTarget.SandboxPath));
        }
    }
}
