using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceTests
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
            CharacterSelection.selected = null;
        }

        [Test]
        public void ScenePath_Sandbox_IsJuliensSandboxScene()
        {
            Assert.That(StageInterface.ScenePath("Sandbox"), Is.EqualTo("Assets/Scenes/Sandbox.unity"));
        }

        [Test]
        public void Route_Sandbox_SelectsTheSandboxTarget()
        {
            string path = StageInterface.Route("Sandbox");

            Assert.That(path, Is.EqualTo(StageTarget.SandboxPath));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.SandboxPath));
        }

        [Test]
        public void Route_StartAfterSandbox_TargetsMainAgain()
        {
            StageInterface.Route("Sandbox");

            string path = StageInterface.Route("Main");

            Assert.That(path, Is.EqualTo(StageTarget.MainPath));
            Assert.That(StageTarget.isSelected, Is.False);
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
        }

        // The class screen records the pick before the Start route runs; the route resets the scene, not the class
        [Test]
        public void Route_StartAfterAClassPick_KeepsThePick()
        {
            CharacterData druid = ScriptableObject.CreateInstance<CharacterData>();
            try
            {
                StageInterface.Route("Sandbox");
                CharacterSelection.selected = druid;

                string path = StageInterface.Route("Main");

                Assert.That(path, Is.EqualTo(StageTarget.MainPath));
                Assert.That(StageTarget.isSelected, Is.False);
                Assert.That(CharacterSelection.selected, Is.SameAs(druid));
            }
            finally
            {
                Object.DestroyImmediate(druid);
            }
        }

        [Test]
        public void Route_ToolkitStartName_AlsoTargetsMain()
        {
            StageInterface.Route("Sandbox");

            Assert.That(StageInterface.Route("MainToolkit"), Is.EqualTo(StageTarget.MainPath));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
        }

        [TestCase("MenuToolkit")]
        [TestCase("MenuScene")]
        public void Route_Menu_LeavesTheGameplayChoiceAlone(string menu)
        {
            StageInterface.Route("Sandbox");

            Assert.That(StageInterface.Route(menu), Is.EqualTo(StageInterface.MenuPath));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.SandboxPath));
        }

        [Test]
        public void Route_UnknownScene_LoadsNothingAndKeepsTheTarget()
        {
            StageInterface.Route("Sandbox");

            Assert.That(StageInterface.Route("Unrelated"), Is.Null);
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.SandboxPath));
        }

        [Test]
        public void AttachSandboxInput_SceneWithInteraction_HostsOneTouchInputFedWithIt()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            InteractionManager interaction = new GameObject("Managers").AddComponent<InteractionManager>();

            StageTouchInput first = StageInterface.AttachSandboxInput(scene);
            StageTouchInput second = StageInterface.AttachSandboxInput(scene);

            Assert.That(first, Is.Not.Null);
            Assert.That(first.gameObject.scene, Is.EqualTo(scene));
            Assert.That(first.gameObject.name, Is.EqualTo(StageInterface.SandboxInputName));
            Assert.That(TestHelpers.GetPrivateField<InteractionManager>(first, "_interaction"), Is.SameAs(interaction));
            Assert.That(second, Is.SameAs(first), "A reloaded or re-attached sandbox must not stack touch hosts.");
            Assert.That(Object.FindObjectsByType<StageTouchInput>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<ToolkitGameUI>(), Is.Null,
                "The Toolkit HUD would hide Julien's sandbox canvases.");
        }

        [Test]
        public void AttachSandboxInput_SceneWithoutInteraction_AddsNothing()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.That(StageInterface.AttachSandboxInput(scene), Is.Null);
            Assert.That(Object.FindAnyObjectByType<StageTouchInput>(), Is.Null);
        }
    }
}
