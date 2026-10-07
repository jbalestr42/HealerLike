using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using HealerLike.Render.Creatures;

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

            LogLeftovers(moment);
        }

        // Where the start screen's parts landed; the actions must sit whole inside the panel
        void LogMenuLayout(ToolkitGameUI menu)
        {
            UIDocument document = menu.GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;
            if (root == null)
            {
                _problems.Add("The menu has no document root.");
                return;
            }

            Rect screen = root.worldBound;
            List<string> parts = new List<string>();
            foreach (string name in new[] { "hud-root", "menu-panel", "menu-title", "menu-tagline", "field-toolbar",
                         "start-button", "sandbox-button" })
            {
                VisualElement element = root.Q(name);
                if (element == null)
                {
                    _problems.Add("The menu has no " + name + ".");
                    continue;
                }

                Rect bound = element.worldBound;
                parts.Add($"{name} {bound.xMin:0},{bound.yMin:0} {bound.width:0}x{bound.height:0}");
                bool isAction = name == "start-button" || name == "sandbox-button";
                if (isAction && (bound.width < 1f || bound.xMin < screen.xMin - 0.5f || bound.xMax > screen.xMax + 0.5f
                                 || bound.yMin < screen.yMin - 0.5f || bound.yMax > screen.yMax + 0.5f))
                {
                    _problems.Add($"The menu's {name} is not whole on screen: {bound} in {screen}.");
                }
            }

            Debug.Log($"[ClassSelectCaptureRun] Menu layout in {screen.width:0}x{screen.height:0}: {string.Join("; ", parts)}");
        }

        // The menu backdrop lives in the menu scene only: after the pick nothing of it may remain, and neither
        // scene may add a second live camera, audio listener or event system
        void LogLeftovers(string moment)
        {
            int cameras = 0;
            foreach (Camera camera in Object.FindObjectsByType<Camera>())
            {
                cameras += camera.isActiveAndEnabled ? 1 : 0;
            }

            int listeners = 0;
            foreach (AudioListener listener in Object.FindObjectsByType<AudioListener>())
            {
                listeners += listener.isActiveAndEnabled ? 1 : 0;
            }

            int eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length;
            int backdrops = 0;
            foreach (Transform root in Object.FindObjectsByType<Transform>())
            {
                backdrops += root.name == StageBackdrop.HostName ? 1 : 0;
            }

            bool expectsBackdrop = _manager.entityManager == null;
            Debug.Log($"[ClassSelectCaptureRun] {moment}: live cameras {cameras}, audio listeners {listeners}, "
                + $"event systems {eventSystems}, backdrop hosts {backdrops}, backdrop attached "
                + $"{_manager.backdrop.isAttached} with {_manager.backdrop.creatureCount} creatures, fog "
                + $"{Shader.GetGlobalFloat("_HLFogStart"):0.#}..{Shader.GetGlobalFloat("_HLFogEnd"):0.#}, camera "
                + $"{(_manager.gameCamera != null ? _manager.gameCamera.transform.position.ToString() : "none")}");
            if (cameras != 1 || listeners > 1 || eventSystems > 1)
            {
                _problems.Add($"{moment}: {cameras} live cameras, {listeners} audio listeners, {eventSystems} event systems.");
            }

            if (backdrops != (expectsBackdrop ? 1 : 0) || _manager.backdrop.isAttached != expectsBackdrop)
            {
                _problems.Add($"{moment}: {backdrops} backdrop hosts, attached {_manager.backdrop.isAttached}.");
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

                LogMenuLayout(menu);
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
            yield return KitPopover(actions);
            List<Button> cards = actions.root.Q(ToolkitClassSelect.ListName).Query<Button>("data-card").ToList();
            if (cards.Count > 0)
            {
                yield return actions.BringIntoView(cards[cards.Count - 1]);
                yield return Wait(0.3f);
                yield return CaptureScreen("class-02-screen-end");
            }
        }

        // A touch on the first card's first kit chip opens its details in the popover, over the class screen
        IEnumerator KitPopover(StageInterfaceActions actions)
        {
            VisualElement list = actions.root.Q(ToolkitClassSelect.ListName);
            foreach (Button card in list.Query<Button>("data-card").ToList())
            {
                ToolkitCardModel model = card.userData as ToolkitCardModel;
                Debug.Log("[ClassSelectCaptureRun] Card " + (model != null ? model.title : "?") + ": "
                    + card.Query(className: ToolkitKitRow.ChipClass).ToList().Count + " kit chips, stats '"
                    + (card.Q<Label>("card-stats")?.text ?? "").Replace("\n", " | ") + "', details '"
                    + (card.Q<Label>("card-details")?.text ?? "").Replace("\n", " | ") + "'");
            }

            VisualElement chip = list.Q(className: ToolkitKitRow.ChipClass);
            if (chip == null)
            {
                _problems.Add("The class cards show no kit chip.");
                yield break;
            }

            // A finger through the legacy input module, as the menu's own card pick would be by a player
            VisualElement picked = chip.panel.Pick(chip.worldBound.center);
            Debug.Log("[ClassSelectCaptureRun] Picked at the chip centre: " + (picked != null ? picked.name + " ." + string.Join(" .", picked.GetClasses()) : "nothing")
                + ", inside the chip " + (picked == chip || chip.Contains(picked)));
            actions.ConfigureLegacyInput();
            yield return actions.TouchGesture(StageInterfaceActions.ScreenPoint(chip));
            actions.Dispose();
            yield return Wait(0.3f);
            VisualElement popover = actions.root.Q("detail-panel");
            Debug.Log("[ClassSelectCaptureRun] After the legacy touch, popover shown " + StageInterfaceOutput.IsVisible(popover));
            if (!StageInterfaceOutput.IsVisible(popover))
            {
                // The capture's legacy touch does not reach the chip's own pointer handler; a direct press on the chip
                // still shows where the popover lands over the menu, which is what this frame is for
                using (PointerDownEvent press = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = chip.worldBound.center, button = 0 }))
                {
                    press.target = chip;
                    chip.SendEvent(press);
                }

                yield return Wait(0.3f);
                Debug.Log("[ClassSelectCaptureRun] After a direct press on the chip, popover shown " + StageInterfaceOutput.IsVisible(popover));
            }

            Rect bound = popover.worldBound;
            Rect screen = actions.root.worldBound;
            Debug.Log("[ClassSelectCaptureRun] Kit popover '" + actions.root.Q<Label>("detail-title").text + "' shown "
                + StageInterfaceOutput.IsVisible(popover) + " at " + bound + " in " + screen + ", last child of its parent "
                + (popover.parent != null && popover.parent.IndexOf(popover) == popover.parent.childCount - 1));
            if (!StageInterfaceOutput.IsVisible(popover) || bound.yMin < screen.yMin || bound.yMax > screen.yMax)
            {
                _problems.Add("The kit chip's popover is not whole on the class screen: " + bound + " in " + screen);
            }

            yield return CaptureScreen("class-01b-kit-popover");
            if (StageInterfaceOutput.IsVisible(popover))
            {
                actions.Submit("detail-close-button");
                yield return Wait(0.3f);
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
                    + ToolkitClassSelect.SkillNames(played));
                Debug.Log("[ClassSelectCaptureRun] " + played.title + " units in data: "
                    + ToolkitClassSelect.UnitTitles(played));
            }

            Debug.Log("[ClassSelectCaptureRun] HUD spell cards: "
                + string.Join(", ", Titles(actions.root.Q("spell-list"))));
            Debug.Log("[ClassSelectCaptureRun] HUD party cards: "
                + string.Join(", ", Titles(actions.root.Q("party-list"))));
            LogHealer("Run start");
            LogCorner("Run start", new Vector2(0.05f, 0.05f));
            yield return CaptureScreen("class-03-" + Pick.ToLowerInvariant() + "-run");
            yield return Battle(actions, player, played);
        }

        // A few of the class's own units on the lower board, the battle started from the HUD, then the first spell
        // cast on an ally, each captured with where the healer stands and what it draws
        IEnumerator Battle(StageInterfaceActions actions, PlayerBehaviour player, CharacterData played)
        {
            if (player == null || played == null || played.entities.Count == 0 || _manager.entityManager == null)
            {
                _problems.Add("No played class to take into battle.");
                yield break;
            }

            Vector3[] offsets = { new Vector3(-2f, 0f, -2f), new Vector3(0f, 0f, -3f), new Vector3(2f, 0f, -2f),
                new Vector3(-3f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(0f, 0f, -5f) };
            // Dragged from the party row onto the board, as a player deploys, so the placement preview runs too
            actions.ConfigureLegacyInput();
            for (int i = 0; i < Mathf.Min(offsets.Length, played.entities.Count); i++)
            {
                Button card = actions.Cards("party-list").Find(b => (b.userData as ToolkitCardModel)?.canDrag == true);
                if (card == null)
                {
                    break;
                }

                yield return actions.BringIntoView(card);
                Vector2 start = StageInterfaceActions.ScreenPoint(card);
                Vector2 destination = (Vector2)_manager.gameCamera.WorldToScreenPoint(
                    player.grid.GetNearestWalkablePosition(offsets[i]))
                    - Vector2.up * (56 * ToolkitScreenLayout.GetScale(Screen.width, Screen.height, false));
                using (StagePresentationTouch touch = new StagePresentationTouch(actions))
                {
                    yield return touch.Frame(TouchPhase.Began, start);
                    yield return touch.Frame(TouchPhase.Moved, start + Vector2.up * 48);
                    yield return touch.Frame(TouchPhase.Moved, destination);
                    yield return StageCompactGestures.Still(touch, destination, 0.25f);
                    yield return touch.Frame(TouchPhase.Ended, destination);
                    yield return touch.Frame(TouchPhase.Ended, destination);
                }

                yield return Wait(0.3f);
            }

            actions.Dispose();
            if (_manager.entityManager.GetEntities(Entity.EntityType.Player).Count == 0)
            {
                // The drag did not land in this Game view: place the same units directly
                for (int i = 0; i < Mathf.Min(offsets.Length, played.entities.Count); i++)
                {
                    _manager.entityManager.SpawnEntity(played.entities[i], player.grid.GetNearestWalkablePosition(
                        offsets[i]), Entity.EntityType.Player);
                }
            }

            Debug.Log("[ClassSelectCaptureRun] Allies deployed: "
                + _manager.entityManager.GetEntities(Entity.EntityType.Player).Count);
            LogRenderers("Deployed");

            yield return Wait(0.5f);
            actions.Submit("wave-button");
            yield return Wait(3f);
            LogHealer("Battle");
            yield return CaptureScreen("class-04-" + Pick.ToLowerInvariant() + "-battle");
            List<Button> spells = actions.Cards("spell-list").FindAll(card => card.enabledInHierarchy);
            GameObject ally = _manager.entityManager.GetEntities(Entity.EntityType.Player).Find(go => go != null);
            if (spells.Count > 0 && ally != null)
            {
                // A frame sequence around arming the targeted spell and casting it on an ally, with the camera logged
                string prefix = "class-06-" + Pick.ToLowerInvariant() + "-target-";
                int frame = 0;
                LogCamera(prefix + frame, actions);
                yield return CaptureScreen(prefix + (frame++).ToString("00"));
                actions.Submit(spells[0]);
                for (int i = 0; i < 4; i++)
                {
                    yield return Wait(0.12f);
                    LogCamera(prefix + frame + " armed", actions);
                    yield return CaptureScreen(prefix + (frame++).ToString("00"));
                }

                Vector3 screen = _manager.gameCamera.WorldToScreenPoint(ally.transform.position + Vector3.up * 0.5f);
                actions.WorldTap(screen);
                for (int i = 0; i < 5; i++)
                {
                    yield return Wait(0.12f);
                    LogCamera(prefix + frame + " cast", actions);
                    yield return CaptureScreen(prefix + (frame++).ToString("00"));
                }
            }

            LogHealer("Cast");
            yield return CaptureScreen("class-05-" + Pick.ToLowerInvariant() + "-cast");
            yield return ReturnToMenu(actions);
        }

        // Every spell cast on an ally in turn while the battle plays, then Pause > Return to menu, as a player
        // leaves a run: the menu is captured and every renderer still loaded is dumped with its scene and path
        IEnumerator ReturnToMenu(StageInterfaceActions actions)
        {
            string pick = Pick.ToLowerInvariant();
            float until = Time.realtimeSinceStartup + 8f;
            int next = 0;
            while (Time.realtimeSinceStartup < until)
            {
                List<Button> spells = actions.Cards("spell-list").FindAll(card => card.enabledInHierarchy);
                GameObject ally = _manager.entityManager != null
                    ? _manager.entityManager.GetEntities(Entity.EntityType.Player).Find(go => go != null) : null;
                if (spells.Count > 0 && ally != null)
                {
                    actions.Submit(spells[next++ % spells.Count]);
                    yield return Wait(0.15f);
                    actions.WorldTap(_manager.gameCamera.WorldToScreenPoint(ally.transform.position + Vector3.up * 0.5f));
                }

                yield return Wait(0.6f);
            }

            LogRenderers("Run end");
            yield return CaptureScreen("class-07-" + pick + "-run-end");
            if (System.Environment.GetEnvironmentVariable("RENDER_CLASS_STOP_IN_RUN") == "1")
            {
                // Stop play in the middle of the run, as a player stops the Editor from Main
                yield break;
            }

            // A roster card armed for placement and left armed, as a card tapped and not yet dropped
            InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
            PlayerBehaviour player = Object.FindAnyObjectByType<PlayerBehaviour>();
            if (interaction != null && player != null && player.character != null
                && StageInterfaceOutput.IsVisible(actions.root.Q("gameover-panel")) == false)
            {
                List<EntityData> roster = player.character.data.entities;
                interaction.SetInteraction(new EntityGridInteraction(roster[roster.Count - 1]));
                yield return Wait(0.6f);
                LogPlacement("Armed placement");
                yield return CaptureScreen("class-07b-" + pick + "-armed-placement");
            }
            if (StageInterfaceOutput.IsVisible(actions.root.Q("gameover-panel")))
            {
                Debug.Log("[ClassSelectCaptureRun] The party fell, returning from the game over panel");
                actions.Submit(actions.root.Q("gameover-panel").Q<Button>());
            }
            else
            {
                actions.Submit("pause-button");
                yield return Wait(0.5f);
                actions.Submit("menu-button");
            }
            ToolkitGameUI menu = null;
            yield return WaitForHost(StageInterface.MenuPath, host => menu = host);
            yield return Wait(2f);
            LogStage("Back on the menu");
            LogRenderers("Menu after run");
            yield return CaptureScreen("class-08-" + pick + "-menu-after-run");
        }

        // Every drawn renderer whose screen footprint covers a viewport point, nearest first
        void LogCorner(string moment, Vector2 viewportPoint)
        {
            Camera camera = _manager.gameCamera;
            Ray ray = camera.ViewportPointToRay(viewportPoint);
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || !renderer.isVisible)
                {
                    continue;
                }

                if (renderer.bounds.IntersectRay(ray, out float distance))
                {
                    Material material = renderer.sharedMaterial;
                    Debug.Log($"[ClassSelectCaptureRun] {moment} corner {viewportPoint} {TransformPath(renderer.transform)} "
                        + $"{renderer.GetType().Name} distance {distance:F1} bounds {renderer.bounds.center} {renderer.bounds.size} "
                        + (material ? material.name + "/" + material.shader.name : "NULL"));
                }
            }
        }

        void LogPlacement(string moment)
        {
            StageCreaturePlacement placement = _manager.placement;
            GameObject model = placement != null ? placement.legacyModel : null;
            Debug.Log($"[ClassSelectCaptureRun] {moment}: preview {(placement != null && placement.preview != null)}, "
                + $"legacy model {(model != null ? model.name + " in " + model.scene.name + " at " + model.transform.position : "none")}");
            if (model != null)
            {
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    Debug.Log($"[ClassSelectCaptureRun] {moment} legacy renderer {TransformPath(renderer.transform)} "
                        + $"enabled {renderer.enabled} material "
                        + (renderer.sharedMaterial ? renderer.sharedMaterial.name + "/" + renderer.sharedMaterial.shader.name : "NULL"));
                }
            }
        }

        // Every renderer in every loaded scene, DontDestroyOnLoad included, with what it draws with
        void LogRenderers(string moment)
        {
            // Hidden (HideAndDontSave) objects too, which neither the hierarchy nor FindObjectsByType shows
            Renderer[] renderers = System.Array.FindAll(Resources.FindObjectsOfTypeAll<Renderer>(),
                renderer => renderer.gameObject.scene.IsValid() || (renderer.hideFlags & HideFlags.DontSave) != 0);
            Debug.Log($"[ClassSelectCaptureRun] {moment}: {renderers.Length} renderers");
            foreach (Renderer renderer in renderers)
            {
                bool shown = renderer.enabled && renderer.gameObject.activeInHierarchy;
                if (!shown && !moment.StartsWith("Menu"))
                {
                    continue;
                }

                List<string> materials = new List<string>();
                foreach (Material material in renderer.sharedMaterials)
                {
                    materials.Add(material == null ? "NULL" : material.name + "/" + (material.shader ? material.shader.name
                        + (material.shader.isSupported ? "" : " UNSUPPORTED") : "no shader"));
                }

                string scene = renderer.gameObject.scene.IsValid() ? renderer.gameObject.scene.name : "no scene "
                    + renderer.gameObject.hideFlags;
                if (moment.StartsWith("Menu") || materials.Exists(m => m == "NULL" || m.Contains("UNSUPPORTED")
                    || m.Contains("InternalError")))
                {
                    Debug.Log($"[ClassSelectCaptureRun] {moment} renderer [{scene}] {(shown ? "SHOWN" : "hidden")} "
                        + $"flags {renderer.gameObject.hideFlags} {TransformPath(renderer.transform)} "
                        + $"{renderer.GetType().Name} at {renderer.bounds.center} size {renderer.bounds.size} "
                        + string.Join(", ", materials));
                }
            }
        }

        void LogCamera(string moment, StageInterfaceActions actions)
        {
            Camera camera = _manager.gameCamera;
            InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
            AInteraction armed = interaction != null ? interaction.GetInteraction() : null;
            Debug.Log($"[ClassSelectCaptureRun] Camera {moment}: position {camera.transform.position:F3} euler "
                + $"{camera.transform.eulerAngles:F2} fov {camera.fieldOfView:F2}, world viewport "
                + $"{actions.ui.normalizedWorldViewport}, interaction {(armed != null ? armed.GetType().Name : "none")}");
        }

        // Where the character is, against the board and the screen, and every renderer that would draw pink
        void LogHealer(string moment)
        {
            PlayerBehaviour player = Object.FindAnyObjectByType<PlayerBehaviour>();
            Character character = player != null ? player.character : null;
            if (character == null)
            {
                Debug.Log("[ClassSelectCaptureRun] " + moment + ": no character");
                return;
            }

            Camera camera = _manager.gameCamera;
            Vector3 position = character.transform.position;
            Vector3 viewport = camera != null ? camera.WorldToViewportPoint(position) : Vector3.zero;
            string board = "none";
            if (player.grid != null && player.grid.cells != null && player.grid.cells.Length > 0)
            {
                float minZ = float.MaxValue;
                float maxZ = float.MinValue;
                foreach (GridCell cell in player.grid.cells)
                {
                    minZ = Mathf.Min(minZ, cell.center.z);
                    maxZ = Mathf.Max(maxZ, cell.center.z);
                }

                board = "cell z " + minZ + ".." + maxZ;
            }

            CharacterView view = character.GetComponentInChildren<CharacterView>();
            string cast = "no view";
            if (view != null)
            {
                cast = view.TryGetCastPoint(out Vector3 point)
                    ? "cast point " + point + " viewport " + (camera ? camera.WorldToViewportPoint(point) : Vector3.zero)
                    : "no screen cast point";
                cast += ", showBody " + view.showBody + ", view at " + view.transform.position;
            }

            Debug.Log($"[ClassSelectCaptureRun] {moment}: {character.data.title} at {position} viewport {viewport}, "
                + $"board {board}, {cast}");
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                bool near = Vector3.Distance(renderer.bounds.center, position) < 1.5f;
                foreach (Material material in renderer.sharedMaterials)
                {
                    bool broken = material == null || material.shader == null || !material.shader.isSupported
                        || material.shader.name == "Hidden/InternalErrorShader";
                    if (broken || near)
                    {
                        Debug.Log($"[ClassSelectCaptureRun] {moment} renderer {TransformPath(renderer.transform)}"
                            + $" {(broken ? "BROKEN" : "near")} material "
                            + (material != null ? material.name + " shader " + (material.shader ? material.shader.name : "null") : "null")
                            + " at " + renderer.bounds.center);
                    }
                }
            }
        }

        static string TransformPath(Transform transform)
        {
            return transform.parent == null ? transform.name : TransformPath(transform.parent) + "/" + transform.name;
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
