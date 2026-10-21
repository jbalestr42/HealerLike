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
            Assert.That(StageTarget.isSelected, Is.True);
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
        }

        // A plain boot has nothing selected (the menu); Start there records Main, Sandbox records the sandbox
        [Test]
        public void Route_FromAMenuBoot_StartSelectsMainAndSandboxSelectsTheSandbox()
        {
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageInterface.MenuPath));

            Assert.That(StageInterface.Route("MenuToolkit"), Is.EqualTo(StageInterface.MenuPath));
            Assert.That(StageTarget.isSelected, Is.False);

            Assert.That(StageInterface.Route("Main"), Is.EqualTo(StageTarget.MainPath));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));

            StageTarget.Reset();
            Assert.That(StageInterface.Route("Sandbox"), Is.EqualTo(StageTarget.SandboxPath));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.SandboxPath));
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
                Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
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

        // The battle HUD's world viewport, and the same viewport while a targeted spell's Cancel row is shown
        static readonly Rect battleViewport = new Rect(0.02f, 0.33f, 0.96f, 0.59f);
        static readonly Rect aimingViewport = new Rect(0.02f, 0.40f, 0.96f, 0.52f);

        // Arming a targeted heal must not reframe the camera: the framed viewport holds while the aim lasts
        [Test]
        public void FramedViewport_WhileAiming_KeepsTheFramedViewport()
        {
            Rect viewport = StageInterface.FramedViewport(battleViewport, aimingViewport, true, true);

            Assert.That(viewport, Is.EqualTo(battleViewport));
        }

        // Once the spell is cast or cancelled the camera follows the HUD again
        [Test]
        public void FramedViewport_NotAiming_TakesTheMeasuredViewport()
        {
            Rect viewport = StageInterface.FramedViewport(aimingViewport, battleViewport, false, true);

            Assert.That(viewport, Is.EqualTo(battleViewport));
        }

        // Aim then cast lands on the viewport the battle was framed to, so the pair leaves the camera where it was
        [Test]
        public void FramedViewport_AimThenCast_EndsOnTheBattleViewport()
        {
            Rect aimed = StageInterface.FramedViewport(battleViewport, aimingViewport, true, true);
            Rect cast = StageInterface.FramedViewport(aimed, battleViewport, false, true);

            Assert.That(aimed, Is.EqualTo(battleViewport));
            Assert.That(cast, Is.EqualTo(battleViewport));
        }

        // Nothing framed yet (first frame, or a new camera): an armed interaction cannot hold an empty viewport
        [TestCase(false)]
        [TestCase(true)]
        public void FramedViewport_AimingWithNothingFramed_TakesTheMeasuredViewport(bool hasFramed)
        {
            Rect framed = hasFramed ? Rect.zero : battleViewport;

            Rect viewport = StageInterface.FramedViewport(framed, aimingViewport, true, hasFramed);

            Assert.That(viewport, Is.EqualTo(aimingViewport));
        }
    }
}
