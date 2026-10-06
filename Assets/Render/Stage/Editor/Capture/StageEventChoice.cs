using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UiButton = UnityEngine.UI.Button;

namespace HealerLike.Render.Stage
{
    // Julien's event and rest choices are uGUI buttons on their own overlay canvas, under the live Toolkit HUD.
    // A choice is taken by the same multi-frame touch as every Toolkit button, read by the StandaloneInputModule;
    // nothing invokes onClick or EventView.Select directly.
    public sealed class StageEventChoice
    {
        readonly StageCaptureSession _session;
        readonly StageEventRoomRun.Manifest _manifest;
        public StageEventChoice(StageCaptureSession session, StageEventRoomRun.Manifest manifest)
        {
            _session = session;
            _manifest = manifest;
        }

        public EventView view
        {
            get
            {
                UIManager owner = Object.FindAnyObjectByType<UIManager>();
                return owner != null ? owner.GetView<EventView>(ViewType.Event) : null;
            }
        }

        public IEnumerator WaitForChoices(string title)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!IsShowing(title))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new InvalidOperationException("Julien's choice screen never showed " + title);
                }

                yield return null;
            }

            // The layout group places the new buttons on the next canvas update
            yield return AStageRun.Wait(0.3f);
        }

        bool IsShowing(string title)
        {
            EventView eventView = view;
            if (eventView == null || !eventView.isActiveAndEnabled || eventView.choiceButtons.Count == 0)
            {
                return false;
            }

            return title == null || Array.Exists(eventView.GetComponentsInChildren<TMPro.TMP_Text>(),
                label => label.text == title);
        }

        public UiButton Choice(string label)
        {
            foreach (UiButton button in view.choiceButtons)
            {
                if (button != null && button.name == "Choice " + label)
                {
                    return button;
                }
            }

            throw new InvalidOperationException("No choice named " + label + " on Julien's screen");
        }

        public static Vector2 ScreenPoint(UiButton button)
        {
            RectTransform rect = (RectTransform)button.transform;
            Canvas canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        }

        // What the EventSystem would hand the touch to, every raycaster sorted as the input module sorts them
        public static List<RaycastResult> Hits(Vector2 point)
        {
            PointerEventData pointer = new PointerEventData(EventSystem.current) { position = point };
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits;
        }

        static string Describe(List<RaycastResult> hits)
        {
            List<string> names = new List<string>();
            foreach (RaycastResult hit in hits)
            {
                names.Add(hit.gameObject.name + " (" + hit.module.GetType().Name + ")");
            }

            return names.Count > 0 ? string.Join(", ", names) : "nothing";
        }

        // The seam, before the choice is taken: our HUD is still up with its buttons off, a touch on it changes
        // nothing on Julien's screen, and the first hit at his button is his button, not our panel
        public IEnumerator CheckSeam(string room, UiButton target)
        {
            AscensionGameType ascension = Object.FindAnyObjectByType<AscensionGameType>();
            VisualElement root = _session.actions.root;
            _session.output.Check(StageInterfaceOutput.IsVisible(root.Q("hud-root")),
                room + ": the Toolkit HUD stays visible under Julien's choice screen");
            UnityEngine.UIElements.Button map = root.Q<UnityEngine.UIElements.Button>("map-button");
            _session.output.Check(StageInterfaceOutput.IsVisible(map) && !map.enabledInHierarchy,
                room + ": the HUD map button is shown and disabled while the choice is open");
            List<string> enabled = new List<string>();
            root.Query<UnityEngine.UIElements.Button>().ForEach(button =>
            {
                if (StageInterfaceOutput.IsVisible(button) && button.enabledInHierarchy)
                {
                    enabled.Add(button.name);
                }
            });
            _manifest.enabledHudButtons.Add(room + ": " + (enabled.Count > 0 ? string.Join(", ", enabled) : "none"));

            Vector2 choicePoint = ScreenPoint(target);
            List<RaycastResult> hits = Hits(choicePoint);
            _manifest.choiceHits.Add(room + " " + target.name + " at " + choicePoint + ": " + Describe(hits));
            _session.output.Check(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(target.transform),
                room + ": the first EventSystem hit at Julien's choice is that choice, not the Toolkit panel ("
                + Describe(hits) + ")");

            List<UiButton> before = new List<UiButton>(view.choiceButtons);
            AscensionGameType.State state = LegacyUiReader.AscensionState(ascension);
            Vector2 mapPoint = StageInterfaceActions.ScreenPoint(map);
            _manifest.hudHits.Add(room + " map-button at " + mapPoint + ": " + Describe(Hits(mapPoint)));
            yield return _session.actions.TouchGesture(mapPoint);
            yield return AStageRun.Wait(0.2f);
            _session.output.Check(view.isActiveAndEnabled && SameButtons(before, view.choiceButtons)
                && LegacyUiReader.AscensionState(ascension) == state
                && !StageInterfaceOutput.IsVisible(root.Q("map-panel")),
                room + ": a touch on the disabled HUD map button leaves Julien's screen, its choices and the run "
                + "state as they were");
        }

        static bool SameButtons(List<UiButton> before, IReadOnlyList<UiButton> after)
        {
            if (before.Count != after.Count)
            {
                return false;
            }

            for (int index = 0; index < before.Count; index++)
            {
                if (before[index] != after[index] || before[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        // One real touch on the choice, checked to leave the battlefield untouched
        public IEnumerator Tap(UiButton button)
        {
            if (button == null || !button.IsInteractable() || !button.isActiveAndEnabled)
            {
                throw new InvalidOperationException("Julien's choice is not available: "
                    + (button != null ? button.name : "null"));
            }

            int allies = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            ISelectable selected = StageMapReadout.Selected(_session.interaction);
            int clicks = 0;
            UnityEngine.Events.UnityAction count = () => clicks++;
            button.onClick.AddListener(count);
            string name = button.name;
            Vector2 point = ScreenPoint(button);
            try
            {
                yield return _session.actions.TouchGesture(point);
                yield return AStageRun.Wait(0.2f);
            }
            finally
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(count);
                }
            }

            _manifest.clicks.Add(name + " at " + point + ": onClick " + clicks);
            _session.output.Check(clicks == 1, "One touch on " + name + " fired its onClick exactly once");
            _session.output.Check(_session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == allies
                && StageMapReadout.Selected(_session.interaction) == selected
                && _session.interaction.GetInteraction() == null,
                "The touch on " + name + " placed, selected or started nothing on the battlefield");
        }
    }
}
