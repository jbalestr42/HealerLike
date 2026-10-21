using System.Collections.Generic;
using NUnit.Framework;
using UI.Toolkit;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // The sandbox's way back: a Menu button and the back gesture, only in the sandbox, loading the Toolkit menu
    // through the stage's loader at normal time, after which Start and Sandbox work as before
    public class StageSandboxExitTests
    {
        class ArmedInteraction : AInteraction
        {
            public override int GetLayerMask()
            {
                return Physics.DefaultRaycastLayers;
            }
        }

        readonly List<string> _loads = new List<string>();
        readonly List<Object> _created = new List<Object>();
        float _speed;

        [SetUp]
        public void SetUp()
        {
            _speed = Time.timeScale;
            _loads.Clear();
            StageTarget.Reset();
            CharacterSelection.selected = null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _speed;
            StageTarget.Reset();
            CharacterSelection.selected = null;
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        bool RecordLoad(string scene)
        {
            _loads.Add(scene);
            return true;
        }

        // The stage's real routing behind a recording loader, as StageInterface.LoadScene routes before it loads
        bool RouteLoad(string scene)
        {
            _loads.Add(scene);
            return StageInterface.Route(scene) != null;
        }

        StageInterface CreateInterface()
        {
            GameObject host = new GameObject("Render Manager");
            _created.Add(host);
            StageInterface stage = host.AddComponent<StageInterface>();
            stage.Init(null, host.AddComponent<BattleFocus>());
            return stage;
        }

        static Scene SandboxLikeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Managers").AddComponent<InteractionManager>();
            return scene;
        }

        StageSandboxExit CreateExit(InteractionManager interaction = null)
        {
            GameObject host = new GameObject(StageSandboxExit.HostName);
            _created.Add(host);
            StageSandboxExit exit = host.AddComponent<StageSandboxExit>();
            exit.Init(RecordLoad, StageInterface.MenuScene, interaction);
            return exit;
        }

        [Test]
        public void HostsSandboxExit_OnlyForTheSandbox()
        {
            Assert.That(StageInterface.HostsSandboxExit(StageTarget.SandboxPath), Is.True);
            Assert.That(StageInterface.HostsSandboxExit(StageInterface.GameplayPath), Is.False);
            Assert.That(StageInterface.HostsSandboxExit(StageInterface.MenuPath), Is.False);
            Assert.That(StageInterface.HostsSandboxExit(null), Is.False);
        }

        [Test]
        public void Attach_Sandbox_AddsOneMenuExitToTheSandboxScene()
        {
            Scene scene = SandboxLikeScene();
            StageInterface stage = CreateInterface();

            stage.Attach(scene, StageTarget.SandboxPath);
            stage.Attach(scene, StageTarget.SandboxPath);

            StageSandboxExit[] exits = Object.FindObjectsByType<StageSandboxExit>(FindObjectsSortMode.None);
            Assert.That(exits.Length, Is.EqualTo(1), "A re-attached sandbox must not stack Menu buttons.");
            Assert.That(exits[0].gameObject.scene, Is.EqualTo(scene), "The exit must unload with the sandbox.");
            Assert.That(exits[0].menuScene, Is.EqualTo(StageInterface.MenuScene));
            Assert.That(exits[0].button, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ToolkitGameUI>(), Is.Null,
                "The Toolkit HUD would hide Julien's sandbox canvases.");
        }

        [Test]
        public void Attach_Menu_AddsNoSandboxExit()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            StageInterface stage = CreateInterface();

            stage.Attach(scene, StageInterface.MenuPath);

            Assert.That(Object.FindAnyObjectByType<StageSandboxExit>(), Is.Null);
            Assert.That(Object.FindAnyObjectByType<ToolkitGameUI>(), Is.Not.Null);
        }

        [Test]
        public void Attach_UnrelatedScene_AddsNoSandboxExit()
        {
            Scene scene = SandboxLikeScene();
            StageInterface stage = CreateInterface();

            stage.Attach(scene, "Assets/Scenes/Other.unity");

            Assert.That(Object.FindAnyObjectByType<StageSandboxExit>(), Is.Null);
        }

        // Main is loaded in Single mode, which unloads the sandbox scene and the exit it hosts
        [Test]
        public void Exit_NextSceneLoaded_IsGoneWithTheSandbox()
        {
            Scene scene = SandboxLikeScene();
            CreateInterface().Attach(scene, StageTarget.SandboxPath);
            Assert.That(Object.FindAnyObjectByType<StageSandboxExit>(), Is.Not.Null);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.That(Object.FindAnyObjectByType<StageSandboxExit>(), Is.Null);
        }

        [Test]
        public void Overlay_IsAThemedMenuButtonOverAnUnpickableLayer()
        {
            StageSandboxExit exit = CreateExit();

            Assert.That(exit.button.name, Is.EqualTo(StageSandboxExit.ButtonName));
            Assert.That(exit.button.text, Is.EqualTo("Menu"));
            Assert.That(exit.button.ClassListContains("button"), Is.True);
            Assert.That(exit.overlay.styleSheets.Contains(Resources.Load<StyleSheet>(StageSandboxExit.StyleSheetName)),
                Is.True, "The button takes the menu's pill look from the render stylesheet.");
            Assert.That(exit.overlay.pickingMode, Is.EqualTo(PickingMode.Ignore),
                "The layer must not swallow board taps outside the button.");
            Assert.That(exit.button.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(exit.button.parent, Is.SameAs(exit.overlay));
        }

        [Test]
        public void MenuButton_Pressed_LoadsTheMenuThroughTheLoader()
        {
            StageSandboxExit exit = CreateExit();
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                panel.root.Add(exit.overlay);

                ToolkitTestPanel.Submit(exit.button);
            }

            CollectionAssert.AreEqual(new[] { StageInterface.MenuScene }, _loads);
            Assert.That(StageInterface.ScenePath(_loads[0]), Is.EqualTo(StageInterface.MenuPath));
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        public void MenuButton_Pressed_RestoresNormalTime(float sandboxSpeed)
        {
            StageSandboxExit exit = CreateExit();
            Time.timeScale = sandboxSpeed;
            float speedAtLoad = -1f;
            exit.Init(scene =>
            {
                speedAtLoad = Time.timeScale;
                return RecordLoad(scene);
            }, StageInterface.MenuScene, null);
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                panel.root.Add(exit.overlay);

                ToolkitTestPanel.Submit(exit.button);
            }

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(speedAtLoad, Is.EqualTo(1f), "Time must be normal before the menu loads.");
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        public void Back_NothingArmed_RestoresTimeAndLoadsTheMenu(float sandboxSpeed)
        {
            Scene scene = SandboxLikeScene();
            InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
            StageSandboxExit exit = CreateExit(interaction);
            Time.timeScale = sandboxSpeed;

            Assert.That(exit.Back(), Is.True);

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            CollectionAssert.AreEqual(new[] { StageInterface.MenuScene }, _loads);
        }

        // Escape first stops an armed placement, as the sandbox hint says; only the next press leaves
        [Test]
        public void Back_PlacementArmed_LeavesItToTheSandbox()
        {
            SandboxLikeScene();
            InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
            TestHelpers.SetPrivateField(interaction, "_interaction", new ArmedInteraction());
            StageSandboxExit exit = CreateExit(interaction);
            Time.timeScale = 0f;

            Assert.That(exit.Back(), Is.False);

            CollectionAssert.IsEmpty(_loads);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Staying in the sandbox keeps its pause.");
        }

        [Test]
        public void Leave_LoaderRefuses_StillRestoresTime()
        {
            StageSandboxExit exit = CreateExit();
            exit.Init(scene => false, StageInterface.MenuScene, null);
            Time.timeScale = 0f;

            Assert.That(exit.Leave(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void Leave_WithoutLoader_LoadsNothing()
        {
            StageSandboxExit exit = CreateExit();
            exit.Init(null, StageInterface.MenuScene, null);

            Assert.That(exit.Leave(), Is.False);
        }

        [Test]
        public void ButtonPosition_FullScreen_IsTheMarginFromTheTopLeft()
        {
            Vector2 position = StageSandboxExit.ButtonPosition(1080, 1920, new Rect(0f, 0f, 1080f, 1920f), 1080f / 390f);

            Assert.That(position.x, Is.EqualTo(StageSandboxExit.Margin).Within(0.001f));
            Assert.That(position.y, Is.EqualTo(StageSandboxExit.Margin).Within(0.001f));
        }

        // A notch of 120 pixels at the top and a 30 pixel side inset push the button inside the safe area
        [Test]
        public void ButtonPosition_Notch_StaysInsideTheSafeArea()
        {
            float scale = 1080f / 390f;

            Vector2 position = StageSandboxExit.ButtonPosition(1080, 1920, new Rect(30f, 0f, 1020f, 1800f), scale);

            Assert.That(position.x, Is.EqualTo(30f / scale + StageSandboxExit.Margin).Within(0.001f));
            Assert.That(position.y, Is.EqualTo(120f / scale + StageSandboxExit.Margin).Within(0.001f));
        }

        [Test]
        public void Leave_FromSandbox_KeepsTheTargetForTheMenuOnly()
        {
            StageSandboxExit exit = CreateExit();
            exit.Init(RouteLoad, StageInterface.MenuScene, null);
            StageInterface.Route(StageInterface.SandboxScene);

            Assert.That(exit.Leave(), Is.True);

            CollectionAssert.AreEqual(new[] { StageInterface.MenuScene }, _loads);
            Assert.That(StageInterface.Route(StageInterface.GameplayScene), Is.EqualTo(StageTarget.MainPath));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath), "Start after the sandbox must play Main.");
        }

        // The whole round trip on the menu's own actions: sandbox, Menu, Start opens the class screen, the pick routes
        // to Main, and the Sandbox entry still routes to the sandbox
        [Test]
        public void AfterReturning_StartOpensTheClassScreenAndRoutesMain_AndSandboxWorksAgain()
        {
            StageInterface.Route(StageInterface.SandboxScene);
            StageSandboxExit exit = CreateExit();
            exit.Init(RouteLoad, StageInterface.MenuScene, null);
            Time.timeScale = 0f;
            Assert.That(exit.Leave(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            PlayTheMenu(new[] { StageInterface.MenuScene });
        }

        // The plain boot lands on the menu with nothing selected and nothing loaded yet: Start opens the class screen,
        // the pick routes Main, Sandbox routes the sandbox, and the sandbox's Menu button comes back to the menu
        [Test]
        public void FromAMenuBoot_StartOpensTheClassScreenAndRoutesMain_SandboxAndItsMenuButtonWork()
        {
            GameObject launcherHost = new GameObject("Stage launcher");
            _created.Add(launcherHost);
            Assert.That(launcherHost.AddComponent<StageLauncher>().scenePath, Is.EqualTo(StageInterface.MenuPath),
                "The plain boot must open the Toolkit menu.");

            PlayTheMenu(new string[0]);

            StageSandboxExit exit = CreateExit();
            exit.Init(RouteLoad, StageInterface.MenuScene, null);
            Assert.That(exit.Leave(), Is.True);
            Assert.That(_loads[_loads.Count - 1], Is.EqualTo(StageInterface.MenuScene));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.SandboxPath),
                "The menu leaves the sandbox choice for Start to replace.");
            Assert.That(StageInterface.Route(StageInterface.GameplayScene), Is.EqualTo(StageTarget.MainPath));
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
        }

        // The menu's own actions from wherever the player came: Start opens the class screen without loading, the
        // Druid routes Main, and the Sandbox entry routes the sandbox
        void PlayTheMenu(string[] loadsBefore)
        {
            GameObject owner = new GameObject("Toolkit menu");
            _created.Add(owner);
            owner.SetActive(false);
            ToolkitGameUI host = owner.AddComponent<ToolkitGameUI>();
            host.gameplayScene = StageInterface.GameplayScene;
            host.menuScene = StageInterface.MenuScene;
            host.sandboxScene = StageInterface.SandboxScene;
            host.sceneLoader = RouteLoad;
            GameData data = ScriptableObject.CreateInstance<GameData>();
            _created.Add(data);
            CharacterData druid = ScriptableObject.CreateInstance<CharacterData>();
            druid.name = "DruidCharacter";
            druid.title = "Druid";
            druid.text = "Druid description";
            _created.Add(druid);
            data.characters.Add(druid);
            host.gameData = data;
            PanelSettings settings = ToolkitMobileLayout.CreatePanelSettings(null);
            _created.Add(settings);
            UIDocument document = owner.GetComponent<UIDocument>();
            document.panelSettings = settings;

            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                panel.root.Add(root);
                ToolkitGameView view = new ToolkitGameView(root);
                ToolkitGameContext context = new ToolkitGameContext { isMenu = true };
                ToolkitMobileLayout mobile = new ToolkitMobileLayout();
                ToolkitTimeControls time = new ToolkitTimeControls();
                ToolkitMapPanel map = new ToolkitMapPanel();
                mobile.Init(view, context, document);
                time.Init(null, context, view);
                ToolkitGameActions actions = new ToolkitGameActions(host, context, view, time, mobile, map);
                try
                {
                    ToolkitTestPanel.Submit(root.Q<Button>("start-button"));
                    Assert.That(root.Q(ToolkitClassSelect.PanelName).ClassListContains("is-hidden"), Is.False,
                        "Start after the sandbox must open the class screen.");
                    CollectionAssert.AreEqual(loadsBefore, _loads, "Start must wait for a class before loading.");

                    Button card = null;
                    foreach (Button candidate in root.Q(ToolkitClassSelect.ListName).Query<Button>("data-card").ToList())
                    {
                        if (candidate.userData is ToolkitCardModel model && model.title == "Druid")
                        {
                            card = candidate;
                        }
                    }

                    Assert.That(card, Is.Not.Null);
                    ToolkitTestPanel.Submit(card);
                    CollectionAssert.AreEqual(new List<string>(loadsBefore) { StageInterface.GameplayScene }, _loads);
                    Assert.That(StageTarget.isSelected, Is.True);
                    Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
                    Assert.That(CharacterSelection.selected, Is.SameAs(druid));

                    ToolkitTestPanel.Submit(root.Q<Button>("sandbox-button"));
                    Assert.That(_loads[_loads.Count - 1], Is.EqualTo(StageInterface.SandboxScene));
                    Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.SandboxPath));
                }
                finally
                {
                    actions.Dispose();
                    map.Dispose();
                    time.Dispose();
                    mobile.Dispose();
                    view.Release();
                    document.panelSettings = null;
                }
            }
        }
    }
}
