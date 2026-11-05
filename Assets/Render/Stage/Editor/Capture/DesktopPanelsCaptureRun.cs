using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // Main's panels on a Full HD PC screen, to judge their size: the map, the preparation HUD, a unit's and a spell's
    // details with every section open, the inventory, the character sheet, then a won fight's reward. Each panel is
    // logged with its size and whether its content overflows (needs a scroll), and does not judge the look.
    public class DesktopPanelsCaptureRun : AStageRun
    {
        public static readonly string Mode = "desktop-panels";
        public static readonly int Width = 1920;
        public static readonly int Height = 1080;

        readonly List<string> _problems = new List<string>();

        protected override bool shouldStartGame { get { return false; } }

        protected override IEnumerator Run()
        {
            using (new StageGameViewSize(Width, Height))
            {
                _manager.SetLandscape(true);
                yield return Panels();
            }

            foreach (string problem in _problems)
            {
                Debug.LogError("[DesktopPanelsCaptureRun] " + problem);
            }

            Debug.Log($"[DesktopPanelsCaptureRun] {(_problems.Count == 0 ? "DONE" : "PROBLEMS " + _problems.Count)}");
            StagePlay.Finish(this, _problems.Count == 0);
        }

        IEnumerator Panels()
        {
            ToolkitGameUI hud = null;
            float deadline = Time.realtimeSinceStartup + 30f;
            while (hud == null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("Main has no Toolkit HUD.");
                    yield break;
                }

                hud = Object.FindAnyObjectByType<ToolkitGameUI>();
                yield return null;
            }

            yield return Wait(2f);
            StageInterfaceActions actions = new StageInterfaceActions { ui = hud };
            yield return StageMapActions.WaitForSelection(actions);
            yield return Wait(0.5f);
            yield return Capture(actions, "01-map", "map-panel", "map-scroll");
            yield return StageMapActions.SelectFirst(actions, false, true);
            yield return Wait(2f);
            yield return Capture(actions, "02-hud", "party-list", "spell-list");

            List<Button> party = actions.Cards("party-list");
            if (party.Count > 0)
            {
                yield return actions.InspectCard(party[0]);
                yield return OpenSections(actions);
                yield return Capture(actions, "03-detail-unit", "detail-panel", "detail-scroll");
            }

            List<Button> spells = actions.Cards("spell-list");
            if (spells.Count > 0)
            {
                yield return actions.InspectCard(spells[0]);
                yield return OpenSections(actions);
                yield return Capture(actions, "04-detail-spell", "detail-panel", "detail-scroll");
            }

            Close(actions, "detail-close-button");
            actions.Submit("inventory-button");
            yield return Wait(0.5f);
            yield return Capture(actions, "05-inventory", "inventory-body", "inventory-list");
            Close(actions, "inventory-close-button");

            if (StageInterfaceOutput.IsVisible(actions.root.Q("sheet-button")))
            {
                actions.Submit("sheet-button");
                yield return Wait(0.5f);
                yield return Capture(actions, "06-sheet", "sheet-body", "sheet-text");
                Close(actions, "sheet-close-button");
            }

            yield return Reward(actions, hud);
        }

        // The class's units on the board, the battle started then skipped with the editor's Kill wave
        IEnumerator Reward(StageInterfaceActions actions, ToolkitGameUI hud)
        {
            PlayerBehaviour player = Object.FindAnyObjectByType<PlayerBehaviour>();
            CharacterData played = player != null && player.character != null ? player.character.data : null;
            if (played == null || _manager.entityManager == null)
            {
                _problems.Add("No played class to take into battle.");
                yield break;
            }

            Vector3[] offsets = { new Vector3(-2f, 0f, -2f), new Vector3(0f, 0f, -3f), new Vector3(2f, 0f, -2f) };
            Entity placed = null;
            for (int i = 0; i < Mathf.Min(offsets.Length, played.entities.Count); i++)
            {
                GameObject spawned = _manager.entityManager.SpawnEntity(played.entities[i],
                    StageCells.Nearest(_manager, offsets[i]), Entity.EntityType.Player);
                if (placed == null && spawned != null)
                {
                    placed = spawned.GetComponent<Entity>();
                }
            }

            yield return Wait(0.5f);
            if (placed != null)
            {
                hud.InspectEntity(placed, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                yield return OpenSections(actions);
                yield return Capture(actions, "06b-detail-placed", "detail-panel", "detail-scroll");
                Close(actions, "detail-close-button");
                yield return Wait(0.3f);
            }

            actions.Submit("wave-button");
            yield return Wait(2f);
            yield return Capture(actions, "07-battle", "party-list", "spell-list");
            actions.Submit(ToolkitEditorTools.KillWaveName);
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!StageInterfaceOutput.IsVisible(actions.root.Q("upgrade-panel")))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    _problems.Add("Kill wave never opened the reward screen.");
                    yield break;
                }

                yield return null;
            }

            yield return Wait(0.8f);
            yield return Capture(actions, "08-reward", "upgrade-panel", "upgrade-list");
            yield return RewardCounts(actions, hud);
        }

        // The reward screen redrawn with 2, 4 and 6 choices, cycling the rolled ones, to judge its width
        IEnumerator RewardCounts(StageInterfaceActions actions, ToolkitGameUI hud)
        {
            FieldInfo field = typeof(AscensionGameType).GetField("_rewardChoices",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AscensionGameType ascension = Object.FindAnyObjectByType<AscensionGameType>();
            List<RewardChoice> rolled = ascension != null ? field?.GetValue(ascension) as List<RewardChoice> : null;
            if (rolled == null || rolled.Count == 0)
            {
                _problems.Add("No reward choices to redraw.");
                yield break;
            }

            List<RewardChoice> original = new List<RewardChoice>(rolled);
            foreach (int count in new[] { 2, 4, 6 })
            {
                rolled.Clear();
                for (int i = 0; i < count; i++)
                {
                    rolled.Add(original[i % original.Count]);
                }

                hud.Refresh();
                yield return Wait(0.5f);
                yield return Capture(actions, $"08-reward-{count}", "upgrade-panel", "upgrade-list");
            }

            rolled.Clear();
            rolled.AddRange(original);
        }

        static IEnumerator OpenSections(StageInterfaceActions actions)
        {
            yield return Wait(0.3f);
            foreach (Foldout foldout in actions.root.Q("detail-panel").Query<Foldout>().ToList())
            {
                foldout.value = true;
            }

            yield return Wait(0.3f);
        }

        static void Close(StageInterfaceActions actions, string name)
        {
            Button button = actions.root.Q<Button>(name);
            if (button != null && StageInterfaceOutput.IsVisible(button))
            {
                actions.Submit(button);
            }
        }

        // The Game view as the player sees it, with the panel's size and its scroll's overflow
        IEnumerator Capture(StageInterfaceActions actions, string name, string panelName, string scrollName)
        {
            Log(actions.root, name, panelName);
            Log(actions.root, name, scrollName);
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
            Debug.Log("[DesktopPanelsCaptureRun] Saved " + path);
        }

        static void Log(VisualElement root, string moment, string elementName)
        {
            VisualElement element = root.Q(elementName);
            if (element == null)
            {
                Debug.Log($"[DesktopPanelsCaptureRun] {moment} {elementName}: none");
                return;
            }

            string overflow = "";
            if (element is ScrollView scroll)
            {
                Rect content = scroll.contentContainer.layout;
                Rect viewport = scroll.contentViewport.layout;
                overflow = $", content {content.width:0}x{content.height:0} in {viewport.width:0}x{viewport.height:0}"
                    + (content.height > viewport.height + 1f || content.width > viewport.width + 1f ? " OVERFLOWS" : "");
            }

            Debug.Log($"[DesktopPanelsCaptureRun] {moment} {elementName}: shown {StageInterfaceOutput.IsVisible(element)}, "
                + $"{element.layout.width:0}x{element.layout.height:0}{overflow}");
        }
    }
}
