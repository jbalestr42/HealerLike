using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Native evidence for portraits, cosmetic placement and both live appearance paths.
    public sealed class StagePresentationRun : AStageRun
    {
        readonly StagePresentationOutput _output = new StagePresentationOutput();
        readonly StageInterfaceActions _actions = new StageInterfaceActions();
        StageGameViewSize _size;
        InteractionManager _interaction;
        EntityData _selectedData;
        Texture2D _cardPortrait;
        protected override bool shouldStartGame { get { return false; } }

        protected override void OnFailed(Exception error)
        {
            _output.Fail(error.ToString());
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            try
            {
                _output.Check(Regex.IsMatch(_output.manifest.revision ?? "", "^[0-9a-fA-F]{40}$"),
                    "Exact 40-character capture revision supplied");
                AttachInput();
                yield return Resize(1080, 1920);
                UnityEngine.Random.InitState(271828);
                yield return _actions.PointerTap("start-button");
                yield return Wait(0.8f);
                _output.Check(_actions.legacyModuleReadTouches, "Begin journey consumed actual multi-frame legacy input samples");
                yield return Portraits("01-portrait");
                yield return Resize(844, 390);
                yield return Portraits("02-landscape");
                yield return Resize(1080, 1920);
                yield return Placement();
                yield return StoneAppearance();
                yield return Navigation();
                yield return _output.VerifyImages();
                passed = true;
            }
            finally
            {
                _size?.Dispose();
                _output.Write(passed);
                StagePlay.Finish(this, passed);
            }
        }

        void AttachInput()
        {
            _actions.ui = Object.FindAnyObjectByType<ToolkitGameUI>();
            _output.Check(_actions.ui != null, "RenderStage has Toolkit interface");
            _actions.ConfigureLegacyInput();
            _interaction = Object.FindAnyObjectByType<InteractionManager>();
            _output.Check(Object.FindObjectsByType<ToolkitGameUI>(FindObjectsSortMode.None).Length == 1,
                "Exactly one Toolkit UI host");
        }

        IEnumerator Resize(int width, int height)
        {
            _size?.Dispose();
            _size = new StageGameViewSize(width, height);
            yield return Wait(0.8f);
            _output.Check(Screen.width == width && Screen.height == height, "Game frame is " + width + "x" + height);
            _output.Check(Mathf.Abs(_manager.gameCamera.aspect - (float)width / height) < 0.01f,
                "Camera agrees with Game frame aspect");
        }

        IEnumerator Capture(string name) { yield return _output.ui.Capture(_actions.ui, name); }

        IEnumerator Portraits(string prefix)
        {
            StageInterface attachment = _manager.GetComponent<StageInterface>();
            _output.Check(attachment.portraits != null && ReferenceEquals(_actions.ui.iconProvider, attachment.portraits),
                "Toolkit has the Render-owned creature portrait provider");
            yield return _actions.PointerTap("party-button");
            yield return Wait(0.2f);
            List<Button> cards = _actions.Cards("party-list");
            _output.Check(cards.Count > 0, "Party has actual creature cards");
            VisualElement icon = cards[0].Q("card-icon");
            _cardPortrait = icon.resolvedStyle.backgroundImage.texture;
            _output.Check(icon.ClassListContains("creature-portrait") && _cardPortrait != null
                && _cardPortrait.name.StartsWith("Creature Portrait ")
                && _cardPortrait.width == 256 && _cardPortrait.height == 256,
                "Party card shows the 256px creature screenshot");
            CheckPartyLabel(cards[0], "card-title", prefix);
            CheckPartyLabel(cards[0], "card-status", prefix);
            _output.ExportPortrait(_cardPortrait, prefix + "-cached-portrait");
            List<Camera> portraitCameras = PortraitCameras();
            _output.Check(portraitCameras.Count == 1, "Exactly one runtime portrait capture camera, including hidden objects; observed "
                + portraitCameras.Count);
            Camera portraitCamera = portraitCameras[0];
            _output.Check(portraitCamera.transform.parent != null
                && portraitCamera.transform.parent.name == CreaturePortraitRenderer.RootName
                && !portraitCamera.enabled && !portraitCamera.gameObject.activeInHierarchy,
                "Owned portrait camera remains disabled and inactive outside capture");
            int captures = attachment.portraits.captureCount;
            yield return Wait(0.45f);
            _output.Check(attachment.portraits.captureCount == captures
                && icon.resolvedStyle.backgroundImage.texture == _cardPortrait,
                "Repeated UI refresh reuses the captured texture");
            yield return Capture(prefix + "-party");
            yield return _actions.SelectCardByTouch(cards[0].parent.Q<Button>("card-info"));
            yield return Wait(0.25f);
            VisualElement detail = _actions.root.Q("detail-icon");
            _output.Check(detail != null && detail.ClassListContains("creature-portrait")
                && detail.resolvedStyle.backgroundImage.texture == _cardPortrait,
                "Expanded detail reuses the exact creature screenshot");
            _output.Check(_interaction.GetInteraction() == null, "Inspecting a creature does not begin placement");
            yield return Capture(prefix + "-detail");
            attachment.RefreshCreatureIcons();
            yield return Wait(0.2f);
            Texture2D refreshed = detail.resolvedStyle.backgroundImage.texture;
            _output.Check(refreshed != null && refreshed != _cardPortrait
                && refreshed.name.StartsWith("Creature Portrait "), "Vocabulary refresh replaces the visible portrait");
            _cardPortrait = refreshed;
            yield return _actions.PointerTap("detail-close-button");
            yield return Wait(0.2f);
            List<Button> spells = _actions.Cards("spell-list");
            _output.Check(spells.Count > 0, "Shipped spell cards provide non-creature coverage");
            VisualElement spellIcon = spells[0].Q("card-icon");
            _output.Check(spellIcon.resolvedStyle.backgroundImage.texture != null
                && !spellIcon.ClassListContains("creature-portrait"), "Non-creature spell keeps its normal icon fallback");
        }

        IEnumerator Select(int index)
        {
            if (!StageInterfaceOutput.IsVisible(_actions.root.Q("party-panel")))
                yield return _actions.PointerTap("party-button");
            yield return Wait(0.15f);
            List<Button> cards = _actions.Cards("party-list").FindAll(card => card.enabledInHierarchy
                && card.Q<Label>("card-status").text == "Deploy");
            _output.Check(cards.Count > index, "Requested deploy card exists: " + index);
            yield return _actions.SelectCardByTouch(cards[index]);
            yield return null;
            _output.Check(_interaction.GetInteraction() is EntityGridInteraction, "Toolkit touch starts the real grid interaction");
            _output.Check(_manager.placement.preview != null && _manager.placement.preview.rig != null,
                "Grid interaction has its generated cosmetic preview");
            _selectedData = _manager.placement.data;
            _output.Check(_selectedData != null, "Placement preview resolves selected EntityData");
            _output.Check(_manager.placement.legacyModel != null, "Original interaction model remains owned by gameplay");
            foreach (Renderer renderer in _manager.placement.legacyModel.GetComponentsInChildren<Renderer>(true))
                _output.Check(!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy,
                    "Original placement model renderer is invisible: " + renderer.name);
        }

        IEnumerator Placement()
        {
            int entities = EntityCount();
            string grid = GridSnapshot();
            yield return Select(0);
            CreaturePreview preview = _manager.placement.preview;
            Transform firstRoot = preview.rig.root;
            Vector3 point = _manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2f);
            using (StagePresentationTouch finger = new StagePresentationTouch(_actions))
            {
                yield return finger.Frame(TouchPhase.Began, _manager.gameCamera.WorldToScreenPoint(point));
                yield return finger.Frame(TouchPhase.Stationary, _manager.gameCamera.WorldToScreenPoint(point));
                _output.Check(Vector3.Distance(preview.rig.root.position, point) < 0.05f,
                    "Held first board touch positions generated preview before release");
                _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                    "Preview creates no gameplay entity and changes no grid occupancy");
                yield return Wait(CreatureAppearance.Duration + 0.15f);
                _output.Check(!preview.rig.isAppearing, "Placement appearance reaches its grown pose while finger remains held");
                float elapsed = preview.rig.appearanceElapsed;
                Vector3 moved = _manager.player.grid.GetNearestWalkablePosition(point + Vector3.forward * 2f);
                yield return finger.Frame(TouchPhase.Moved, _manager.gameCamera.WorldToScreenPoint(moved));
                yield return finger.Frame(TouchPhase.Stationary, _manager.gameCamera.WorldToScreenPoint(moved));
                _output.Check(ReferenceEquals(preview, _manager.placement.preview)
                    && preview.rig.appearanceElapsed >= elapsed && !preview.rig.isAppearing,
                    "Moving placement reuses the grown rig without replaying appearance");
                _output.Check(Vector3.Distance(preview.rig.root.position, moved) < 0.05f,
                    "Generated preview follows moved held touch");
                _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                    "Moving cosmetic preview still leaves entity count and occupancy unchanged");
                yield return Capture("03-held-placement");
                yield return finger.Frame(TouchPhase.Canceled, _manager.gameCamera.WorldToScreenPoint(moved));
            }
            yield return null;
            yield return _actions.PointerTap("cancel-button");
            yield return Wait(0.2f);
            _output.Check(_interaction.GetInteraction() == null && _manager.placement.preview == null
                && firstRoot == null, "Cancel destroys generated preview and ends interaction");
            _output.Check(EntityCount() == entities && GridSnapshot() == grid, "Cancelled placement leaves gameplay unchanged");

            yield return Select(0);
            CreaturePreview replaced = _manager.placement.preview;
            Transform replacedRoot = replaced.rig.root;
            EntityData previous = _selectedData;
            yield return Select(1);
            yield return null;
            _output.Check(_selectedData != previous && !ReferenceEquals(replaced, _manager.placement.preview)
                && replacedRoot == null, "Selecting another creature disposes the former preview");
            _output.Check(EntityCount() == entities && GridSnapshot() == grid, "Selection switch is cosmetic only");

            point = _manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2f);
            yield return Wait(CreatureAppearance.Duration + 0.1f);
            CreatureRig grown = _manager.placement.preview.rig;
            yield return LivePlacement(point, grown, entities);
            yield return null;
            _output.Check(_interaction.enabled, "Legacy mouse adapter restored after held touch ends");
        }

        IEnumerator LivePlacement(Vector3 point, CreatureRig grown, int entities)
        {
            BattleFocus focus = _manager.GetComponentInChildren<BattleFocus>();
            bool wasEnabled = focus.enabled;
            Camera camera = _manager.gameCamera;
            Pose previous = new Pose(camera.transform.position, camera.transform.rotation);
            focus.enabled = false;
            Fit(grown, point - grown.root.position);
            _output.manifest.interventions.Add("Plant appearance camera fitted once to the fully grown placement bounds; live creature spawned by the real held touch release");
            try
            {
                yield return null;
                // The camera is fixed before the gesture starts, so the tap threshold measures finger motion only.
                using (StagePresentationTouch finger = new StagePresentationTouch(_actions))
                {
                    Vector2 screen = camera.WorldToScreenPoint(point);
                    yield return finger.Frame(TouchPhase.Began, screen);
                    yield return finger.Frame(TouchPhase.Stationary, screen);
                    yield return Wait(0.16f);
                    yield return finger.Frame(TouchPhase.Ended, screen);
                }
                _output.Check(EntityCount() == entities + 1, "First held world touch places exactly one gameplay entity");
                List<GameObject> allies = _manager.entityManager.GetEntities(Entity.EntityType.Player);
                GameObject placed = allies[allies.Count - 1];
                yield return Appearance(placed, "plant", AssetDatabase.GetAssetPath(_selectedData));
                _output.Check(_manager.placement.preview == null && _interaction.GetInteraction() == null,
                    "Successful deployment releases cosmetic preview and grid interaction");
            }
            finally
            {
                camera.transform.SetPositionAndRotation(previous.position, previous.rotation);
                focus.enabled = wasEnabled;
            }
        }

        IEnumerator StoneAppearance()
        {
            EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(StagePlayer.Allies[0]);
            Vector3 point = _manager.player.grid.GetNearestWalkablePosition(Vector3.right * 2f);
            GameObject measureRoot = new GameObject("Stone capture framing");
            BattleFocus focus = _manager.GetComponentInChildren<BattleFocus>();
            bool wasEnabled = focus.enabled;
            Camera camera = _manager.gameCamera;
            Pose previous = new Pose(camera.transform.position, camera.transform.rotation);
            focus.enabled = false;
            try
            {
                using (CreaturePreview measure = new CreaturePreview())
                {
                    _output.Check(measure.Init(_manager.creatureLooks, data, Entity.EntityType.Computer,
                        _manager.meshes, measureRoot.transform, StageCalibration.CellSize), "Stone framing resolves real shared creature source");
                    measure.CompleteAppearance();
                    measure.Tick(Time.time, 0f, new FootFrame(point, Vector3.up, StageCalibration.CellSize), -camera.transform.forward);
                    Fit(measure.rig);
                }
                Object.Destroy(measureRoot);
                yield return null;
                _output.manifest.interventions.Add("Stone spawned through EntityManager.SpawnEntity during planning because natural enemy startup precedes capture; camera fitted once using fully grown shared preview; production host owns all animation");
                GameObject stone = _manager.entityManager.SpawnEntity(data, point, Entity.EntityType.Computer);
                _output.Check(stone != null, "Controlled real enemy spawn succeeds on an open cell");
                // SpawnEntity assigns its final position after Init notifies render observers.
                yield return null;
                yield return Appearance(stone, "stone", AssetDatabase.GetAssetPath(data));
            }
            finally
            {
                if (measureRoot != null) Object.Destroy(measureRoot);
                camera.transform.SetPositionAndRotation(previous.position, previous.rotation);
                focus.enabled = wasEnabled;
            }
        }

        IEnumerator Appearance(GameObject entity, string subject, string source)
        {
            float started = Time.unscaledTime;
            CreatureBuilder host = entity.GetComponentInChildren<CreatureBuilder>();
            while (host == null || host.rig == null)
            {
                _output.Check(Time.unscaledTime - started < 3f, "Live spawn acquires creature host in time");
                yield return null;
                host = entity.GetComponentInChildren<CreatureBuilder>();
            }
            CreatureRig rig = host.rig;
            float[] samples = { 0f, 0.12f, 0.25f, 0.4f, 0.6f, 0.8f, 1.1f };
            int next = 0;
            bool sawSequential = false;
            int partialFrames = 0;
            Pose camera = new Pose(_manager.gameCamera.transform.position, _manager.gameCamera.transform.rotation);
            while (Time.unscaledTime - started < 1.3f || rig.isAppearing)
            {
                float elapsed = Time.unscaledTime - started;
                _output.Check(elapsed < 4f, subject + " appearance completes within four real seconds");
                string file = null;
                if (next < samples.Length && elapsed >= samples[next])
                {
                    file = "growth-" + subject + "-" + next.ToString("00") + ".png";
                    // One screenshot per rendered frame, even if a slow frame crossed multiple thresholds.
                    do { next++; } while (next < samples.Length && elapsed >= samples[next]);
                }
                StagePresentationOutput.GrowthFrame frame = _output.Sample(rig, _manager.gameCamera,
                    subject, source, started, file);
                float min = float.MaxValue;
                float max = 0f;
                foreach (StagePresentationOutput.PartScale part in frame.parts)
                {
                    float ratio = part.scale.magnitude / Mathf.Max(0.0001f, part.authoredScale.magnitude);
                    min = Mathf.Min(min, ratio);
                    max = Mathf.Max(max, ratio);
                }
                if (rig.isAppearing) partialFrames++;
                sawSequential |= rig.isAppearing && max - min > 0.2f;
                _output.Check(Vector3.Distance(camera.position, _manager.gameCamera.transform.position) < 0.0001f
                    && Quaternion.Angle(camera.rotation, _manager.gameCamera.transform.rotation) < 0.001f,
                    subject + " growth camera stays fixed");
                yield return null;
            }
            _output.Check(partialFrames >= 2 && sawSequential,
                subject + " production frames show sequential part scaling, with at least two partial frames");
            _output.Check(!rig.isAppearing && rig.appearanceElapsed >= CreatureAppearance.Duration,
                subject + " production appearance reaches exact timeline completion");
            _output.Sample(rig, _manager.gameCamera, subject, source, started, "growth-" + subject + "-grown.png");
            CheckVisible(rig, subject);
            yield return null;
        }

        void Fit(CreatureRig rig, Vector3 offset = default)
        {
            Bounds bounds = RigBounds(rig);
            bounds.center += offset;
            bounds.Expand(0.4f);
            Camera camera = _manager.gameCamera;
            Pose pose = StageViewport.Fit(bounds, camera.transform.rotation, camera.fieldOfView,
                camera.aspect, StageViewport.Inset(_actions.ui.normalizedWorldViewport, 0.08f));
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        void CheckVisible(CreatureRig rig, string subject)
        {
            Bounds bounds = RigBounds(rig);
            Rect viewport = _actions.ui.normalizedWorldViewport;
            Vector2 min = Vector2.one;
            Vector2 max = Vector2.zero;
            for (int i = 0; i < 8; i++)
            {
                Vector3 screen = _manager.gameCamera.WorldToViewportPoint(bounds.center
                    + Vector3.Scale(bounds.extents, RenderMath.CornerSign(i)));
                _output.Check(screen.z > 0f && viewport.Contains(screen), subject + " fully grown body stays inside free Game viewport");
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            _output.Check((max.y - min.y) * Screen.height > 300f, subject + " body is at least 300 pixels tall for visual judgment");
        }

        static Bounds RigBounds(CreatureRig rig)
        {
            Bounds bounds = new Bounds(rig.root.position, Vector3.zero);
            foreach (Renderer renderer in rig.root.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        IEnumerator Navigation()
        {
            yield return Wait(0.3f);
            yield return Select(0);
            Transform preview = _manager.placement.preview.rig.root;
            StageInterface attachment = _manager.GetComponent<StageInterface>();
            var portraits = attachment.portraits;
            Texture2D texture = portraits.GetCreatureIcon(_selectedData, Entity.EntityType.Player);
            _actions.Submit("pause-button");
            yield return Wait(0.2f);
            _actions.Submit("menu-button");
            yield return Wait(1f);
            _output.Check(preview == null && portraits.isDisposed && texture == null,
                "Scene exit disposes placement preview and portrait textures");
            _output.Check(PortraitCameras().Count == 0, "Scene exit destroys every owned portrait capture camera");
            AttachInput();
            _output.Check(SceneManager.GetActiveScene().path == StageInterface.MenuPath, "Pause menu reaches Toolkit menu");
            yield return Capture("04-menu");
            yield return _actions.PointerTap("start-button");
            yield return Wait(1.2f);
            AttachInput();
            _output.Check(Object.FindAnyObjectByType<RenderManager>() == _manager,
                "New expedition retains the single original RenderManager");
            yield return _actions.PointerTap("start-button");
            yield return Wait(0.8f);
            yield return Portraits("05-new-expedition");
        }

        int EntityCount() { return Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Length; }

        void CheckPartyLabel(Button card, string name, string context)
        {
            Label label = card.Q<Label>(name);
            ScrollView scroll = card.GetFirstAncestorOfType<ScrollView>();
            VisualElement drawer = _actions.root.Q("party-panel");
            _output.Check(label != null && scroll != null && drawer != null,
                context + " party card provides " + name + " inside a ScrollView");
            Rect clip = Intersect(scroll.contentViewport.worldBound, _actions.root.worldBound);
            clip = Intersect(clip, drawer.worldBound);
            Rect bounds = label.worldBound;
            // IsVisible alone checks layout/display, not whether the scroll viewport actually clips the text.
            _output.manifest.checks.Add(context + " " + name + " bounds=" + bounds + ", visibleScrollClip=" + clip);
            _output.Check(StageInterfaceOutput.IsVisible(label) && !string.IsNullOrWhiteSpace(label.text)
                && clip.width > 0f && clip.height > 0f && bounds.xMin >= clip.xMin - 1f
                && bounds.yMin >= clip.yMin - 1f && bounds.xMax <= clip.xMax + 1f
                && bounds.yMax <= clip.yMax + 1f,
                context + " first party card " + name + " is fully inside the visible scroll viewport at "
                    + Screen.width + "x" + Screen.height);
        }

        static Rect Intersect(Rect a, Rect b)
        {
            float x = Mathf.Max(a.xMin, b.xMin);
            float y = Mathf.Max(a.yMin, b.yMin);
            return new Rect(x, y, Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - x),
                Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - y));
        }

        List<Camera> PortraitCameras()
        {
            List<Camera> cameras = new List<Camera>();
            // HideAndDontSave removes a camera internally from its Scene. Scene.IsValid therefore cannot
            // distinguish these owned runtime cameras from assets. Include hidden objects and reject assets.
            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (camera.name != "Portrait Camera" || EditorUtility.IsPersistent(camera)) continue;
                cameras.Add(camera);
                _output.manifest.checks.Add("Observed portrait camera instance " + camera.GetEntityId()
                    + ", parent=" + (camera.transform.parent != null ? camera.transform.parent.name : "none")
                    + ", sceneValid=" + camera.gameObject.scene.IsValid() + ", flags=" + camera.gameObject.hideFlags
                    + ", enabled=" + camera.enabled + ", active=" + camera.gameObject.activeInHierarchy);
            }
            return cameras;
        }

        string GridSnapshot()
        {
            GridManager grid = _manager.player.grid;
            char[] cells = new char[grid.width * grid.height];
            for (int y = 0; y < grid.height; y++)
                for (int x = 0; x < grid.width; x++) cells[y * grid.width + x] = grid.IsWalkable(x, y) ? '1' : '0';
            return new string(cells);
        }
    }
}
