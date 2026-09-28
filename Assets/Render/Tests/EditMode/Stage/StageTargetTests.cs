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
        public void Default_NothingSelected_TargetsMain()
        {
            Assert.That(StageTarget.isSelected, Is.False);
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
        public void Reset_AfterSandbox_TargetsMainAgain()
        {
            StageTarget.Select(StageTarget.SandboxPath);

            StageTarget.Reset();

            Assert.That(StageTarget.isSelected, Is.False);
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
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
        public void Resolve_NothingSelectedOrAuthored_FallsBackToMain(string authored)
        {
            Assert.That(StageTarget.Resolve(authored), Is.EqualTo(StageTarget.MainPath));
        }

        [Test]
        public void Resolve_SandboxSelected_OverridesTheAuthoredScene()
        {
            StageTarget.Select(StageTarget.SandboxPath);

            Assert.That(StageTarget.Resolve(StageTarget.MainPath), Is.EqualTo(StageTarget.SandboxPath));
        }
    }
}
