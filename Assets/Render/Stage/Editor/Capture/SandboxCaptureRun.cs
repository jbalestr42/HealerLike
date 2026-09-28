using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HealerLike.Render.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    // Julien's sandbox under the render stage, played through its own uGUI: entity buttons then a board tap place
    // allies, a wave button brings enemies, Start battle, Pause and Speed are pressed as a player would. Stills of
    // the Game view (with the uGUI overlay) and of the camera alone go to the capture folder, and the time scale
    // each press leaves is logged. It records what happened and does not judge the look.
    // The boot run selects the sandbox before RenderStage starts; the menu run boots Main, goes to the Toolkit menu
    // and presses its Sandbox entry.
    public class SandboxCaptureRun : AStageRun
    {
        public static readonly string BootMode = "sandbox";
        public static readonly string MenuMode = "sandbox-menu";
        public static readonly string[] Units =
        {
            "Assets/Data/Entities/ZealotEntity/ZealotEntity.asset",
            "Assets/Data/Entities/TreantEntity/TreantEntity.asset",
            "Assets/Data/Entities/GroveKeeperEntity/GroveKeeperEntity.asset",
            "Assets/Data/Entities/BloodCultistEntity/BloodCultistEntity.asset",
            "Assets/Data/Entities/NormalEntity/NormalEntity.asset"
        };

        readonly bool _viaMenu;
        readonly List<string> _problems = new List<string>();

        public SandboxCaptureRun(bool viaMenu)
        {
            _viaMenu = viaMenu;
        }

        protected override bool shouldStartGame { get { return false; } }

        // A play session starts with no target (StageTarget resets at SubsystemRegistration), so the boot run
        // selects the sandbox after that reset and before StageLauncher.Start reads it
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SelectBootTarget()
        {
            if (StagePlay.Mode == BootMode)
            {
                StageTarget.Select(StageTarget.SandboxPath);
                Debug.Log("[SandboxCaptureRun] Boot target selected: " + StageTarget.scenePath);
            }
        }

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
                    yield return PlaySandbox();
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
            while (Buttons().Count == 0)
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
            yield return CaptureScreen(prefix + "01-panel");

            yield return PlaceUnits();
            if (!Press("Load a wave"))
            {
                yield break;
            }

            yield return Wait(0.5f);
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

        // Each unit's button arms Julien's placement, a tap on the board places it, as a click would
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

        // The first wave of the list, the wave buttons share their panel with its Cancel button
        void PressFirstWave()
        {
            SandboxButton first = null;
            foreach (KeyValuePair<string, SandboxButton> pair in Buttons())
            {
                if (pair.Key != "Cancel" && IsWaveButton(pair.Value)
                    && (first == null || pair.Value.transform.GetSiblingIndex() < first.transform.GetSiblingIndex()))
                {
                    first = pair.Value;
                }
            }

            if (first == null)
            {
                _problems.Add("No wave button was shown after Load a wave.");
                return;
            }

            Press(Label(first));
        }

        static bool IsWaveButton(SandboxButton button)
        {
            foreach (SandboxButton cancel in Object.FindObjectsByType<SandboxButton>())
            {
                if (Label(cancel) == "Cancel" && cancel.transform.parent == button.transform.parent)
                {
                    return true;
                }
            }

            return false;
        }

        IEnumerator TimeControls(string prefix)
        {
            float started = Time.timeScale;
            Press("Pause");
            float paused = Time.timeScale;
            float gameBefore = Time.time;
            yield return Wait(1f);
            float gameWhilePaused = Time.time - gameBefore;
            yield return CaptureScreen(prefix + "04-paused");
            Press("Resume");
            float resumed = Time.timeScale;
            Press("Speed x1");
            float slow = Time.timeScale;
            float realStart = Time.realtimeSinceStartup;
            gameBefore = Time.time;
            yield return Wait(2f);
            float ratio = (Time.time - gameBefore) / (Time.realtimeSinceStartup - realStart);
            yield return CaptureScreen(prefix + "05-slow");
            Press("Speed x0.5");
            float slower = Time.timeScale;
            Press("Speed x0.25");
            float back = Time.timeScale;
            Debug.Log("[SandboxCaptureRun] timeScale " + string.Format(CultureInfo.InvariantCulture,
                "start {0} pause {1} (game time while paused {2:F3}s over 1s real) resume {3} speed {4} "
                + "(game/real {5:F2}) speed {6} speed {7}",
                started, paused, gameWhilePaused, resumed, slow, ratio, slower, back));
        }

        Dictionary<string, SandboxButton> Buttons()
        {
            Dictionary<string, SandboxButton> buttons = new Dictionary<string, SandboxButton>();
            foreach (SandboxButton button in Object.FindObjectsByType<SandboxButton>())
            {
                string label = Label(button);
                if (label != null && !buttons.ContainsKey(label))
                {
                    buttons[label] = button;
                }
            }

            return buttons;
        }

        static string Label(SandboxButton button)
        {
            TMPro.TMP_Text text = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            return text != null ? text.text : null;
        }

        void LogButtons()
        {
            List<string> labels = new List<string>(Buttons().Keys);
            Debug.Log("[SandboxCaptureRun] Sandbox buttons: " + string.Join(", ", labels));
        }

        // A pointer click on the button's own object, which runs its uGUI click handler and honours interactable
        bool Press(string label)
        {
            if (!Buttons().TryGetValue(label, out SandboxButton button) || !button.gameObject.activeInHierarchy)
            {
                _problems.Add("No visible sandbox button labelled " + label);
                return false;
            }

            PointerEventData pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
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
