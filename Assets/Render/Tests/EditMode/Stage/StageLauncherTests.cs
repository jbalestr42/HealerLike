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
        public void ScenePath_NoTarget_BootsTheToolkitMenu()
        {
            Assert.That(_launcher.scenePath, Is.EqualTo("Assets/Scenes/Toolkit/MenuToolkit.unity"));
        }

        // The launcher RenderStage boots is the one on the RenderManager prefab: it must not pin Main
        [Test]
        public void ScenePath_TheStagePrefabsLauncher_BootsTheToolkitMenu()
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Render/Stage/Prefabs/RenderManager.prefab");
            Assert.That(prefab, Is.Not.Null);
            StageLauncher launcher = prefab.GetComponent<StageLauncher>();
            Assert.That(launcher, Is.Not.Null);

            Assert.That(launcher.scenePath, Is.EqualTo(StageTarget.MenuPath));
        }

        [Test]
        public void ScenePath_MainTarget_LoadsMain()
        {
            StageTarget.Select(StageTarget.MainPath);

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
        public void ScenePath_TargetResetAfterSandbox_BootsTheMenuAgain()
        {
            StageTarget.Select(StageTarget.SandboxPath);
            StageTarget.Reset();

            Assert.That(_launcher.scenePath, Is.EqualTo(StageTarget.MenuPath));
        }
    }
}
