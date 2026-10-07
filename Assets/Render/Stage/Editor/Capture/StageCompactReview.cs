using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    // Review cases run on the same native scene and input machinery as the original acceptance.
    public sealed class StageCompactReview
    {
        readonly StageCaptureSession _s;
        public StageCompactReview(StageCaptureSession session) { _s = session; }

        public IEnumerator OutsideDismiss(string image)
        {
            Rect world = _s.actions.ui.normalizedWorldViewport;
            Vector2 point = Vector2.zero;
            bool empty = false;
            for (int row = 1; row < 10 && !empty; row++)
            {
                for (int column = 1; column < 10; column++)
            {
                point = new Vector2((world.x + world.width * column / 10f) * Screen.width,
                    (world.y + world.height * row / 10f) * Screen.height);
                bool creature = Physics.Raycast(_s.manager.gameCamera.ScreenPointToRay(point), out RaycastHit hit)
                    && hit.collider.GetComponentInParent<Entity>() != null;
                if (!creature && !_s.actions.touch.IsOverInterface(point)) { empty = true; break; }
            }
            }

            _s.output.Check(empty, "Outside dismissal uses an empty battlefield point with no UI or creature hit");
            _s.output.Check(StageInterfaceOutput.IsVisible(_s.actions.root.Q("detail-panel")),
                "Persistent details are open before outside battlefield tap");
            yield return _s.actions.TouchGesture(point);
            yield return Wait(.2f);
            _s.output.Check(!StageInterfaceOutput.IsVisible(_s.actions.root.Q("detail-panel")),
                "Native empty battlefield press dismisses persistent details: " + image);
            yield return _s.Capture(image, "Synthetic native touch through StandaloneInputModule and world-origin press dismissal");
        }

        public IEnumerator LiveDetails(Button entry)
        {
            Entity entity = (Entity)((ToolkitCardModel)entry.userData).source;
            var root = _s.actions.root;
            string before = root.Q<Label>("detail-description").text;
            float health = entity.health.Value;
            var consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
            consumer.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = 7 } } };
            entity.health.AddResourceModifier(ResourceModifier.Create(consumer, entity.gameObject, entity.gameObject));
            yield return Wait(.3f);
            UnityEngine.Object.Destroy(consumer);
            _s.output.Check(entity.health.Value < health, "Public health consumer changes the live inspected creature after hold release");
            string summary = root.Q<Label>("detail-description").text;
            _s.output.Check(summary != before && summary.Contains(ToolkitPresentation.Resource(entity.health.Value, entity.health.Max))
                && root.Q<Label>("detail-title").text == entity.data.title
                && StageInterfaceOutput.IsVisible(root.Q("detail-panel")),
                "Persistent details refresh actual health after release without changing inspected subject");
            var equipment = root.Q<Button>("detail-inventory-button");
            var viewport = root.Q<ScrollView>("detail-scroll").contentViewport.worldBound;
            _s.output.Check(equipment.worldBound.xMin >= viewport.xMin - 1 && equipment.worldBound.yMin >= viewport.yMin - 1
                && equipment.worldBound.xMax <= viewport.xMax + 1 && equipment.worldBound.yMax <= viewport.yMax + 1,
                "Equipment is fully visible in the initial compact summary without scrolling: " + equipment.worldBound + " inside " + viewport);
            _s.output.Check(!summary.Contains("HealthMax") && !summary.Contains("CriticalChance"),
                "Default summary excludes redundant maximum health and neutral attributes");
            Rect summaryBounds = root.Q("detail-description").worldBound;
            _s.output.Check(summaryBounds.yMin >= viewport.yMin - 1 && summaryBounds.yMax <= viewport.yMax + 1,
                "Initial portrait summary fits before scrolling: " + summaryBounds + " inside " + viewport);
            yield return _s.Capture("07b-persistent-live-health");
            var foldout = root.Q<Foldout>("detail-attributes");
            var toggle = foldout.Q<Toggle>();
            root.Q<ScrollView>("detail-scroll").ScrollTo(toggle);
            yield return Wait(.15f);
            Rect target = toggle.worldBound;
            viewport = root.Q<ScrollView>("detail-scroll").contentViewport.worldBound;
            _s.output.Check(target.width >= 44 && target.height >= 44
                && target.yMin >= viewport.yMin - 1 && target.yMax <= viewport.yMax + 1,
                "All attributes resolved touch target is at least 44 logical pixels and fully visible: " + target);
            foreach (float edge in new[] { 1f, target.height - 1f })
            {
                VisualElement picked = toggle.panel.Pick(new Vector2(target.center.x, target.yMin + edge));
                _s.output.Check(picked == toggle || toggle.Contains(picked),
                    "All attributes target is pickable at vertical inset " + edge);
            }
            yield return _s.actions.TouchGesture(StageInterfaceActions.ScreenPoint(toggle));
            yield return Wait(.15f);
            _s.output.Check(foldout.value && root.Q<Label>("detail-full-stats").text.Contains("Maximum health"),
                "Actual All attributes tap expands the complete readable attribute list");
            var scroll = root.Q<ScrollView>("detail-scroll");
            scroll.scrollOffset = new Vector2(0, foldout.worldBound.yMin - scroll.contentContainer.worldBound.yMin);
            yield return Wait(.15f);
            yield return _s.Capture("07c-expanded-attributes");
            yield return _s.actions.TouchGesture(StageInterfaceActions.ScreenPoint(toggle));
            root.Q<ScrollView>("detail-scroll").scrollOffset = Vector2.zero;
            yield return Wait(.15f);
        }

        public IEnumerator ControllerInspect(Button spell)
        {
            yield return _s.actions.BringIntoView(spell);
            spell.Focus();
            yield return null;
            float mana = _s.manager.player.character.mana.Value;
            _s.actions.captureInput.navigationButton = "Cancel";
            yield return null;
            _s.actions.captureInput.navigationButton = null;
            yield return Wait(.15f);
            _s.output.Check(StageInterfaceOutput.IsVisible(_s.actions.root.Q("detail-panel"))
                && _s.manager.player.character.mana.Value == mana && _s.interaction.GetInteraction() == null,
                "Native input module Cancel action inspects an unavailable spell without casting");
            yield return _s.Capture("14d-controller-inspection", "Synthetic Cancel action through actual StandaloneInputModule, no physical controller");
            _s.actions.captureInput.navigationButton = "Submit";
            yield return null;
            _s.actions.captureInput.navigationButton = null;
            yield return Wait(.15f);
            _s.output.Check(!StageInterfaceOutput.IsVisible(_s.actions.root.Q("detail-panel"))
                && _s.manager.player.character.mana.Value == mana && _s.interaction.GetInteraction() == null,
                "Ordinary Submit closes inspection and cannot activate an unavailable spell");
        }

        // Synthetic Toolkit keys cannot set legacy Input.GetKeyDown. Exercise the real
        // navigation module in the stamped frame, then invoke the unchanged Escape route
        // exactly once. Reflection is only fixture access to that private route, never
        // navigation source classification. Physical polling remains unobserved.
        void Escape(Button spell, bool inspectionOpen, AInteraction targeting)
        {
            var root = _s.actions.root;
            _s.output.Check(ReferenceEquals(spell.focusController.focusedElement, spell),
                "Keyboard Escape starts with the compact spell card focused");
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
            {
                spell.SendEvent(key);
            }

            int cancellations = 0;
            EventCallback<NavigationCancelEvent> observed = _ => cancellations++;
            root.RegisterCallback(observed);
            _s.actions.captureInput.navigationButton = "Cancel";
            try { EventSystem.current.currentInputModule.Process(); }
            finally
            {
                _s.actions.captureInput.navigationButton = null;
                root.UnregisterCallback(observed);
            }
            _s.output.Check(cancellations == 1, "Keyboard navigation Cancel bubbles once without card inspection consuming it");
            _s.output.Check(StageInterfaceOutput.IsVisible(root.Q("detail-panel")) == inspectionOpen
                && ReferenceEquals(targeting, _s.interaction.GetInteraction()) && Time.timeScale > 0,
                "Keyboard navigation alone neither toggles inspection nor cancels targeting or pauses");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var actions = (ToolkitGameActions)typeof(ToolkitGameUI).GetField("_actions", flags).GetValue(_s.actions.ui);
            typeof(ToolkitGameActions).GetMethod("OnEscape", flags).Invoke(actions, null);
        }

        public IEnumerator KeyboardEscapeOwnership(Button spell)
        {
            const string input = "Synthetic Escape KeyDown plus actual Standalone Cancel; existing OnEscape invoked once, hardware key poll unobserved";
            var root = _s.actions.root;
            float mana = _s.manager.player.character.mana.Value;
            yield return _s.actions.BringIntoView(spell);
            spell.Focus(); yield return null;
            Escape(spell, false, null);
            yield return Wait(.15f);
            _s.output.Check(Time.timeScale == 0 && StageInterfaceOutput.IsVisible(root.Q("pause-panel"))
                && !StageInterfaceOutput.IsVisible(root.Q("detail-panel")),
                "Focused-card Escape with no popup pauses once instead of swallowing cancellation");
            yield return _s.Capture("10a-keyboard-escape-pause", input);
            yield return _s.actions.PointerTap("resume-button");
            yield return Wait(.15f);
            spell.Focus(); yield return null;
            _s.actions.captureInput.navigationButton = "Cancel";
            yield return null;
            _s.actions.captureInput.navigationButton = null;
            yield return Wait(.15f);
            _s.output.Check(StageInterfaceOutput.IsVisible(root.Q("detail-panel")) && Time.timeScale > 0,
                "Controller Cancel still inspects on a later frame after keyboard Escape");
            Escape(spell, true, null);
            yield return Wait(.15f);
            _s.output.Check(Time.timeScale > 0 && !StageInterfaceOutput.IsVisible(root.Q("detail-panel"))
                && !StageInterfaceOutput.IsVisible(root.Q("pause-panel")),
                "Escape closes navigation inspection once without falling through to pause");
            yield return _s.Capture("10b-keyboard-escape-inspection-close", input);
            spell.Focus(); yield return null;
            _s.actions.captureInput.navigationButton = "Submit";
            yield return null;
            _s.actions.captureInput.navigationButton = null;
            yield return Wait(.15f);
            AInteraction targeting = _s.interaction.GetInteraction();
            _s.output.Check(targeting != null, "Ordinary controller Submit still starts usable spell targeting");
            Escape(spell, false, targeting);
            yield return Wait(.15f);
            _s.output.Check(_s.interaction.GetInteraction() == null && Time.timeScale > 0
                && !StageInterfaceOutput.IsVisible(root.Q("detail-panel"))
                && !StageInterfaceOutput.IsVisible(root.Q("pause-panel"))
                && _s.manager.player.character.mana.Value == mana,
                "Escape cancels real spell targeting without inspection, pause or mana spend");
            yield return _s.Capture("10c-keyboard-escape-targeting-cancel", input);
        }

        public void PortraitPixels()
        {
            var data = (EntityData)((ToolkitCardModel)_s.actions.Cards("party-list")[0].userData).source;
            Texture2D portrait = _s.actions.ui.iconProvider.GetCreatureIcon(data, Entity.EntityType.Player);
            var target = RenderTexture.GetTemporary(portrait.width, portrait.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var readable = new Texture2D(portrait.width, portrait.height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(portrait, target);
                RenderTexture.active = target;
                readable.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                readable.Apply();
                var pixels = readable.GetPixels32();
                int transparent = pixels.Count(c => c.a == 0), opaque = pixels.Count(c => c.a == 255);
                _s.output.Check(transparent > pixels.Length / 2 && opaque > 500,
                    "Native 256px portrait contains transparent clear and opaque creature pixels: " + transparent + "/" + opaque);
                _s.output.Check(pixels[0].a == 0 && pixels[portrait.width - 1].a == 0
                    && pixels[pixels.Length - 1].a == 0, "Portrait clear corners have zero alpha");
                File.WriteAllBytes(Path.Combine(StagePlay.CaptureFolder, "portrait-cutout.png"), readable.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(readable);
            }
        }
    }
}
