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
    // The game's event and rest choices are uGUI buttons on their own overlay canvas, under the live Toolkit HUD.
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
        // nothing on the choice screen, and the first hit at a choice button is that button, not our panel
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
            yield return CheckLiveCards(room, before, state);
        }

        // The spell and roster cards stay enabled under the choice screen: a tap on a spell and a drag of a unit onto
        // the field must still do nothing while a choice is open
        IEnumerator CheckLiveCards(string room, List<UiButton> before, AscensionGameType.State state)
        {
            AscensionGameType ascension = Object.FindAnyObjectByType<AscensionGameType>();
            RenderManager manager = _session.manager;
            float mana = manager.player.character.mana.Value;
            int allies = manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            ISelectable selected = StageMapReadout.Selected(_session.interaction);
            UnityEngine.UIElements.Button spell = _session.actions.Cards("spell-list")[0];
            Vector2 spellPoint = StageInterfaceActions.ScreenPoint(spell);
            _manifest.hudHits.Add(room + " spell card at " + spellPoint + ": " + Describe(Hits(spellPoint)));
            yield return _session.actions.TouchGesture(spellPoint);
            yield return AStageRun.Wait(0.3f);
            bool spellIdle = manager.player.character.mana.Value == mana && _session.interaction.GetInteraction() == null
                && StageMapReadout.Selected(_session.interaction) == selected;
            _manifest.cardEffects.Add(room + " spell tap: mana " + mana + " -> " + manager.player.character.mana.Value
                + ", interaction " + (_session.interaction.GetInteraction() != null ? "started" : "none"));
            _manifest.cardEffects.Add(room + " after the spell tap: " + ScreenState());
            _session.output.Check(spellIdle, room + ": a tap on a spell card under Julien's screen spends no mana "
                + "and starts no targeting");

            UnityEngine.UIElements.Button unit = _session.actions.Cards("party-list")[0];
            yield return _session.actions.BringIntoView(unit);
            Vector2 start = StageInterfaceActions.ScreenPoint(unit);
            Vector3 cell = manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2);
            Vector2 drop = new StageCompactGestures(_session).DropPoint(cell);
            _manifest.hudHits.Add(room + " unit drop at " + drop + ": " + Describe(Hits(drop)));
            using (StagePresentationTouch touch = new StagePresentationTouch(_session.actions))
            {
                yield return touch.Frame(TouchPhase.Began, start);
                yield return touch.Frame(TouchPhase.Moved, start + Vector2.up * 48);
                yield return touch.Frame(TouchPhase.Moved, drop);
                yield return StageCompactGestures.Still(touch, drop, 0.25f);
                // One release only: StandaloneInputModule forgets the finger on Ended, so a second Ended frame
                // is a new finger pressed and released on the spot, a tap on whatever lies under the drop
                yield return touch.Frame(TouchPhase.Ended, drop);
                _session.actions.captureInput.samples = System.Array.Empty<Touch>();
                _session.actions.touch.captureTouches = System.Array.Empty<Touch>();
                yield return null;
            }

            yield return AStageRun.Wait(0.25f);
            int alliesAfter = manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            _manifest.cardEffects.Add(room + " unit drag to the field: allies " + allies + " -> " + alliesAfter);
            _manifest.cardEffects.Add(room + " after the unit drag: " + ScreenState());
            _session.output.Check(alliesAfter == allies && _session.interaction.GetInteraction() == null,
                room + ": dragging a unit card onto the field under Julien's screen places nobody");
            _session.output.Check(view.isActiveAndEnabled && SameButtons(before, view.choiceButtons)
                && LegacyUiReader.AscensionState(ascension) == state,
                room + ": the spell tap and the unit drag leave Julien's screen and the run state as they were");
        }

        string ScreenState()
        {
            EventView eventView = view;
            if (eventView == null || !eventView.isActiveAndEnabled)
            {
                return "Julien's screen closed, state " + LegacyUiReader.AscensionState(
                    Object.FindAnyObjectByType<AscensionGameType>());
            }

            List<string> names = new List<string>();
            foreach (UiButton button in eventView.choiceButtons)
            {
                names.Add(button != null ? button.name : "destroyed");
            }

            return string.Join(", ", names) + ", state " + LegacyUiReader.AscensionState(
                Object.FindAnyObjectByType<AscensionGameType>());
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
