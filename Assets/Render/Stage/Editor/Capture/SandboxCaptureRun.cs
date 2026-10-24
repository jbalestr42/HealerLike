using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HealerLike.Render.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // The game's sandbox under the render stage, played through its Toolkit HUD: the sandbox tools' unit buttons then
    // a board tap place allies, a wave button brings enemies, Start battle, Pause and the speeds are pressed as a
    // player would. Stills of the Game view (with the HUD) and of the camera alone go to the capture folder, and the
    // time scale each press leaves is logged. It records what happened and does not judge the look.
    // The boot run boots the sandbox (StagePlay.BootTarget selects it before RenderStage starts); the menu run boots Main, goes to the Toolkit menu
    // and presses its Sandbox entry. The back run boots the sandbox, pauses it, and leaves through the pause panel's
    // Return to menu, then presses Start there.
    public class SandboxCaptureRun : AStageRun
    {
        public static readonly string BootMode = "sandbox";
        public static readonly string MenuMode = "sandbox-menu";
        public static readonly string BackMode = "sandbox-back";
        public static readonly string[] Units =
        {
            "Assets/Data/Entities/ZealotEntity/ZealotEntity.asset",
            "Assets/Data/Entities/TreantEntity/TreantEntity.asset",
            "Assets/Data/Entities/GroveKeeperEntity/GroveKeeperEntity.asset",
            "Assets/Data/Entities/BloodCultistEntity/BloodCultistEntity.asset",
            "Assets/Data/Entities/NormalEntity/NormalEntity.asset"
        };

        readonly bool _viaMenu;
        readonly bool _back;
        readonly List<string> _problems = new List<string>();

        public SandboxCaptureRun(bool viaMenu, bool back = false)
        {
            _viaMenu = viaMenu;
            _back = back;
        }

        // The modes whose session boots straight into the sandbox
        public static bool BootsSandbox(string mode)
        {
            return mode == BootMode || mode == BackMode;
        }

        protected override bool shouldStartGame { get { return false; } }

        protected override IEnumerator Run()
        {
            Debug.Log($"[SandboxCaptureRun] Booted {ActiveGamePath()} target {StageTarget.scenePath} "
                + $"selected {StageTarget.isSelected}");
            using (new StageGameViewSize(StageCalibration.PortraitWidth, StageCalibration.PortraitHeight))
            {
                _manager.SetLandscape(false);
                if (_viaMenu)
                {
                    yield return ThroughMenu();
                }

                if (_problems.Count == 0)
                {
                    yield return _back ? BackToMenu() : PlaySandbox();
                }
            }

            foreach (string problem in _problems)
            {
                Debug.LogError("[SandboxCaptureRun] " + problem);
            }

            Debug.Log($"[SandboxCaptureRun] {(_problems.Count == 0 ? "DONE" : "PROBLEMS " + _problems.Count)}");
            StagePlay.Finish(this, _problems.Count == 0);
        }

        string ActiveGamePath()
        {
            return _manager.entityManager != null ? _manager.entityManager.gameObject.scene.path : "none";
        }

        // Main's Toolkit HUD takes the player to the menu through the stage's loader, the menu's Sandbox entry
        // takes it to the sandbox
        IEnumerator ThroughMenu()
        {
            ToolkitGameUI hud = Object.FindAnyObjectByType<ToolkitGameUI>();
            if (hud == null || hud.sceneLoader == null)
            {
                _problems.Add("Main has no Toolkit HUD with a scene loader.");
                yield break;
            }

            hud.sceneLoader(hud.menuScene);
            ToolkitGameUI menu = null;
            float deadline = Time.realtimeSinceStartup + 30f;
            while (menu == null || menu.gameObject.scene.path != StageInterface.MenuPath)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("The Toolkit menu never loaded.");
                    yield break;
                }

                menu = Object.FindAnyObjectByType<ToolkitGameUI>();
                yield return null;
            }

            yield return Wait(2f);
            yield return CaptureScreen("sandbox-menu-entry");
            Debug.Log($"[SandboxCaptureRun] Menu target before the press {StageTarget.scenePath}");
            StageInterfaceActions actions = new StageInterfaceActions { ui = menu };
            actions.Submit("sandbox-button");
            deadline = Time.realtimeSinceStartup + 30f;
            while (ActiveGamePath() != StageTarget.SandboxPath)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("The Sandbox entry never reached the sandbox scene.");
                    yield break;
                }

                yield return null;
            }

            Debug.Log($"[SandboxCaptureRun] Menu route reached {ActiveGamePath()} target {StageTarget.scenePath}");
        }

        IEnumerator PlaySandbox()
        {
            if (ActiveGamePath() != StageTarget.SandboxPath)
            {
                _problems.Add("The stage attached to " + ActiveGamePath() + ", not the sandbox.");
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 30f;
            while (!IsSandboxPanelBuilt())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("The sandbox panel never created its buttons.");
                    yield break;
                }

                yield return null;
            }

            yield return Wait(2f);
            string prefix = _viaMenu ? "sandbox-menu-" : "sandbox-";
            LogButtons();
            Actions().Submit("sandbox-button");
            yield return Wait(0.5f);
            yield return CaptureScreen(prefix + "01-panel");

            yield return PlaceUnits();
            PressFirstWave();
            yield return Wait(2f);
            LogViews();
            Render(prefix + "02-views-camera");
            yield return CaptureScreen(prefix + "02-views");

            if (_viaMenu)
            {
                yield break;
            }

            Press("Start battle");
            yield return Wait(3f);
            yield return CaptureScreen(prefix + "03-battle");
            yield return TimeControls(prefix);
        }

        // The Toolkit HUD's pause, then its Return to menu: the Toolkit menu must come up at normal time, and its Start
        // must still open the class screen
        IEnumerator BackToMenu()
        {
            if (ActiveGamePath() != StageTarget.SandboxPath)
            {
                _problems.Add("The stage attached to " + ActiveGamePath() + ", not the sandbox.");
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 30f;
            while (!IsSandboxPanelBuilt())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("The sandbox never showed its Toolkit HUD with its tools.");
                    yield break;
                }

                yield return null;
            }

            yield return Wait(2f);
            LogTimeScale("sandbox shown");
            yield return CaptureScreen("sandbox-back-01-hud");

            Actions().Submit("pause-button");
            yield return Wait(1f);
            LogTimeScale("after Pause");
            yield return CaptureScreen("sandbox-back-02-paused");

            Actions().Submit("menu-button");
            LogTimeScale("after Menu");
            ToolkitGameUI menu = null;
            deadline = Time.realtimeSinceStartup + 30f;
            while (menu == null || menu.gameObject.scene.path != StageInterface.MenuPath)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("Return to menu never reached the Toolkit menu.");
                    yield break;
                }

                menu = Object.FindAnyObjectByType<ToolkitGameUI>();
                yield return null;
            }

            yield return Wait(2f);
            LogTimeScale("menu shown");
            Debug.Log($"[SandboxCaptureRun] Menu reached {menu.gameObject.scene.path}, target {StageTarget.scenePath}");
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                _problems.Add("The menu came up at timeScale " + Time.timeScale);
            }

            yield return CaptureScreen("sandbox-back-03-menu");

            StageInterfaceActions actions = new StageInterfaceActions { ui = menu };
            actions.Submit("start-button");
            yield return Wait(1f);
            VisualElement classPanel = actions.root.Q(ToolkitClassSelect.PanelName);
            bool classShown = classPanel != null && StageInterfaceOutput.IsVisible(classPanel);
            Debug.Log($"[SandboxCaptureRun] Start after the sandbox: class screen shown {classShown}");
            if (!classShown)
            {
                _problems.Add("Start after returning from the sandbox did not open the class screen.");
            }

            yield return CaptureScreen("sandbox-back-04-class-screen");
        }

        static void LogTimeScale(string step)
        {
            Debug.Log($"[SandboxCaptureRun] timeScale {step}: {Time.timeScale.ToString(CultureInfo.InvariantCulture)}");
        }

        // Each unit's button arms the sandbox placement, a tap on the board places it, as a click would
        IEnumerator PlaceUnits()
        {
            Bounds board = _manager.board;
            for (int i = 0; i < Units.Length; i++)
            {
                EntityData data = RenderAssets.Load<EntityData>(Units[i]);
                if (data == null || !Press(data.title))
                {
                    continue;
                }

                yield return Wait(0.3f);
                Vector3 point = new Vector3(Mathf.Lerp(board.min.x, board.max.x, 0.15f + 0.175f * i), board.max.y,
                    Mathf.Lerp(board.min.z, board.max.z, 0.3f));
                TapBoard(point, data.title);
                yield return Wait(0.3f);
            }

            InteractionManager.instance.CancelInteraction();
        }

        void TapBoard(Vector3 point, string title)
        {
            AInteraction interaction = InteractionManager.instance.GetInteraction();
            Camera camera = _manager.gameCamera;
            Ray ray = new Ray(camera.transform.position, point - camera.transform.position);
            if (interaction == null
                || !Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interaction.GetLayerMask()))
            {
                _problems.Add("No placement or no board under the tap for " + title);
                return;
            }

            int before = _manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            interaction.OnMouseClick(hit);
            int after = _manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            Debug.Log($"[SandboxCaptureRun] Placed {title}: allies {before} -> {after}");
        }

        // The first wave of the sandbox tools
        void PressFirstWave()
        {
            Button first = Actions().root.Q(ToolkitSandboxPanel.WavesName).Q<Button>();
            if (first == null)
            {
                _problems.Add("The sandbox tools list no wave.");
                return;
            }

            Press(first.text);
        }

        // Pause, then the slow speed of the pause panel and back to normal, the time scale each press leaves logged
        IEnumerator TimeControls(string prefix)
        {
            StageInterfaceActions actions = Actions();
            float started = Time.timeScale;
            actions.Submit("pause-button");
            float paused = Time.timeScale;
            float gameBefore = Time.time;
            yield return Wait(1f);
            float gameWhilePaused = Time.time - gameBefore;
            yield return CaptureScreen(prefix + "04-paused");
            actions.Submit("speed-slow-button");
            actions.Submit("resume-button");
            float slow = Time.timeScale;
            float realStart = Time.realtimeSinceStartup;
            gameBefore = Time.time;
            yield return Wait(2f);
            float ratio = (Time.time - gameBefore) / (Time.realtimeSinceStartup - realStart);
            yield return CaptureScreen(prefix + "05-slow");
            actions.Submit("pause-button");
            actions.Submit("speed-normal-button");
            actions.Submit("resume-button");
            float back = Time.timeScale;
            Debug.Log("[SandboxCaptureRun] timeScale " + string.Format(CultureInfo.InvariantCulture,
                "start {0} pause {1} (game time while paused {2:F3}s over 1s real) slow {3} (game/real {4:F2}) back {5}",
                started, paused, gameWhilePaused, slow, ratio, back));
        }

        static StageInterfaceActions Actions()
        {
            return new StageInterfaceActions { ui = Object.FindAnyObjectByType<ToolkitGameUI>() };
        }

        // Its tools are listed once the sandbox has set its character up
        static bool IsSandboxPanelBuilt()
        {
            if (Object.FindAnyObjectByType<ToolkitGameUI>() == null)
            {
                return false;
            }

            VisualElement units = Actions().root.Q(ToolkitSandboxPanel.UnitsName);
            return units != null && units.childCount > 0;
        }

        // The Toolkit buttons by label: the toolbar's and the sandbox tools'
        static Dictionary<string, Button> Buttons()
        {
            Dictionary<string, Button> buttons = new Dictionary<string, Button>();
            Actions().root.Query<Button>().ForEach(button =>
            {
                if (!string.IsNullOrEmpty(button.text) && !buttons.ContainsKey(button.text))
                {
                    buttons[button.text] = button;
                }
            });
            return buttons;
        }

        void LogButtons()
        {
            List<string> labels = new List<string>(Buttons().Keys);
            Debug.Log("[SandboxCaptureRun] Toolkit buttons: " + string.Join(", ", labels));
        }

        // A submit on the Toolkit button, as a tap would; a sandbox tool is reached by opening the tools first
        bool Press(string label)
        {
            if (!Buttons().TryGetValue(label, out Button button))
            {
                _problems.Add("No Toolkit button labelled " + label);
                return false;
            }

            StageInterfaceActions actions = Actions();
            VisualElement tools = actions.root.Q(ToolkitSandboxPanel.PanelName);
            if (tools != null && tools.Contains(button) && !StageInterfaceOutput.IsVisible(tools))
            {
                actions.Submit("sandbox-button");
            }

            actions.Submit(button);
            Debug.Log($"[SandboxCaptureRun] Pressed {label}, timeScale {Time.timeScale.ToString(CultureInfo.InvariantCulture)}");
            return true;
        }

        void LogViews()
        {
            int entities = 0;
            int dressed = 0;
            foreach (Entity.EntityType side in new[] { Entity.EntityType.Player, Entity.EntityType.Computer })
            {
                foreach (GameObject entityGo in _manager.entityManager.GetEntities(side))
                {
                    if (entityGo == null)
                    {
                        continue;
                    }

                    entities++;
                    CreatureBuilder builder = entityGo.GetComponentInChildren<CreatureBuilder>();
                    dressed += builder != null ? 1 : 0;
                    Entity entity = entityGo.GetComponent<Entity>();
                    Debug.Log($"[SandboxCaptureRun] {side} {entityGo.name} "
                        + $"({(entity != null && entity.data != null ? entity.data.name : "?")}) "
                        + $"creature view {(builder != null ? builder.name : "none")}");
                }
            }

            Debug.Log($"[SandboxCaptureRun] Creature views {dressed} of {entities} entities");
            if (entities == 0 || dressed != entities)
            {
                _problems.Add($"Creature views on {dressed} of {entities} sandbox entities.");
            }
        }

        // The Game view as the player sees it, uGUI overlay included
        IEnumerator CaptureScreen(string name)
        {
            string path = StagePlay.CaptureFolder + name + ".png";
            Directory.CreateDirectory(StagePlay.CaptureFolder);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("The Game view screenshot never arrived: " + path);
                    yield break;
                }

                yield return null;
            }

            yield return Wait(0.2f);
            Debug.Log("[SandboxCaptureRun] Saved " + path);
        }

        // The game camera alone, without the overlay, for the creature views
        void Render(string name)
        {
            string path = StagePlay.CaptureFolder + name + ".png";
            Texture2D texture = StageReadback.Render(_manager.gameCamera, StageCalibration.PortraitWidth,
                StageCalibration.PortraitHeight);
            try
            {
                Directory.CreateDirectory(StagePlay.CaptureFolder);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log("[SandboxCaptureRun] Saved " + path);
            }
            finally
            {
                RenderObjects.Release(texture);
            }
        }
    }
}
