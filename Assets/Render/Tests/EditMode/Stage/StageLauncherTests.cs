using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Start is never run here: it would load a scene. The path it loads is scenePath.
    public class StageLauncherTests
    {
        GameObject _host;
        StageLauncher _launcher;

        [SetUp]
        public void SetUp()
        {
            StageTarget.Reset();
            _host = new GameObject("Stage launcher test");
            _launcher = _host.AddComponent<StageLauncher>();
        }

        [TearDown]
        public void TearDown()
        {
            StageTarget.Reset();
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void ScenePath_NoTarget_LoadsMainAsAuthored()
        {
            Assert.That(_launcher.scenePath, Is.EqualTo("Assets/Scenes/Main.unity"));
        }

        [Test]
        public void ScenePath_NoTarget_KeepsAnotherAuthoredScene()
        {
            TestHelpers.SetPrivateField(_launcher, "_scenePath", "Assets/Scenes/Other.unity");

            Assert.That(_launcher.scenePath, Is.EqualTo("Assets/Scenes/Other.unity"));
        }

        [Test]
        public void ScenePath_SandboxTarget_LoadsTheSandbox()
        {
            StageTarget.Select(StageTarget.SandboxPath);

            Assert.That(_launcher.scenePath, Is.EqualTo("Assets/Scenes/Sandbox.unity"));
        }

        [Test]
        public void ScenePath_TargetResetAfterSandbox_LoadsMainAgain()
        {
            StageTarget.Select(StageTarget.SandboxPath);
            StageTarget.Reset();

            Assert.That(_launcher.scenePath, Is.EqualTo(StageTarget.MainPath));
        }
    }
}
