using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // The menu's class choice under the render stage, at the portrait phone size: Main boots, its HUD takes the
    // player to the Toolkit menu (or, in the boot mode, the plain boot lands on the menu and it is captured), Start opens the class screen (captured, then scrolled to its end), the Druid card
    // is pressed, and the expedition that follows is started and its first room entered so the HUD shows the played
    // class's spells. It logs the pick, the character PlayerBehaviour plays and the spell cards, and does not judge
    // the look.
    public class ClassSelectCaptureRun : AStageRun
    {
        public static readonly string Mode = "class-select";
        // The plain boot: nothing is selected, the stage opens the Toolkit menu first
        public static readonly string BootMode = "menu-boot";
        public static readonly string DefaultPick = "Druid";
        public static readonly string PickVariable = "RENDER_CLASS_PICK";

        // The class card title pressed, RENDER_CLASS_PICK when set (e.g. Cleric), else the Druid
        public static string Pick
        {
            get { return PickFrom(System.Environment.GetEnvironmentVariable(PickVariable)); }
        }

        public static string PickFrom(string variable)
        {
            return string.IsNullOrWhiteSpace(variable) ? DefaultPick : variable.Trim();
        }

        readonly List<string> _problems = new List<string>();
        readonly bool _fromBoot;

        public ClassSelectCaptureRun(bool fromBoot = false)
        {
            _fromBoot = fromBoot;
        }

        protected override bool shouldStartGame { get { return false; } }

        protected override bool bootsIntoGame { get { return !_fromBoot; } }

        protected override IEnumerator Run()
        {
            using (new StageGameViewSize(StageCalibration.PortraitWidth, StageCalibration.PortraitHeight))
            {
                _manager.SetLandscape(false);
                yield return ToMenu();
                if (_problems.Count == 0)
                {
                    yield return ChooseClass();
                }

                if (_problems.Count == 0)
                {
                    yield return PlayPick();
                }
            }

            foreach (string problem in _problems)
            {
                Debug.LogError("[ClassSelectCaptureRun] " + problem);
            }

            Debug.Log($"[ClassSelectCaptureRun] {(_problems.Count == 0 ? "DONE" : "PROBLEMS " + _problems.Count)}");
            StagePlay.Finish(this, _problems.Count == 0);
        }

        string ActiveGamePath()
        {
            return _manager.entityManager != null ? _manager.entityManager.gameObject.scene.path : "none";
        }

        static ToolkitGameUI HostIn(string scenePath)
        {
            foreach (ToolkitGameUI ui in Object.FindObjectsByType<ToolkitGameUI>())
            {
                if (ui.gameObject.scene.path == scenePath && ui.isActiveAndEnabled)
                {
                    return ui;
                }
            }

            return null;
        }

        IEnumerator WaitForHost(string scenePath, System.Action<ToolkitGameUI> found)
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            ToolkitGameUI host = null;
            while (host == null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("No Toolkit host came up in " + scenePath);
                    yield break;
                }

                host = HostIn(scenePath);
                yield return null;
            }

            found(host);
        }

        // What the stage looks like around the menu: one manager, the legacy menu silenced, the scenes loaded
        void LogStage(string moment)
        {
            List<string> scenes = new List<string>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                scenes.Add(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path);
            }

            List<string> legacy = new List<string>();
            foreach (MainMenu menu in Object.FindObjectsByType<MainMenu>(FindObjectsInactive.Include))
            {
                legacy.Add(menu.name + (menu.enabled ? " enabled" : " disabled"));
            }

            int managers = Object.FindObjectsByType<RenderManager>(FindObjectsInactive.Include).Length;
            Debug.Log($"[ClassSelectCaptureRun] {moment}: RenderManagers {managers}, scenes [{string.Join(", ", scenes)}], "
                + $"legacy MainMenu [{string.Join(", ", legacy)}], game {ActiveGamePath()}, "
                + $"stage target {StageTarget.scenePath} selected {StageTarget.isSelected}");
            if (managers != 1)
            {
                _problems.Add(moment + ": " + managers + " RenderManagers.");
            }
        }

        IEnumerator ToMenu()
        {
            if (!_fromBoot)
            {
                ToolkitGameUI hud = Object.FindAnyObjectByType<ToolkitGameUI>();
                if (hud == null || hud.sceneLoader == null)
                {
                    _problems.Add("Main has no Toolkit HUD with a scene loader.");
                    yield break;
                }

                hud.sceneLoader(hud.menuScene);
            }

            ToolkitGameUI menu = null;
            yield return WaitForHost(StageInterface.MenuPath, host => menu = host);
            if (menu == null)
            {
                yield break;
            }

            yield return Wait(2f);
            if (_fromBoot)
            {
                LogStage("Boot menu");
                if (menu.sceneLoader == null)
                {
                    _problems.Add("The booted menu has no stage scene loader.");
                }

                foreach (MainMenu legacy in Object.FindObjectsByType<MainMenu>())
                {
                    if (legacy.enabled)
                    {
                        _problems.Add("The legacy MainMenu is still enabled on " + legacy.name + ".");
                    }
                }

                yield return CaptureScreen("boot-01-menu");
            }

            Debug.Log("[ClassSelectCaptureRun] Menu game data "
                + (menu.gameData != null ? menu.gameData.name : "none") + ", offers "
                + string.Join(", ", ToolkitClassSelect.Offered(menu.gameData).ConvertAll(c => c.title)));
            StageInterfaceActions actions = new StageInterfaceActions { ui = menu };
            actions.Submit("start-button");
            yield return Wait(1f);
            VisualElement panel = actions.root.Q(ToolkitClassSelect.PanelName);
            if (panel == null || !StageInterfaceOutput.IsVisible(panel))
            {
                _problems.Add("Start did not open the class screen.");
                yield break;
            }

            if (ActiveGamePath() != "none" && ActiveGamePath() != StageInterface.MenuPath)
            {
                _problems.Add("Start left the menu before a class was picked: " + ActiveGamePath());
            }

            Debug.Log("[ClassSelectCaptureRun] Class screen open, selection before the pick "
                + (CharacterSelection.selected != null ? CharacterSelection.selected.title : "null")
                + ", cards " + string.Join(", ", Titles(actions.root.Q(ToolkitClassSelect.ListName))));
            yield return CaptureScreen("class-01-screen");
            List<Button> cards = actions.root.Q(ToolkitClassSelect.ListName).Query<Button>("data-card").ToList();
            if (cards.Count > 0)
            {
                yield return actions.BringIntoView(cards[cards.Count - 1]);
                yield return Wait(0.3f);
                yield return CaptureScreen("class-02-screen-end");
            }
        }

        static List<string> Titles(VisualElement list)
        {
            List<string> titles = new List<string>();
            if (list == null)
            {
                return titles;
            }

            foreach (Button card in list.Query<Button>("data-card").ToList())
            {
                ToolkitCardModel model = card.userData as ToolkitCardModel;
                titles.Add(model != null ? model.title : "?");
            }

            return titles;
        }

        IEnumerator ChooseClass()
        {
            ToolkitGameUI menu = HostIn(StageInterface.MenuPath);
            StageInterfaceActions actions = new StageInterfaceActions { ui = menu };
            Button pick = null;
            foreach (Button card in actions.root.Q(ToolkitClassSelect.ListName).Query<Button>("data-card").ToList())
            {
                if (card.userData is ToolkitCardModel model && model.title == Pick)
                {
                    pick = card;
                }
            }

            if (pick == null)
            {
                _problems.Add("The class screen has no " + Pick + " card.");
                yield break;
            }

            yield return actions.BringIntoView(pick);
            yield return Wait(0.3f);
            actions.Submit(pick);
            Debug.Log("[ClassSelectCaptureRun] Pressed " + Pick + ", CharacterSelection.selected "
                + (CharacterSelection.selected != null ? CharacterSelection.selected.name : "null")
                + ", stage target " + StageTarget.scenePath + " selected " + StageTarget.isSelected);
            float deadline = Time.realtimeSinceStartup + 30f;
            while (ActiveGamePath() != StageInterface.GameplayPath)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("The pick never reached Main.");
                    yield break;
                }

                yield return null;
            }

            Debug.Log("[ClassSelectCaptureRun] Main loaded after the pick: " + ActiveGamePath());
            LogStage("After the pick");
        }

        IEnumerator PlayPick()
        {
            ToolkitGameUI hud = null;
            yield return WaitForHost(StageInterface.GameplayPath, host => hud = host);
            if (hud == null)
            {
                yield break;
            }

            yield return Wait(1.5f);
            StageInterfaceActions actions = new StageInterfaceActions { ui = hud };
            actions.Submit("start-button");
            yield return StageMapActions.SelectFirst(actions, false);
            yield return Wait(2f);
            PlayerBehaviour player = Object.FindAnyObjectByType<PlayerBehaviour>();
            CharacterData played = player != null && player.character != null ? player.character.data : null;
            Debug.Log("[ClassSelectCaptureRun] CharacterSelection.selected "
                + (CharacterSelection.selected != null ? CharacterSelection.selected.name : "null")
                + "; PlayerBehaviour plays " + (played != null ? played.name + " (" + played.title + ")" : "nothing"));
            if (played == null || played != CharacterSelection.selected || played.title != Pick)
            {
                _problems.Add("PlayerBehaviour does not play the picked " + Pick + ".");
            }

            if (played != null)
            {
                Debug.Log("[ClassSelectCaptureRun] " + played.title + " skills in data: "
                    + CharacterCardText.GetSkills(played).Replace("\n", " "));
                Debug.Log("[ClassSelectCaptureRun] " + played.title + " units in data: "
                    + CharacterCardText.GetUnits(played));
            }

            Debug.Log("[ClassSelectCaptureRun] HUD spell cards: "
                + string.Join(", ", Titles(actions.root.Q("spell-list"))));
            Debug.Log("[ClassSelectCaptureRun] HUD party cards: "
                + string.Join(", ", Titles(actions.root.Q("party-list"))));
            yield return CaptureScreen("class-03-" + Pick.ToLowerInvariant() + "-run");
        }

        // The Game view as the player sees it, the Toolkit overlay included
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
            Debug.Log("[ClassSelectCaptureRun] Saved " + path);
        }
    }
}
