using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // The event and rest choices of the run, drawn by ToolkitEventPanel as cards. A choice is taken by the same
    // multi-frame touch as every Toolkit button; nothing picks a choice of the run directly from here.
    public sealed class StageEventChoice
    {
        readonly StageCaptureSession _session;
        readonly StageEventRoomRun.Manifest _manifest;
        public StageEventChoice(StageCaptureSession session, StageEventRoomRun.Manifest manifest)
        {
            _session = session;
            _manifest = manifest;
        }

        static AscensionGameType ascension
        {
            get { return Object.FindAnyObjectByType<AscensionGameType>(); }
        }

        // The event on screen, null when none is
        public EventPrompt prompt
        {
            get { return ascension != null ? ascension.currentEvent : null; }
        }

        public IEnumerator WaitForChoices(string title)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!IsShowing(title))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new InvalidOperationException("The choice screen never showed " + title);
                }

                yield return null;
            }

            // The panel redraws on the HUD's 0.1 second refresh, one card per choice
            float cardDeadline = Time.realtimeSinceStartup + 2f;
            while (_session.actions.Cards("event-list").Count != prompt.choices.Count
                && Time.realtimeSinceStartup < cardDeadline)
            {
                yield return null;
            }

            yield return AStageRun.Wait(0.3f);
        }

        bool IsShowing(string title)
        {
            EventPrompt current = prompt;
            return current != null && current.choices.Count > 0 && (title == null || current.title == title);
        }

        public EventChoice Choice(string label)
        {
            foreach (EventChoice choice in prompt.choices)
            {
                if (choice.label == label)
                {
                    return choice;
                }
            }

            throw new InvalidOperationException("No choice named " + label + " on the choice screen");
        }

        public bool isToolkitPanelDrawing
        {
            get { return StageInterfaceOutput.IsVisible(_session.actions.root.Q("event-panel")); }
        }

        // The Toolkit card drawn for the choice of that label, null when there is none
        public UnityEngine.UIElements.Button Card(string label)
        {
            foreach (UnityEngine.UIElements.Button card in _session.actions.Cards("event-list"))
            {
                Label title = card.Q<Label>("card-title");
                if (title != null && title.text == label)
                {
                    return card;
                }
            }

            return null;
        }

        // One card per choice, each enabled exactly when its choice is available, with a description our label can draw
        public void CheckCards(string room)
        {
            IReadOnlyList<EventChoice> choices = prompt.choices;
            int cardCount = _session.actions.Cards("event-list").Count;
            _session.output.Check(cardCount == choices.Count, room + ": the Toolkit panel draws one card per choice ("
                + cardCount + " cards, " + choices.Count + " choices)");
            foreach (EventChoice choice in choices)
            {
                UnityEngine.UIElements.Button card = Card(choice.label);
                _session.output.Check(card != null && card.enabledInHierarchy == choice.isAvailable,
                    room + ": the card of " + choice.label + " exists and is enabled exactly when the choice is available");
                if (card == null)
                {
                    continue;
                }

                string description = card.Q<Label>("card-description").text;
                string status = card.Q<Label>("card-status").text;
                _manifest.cards.Add(room + " " + choice.label + " [" + status + "]: " + description.Replace("\n", " / "));
                _session.output.Check(!description.Contains("<size") && !description.Contains("<b>"),
                    room + ": the card of " + choice.label + " carries no raw tag (" + description + ")");
            }
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

        // Before the choice is taken: our HUD is still up with its buttons off, a touch on it changes nothing on the
        // choice screen, and the event panel covers the spell and unit cards
        public IEnumerator CheckSeam(string room, EventChoice target)
        {
            VisualElement root = _session.actions.root;
            _session.output.Check(StageInterfaceOutput.IsVisible(root.Q("hud-root")),
                room + ": the Toolkit HUD stays visible under the choice screen");
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

            _manifest.eventSurfaces.Add(room + ": " + (isToolkitPanelDrawing ? "Toolkit event panel" : "nothing"));
            _session.output.Check(isToolkitPanelDrawing, room + ": the Toolkit event panel draws the choices");
            UnityEngine.UIElements.Button card = Card(target.label);
            _session.output.Check(card != null && StageInterfaceOutput.IsVisible(card),
                room + ": the card of " + target.label + " is visible");

            EventPrompt before = prompt;
            AscensionGameType.State state = ascension.state;
            Vector2 mapPoint = StageInterfaceActions.ScreenPoint(map);
            _manifest.hudHits.Add(room + " map-button at " + mapPoint + ": " + Describe(Hits(mapPoint)));
            yield return _session.actions.TouchGesture(mapPoint);
            yield return AStageRun.Wait(0.2f);
            _session.output.Check(prompt == before && !before.hasChosen && ascension.state == state
                && !StageInterfaceOutput.IsVisible(root.Q("map-panel")),
                room + ": a touch on the disabled HUD map button leaves the choice screen and the run state as they were");

            // The event panel is a modal over the HUD: a touch at a spell or unit card lands on the panel
            VisualElement panel = root.Q("event-panel");
            UnityEngine.UIElements.Button spellCard = _session.actions.Cards("spell-list")[0];
            UnityEngine.UIElements.Button unitCard = _session.actions.Cards("party-list")[0];
            bool covered = panel.pickingMode == PickingMode.Position
                && panel.worldBound.Contains(spellCard.worldBound.center)
                && panel.worldBound.Contains(unitCard.worldBound.center);
            _manifest.cardEffects.Add(room + ": the Toolkit event panel " + (covered ? "covers" : "does not cover")
                + " the spell and unit cards, " + ScreenState());
            _session.output.Check(covered, room + ": the Toolkit event panel covers the spell and unit cards");
        }

        string ScreenState()
        {
            EventPrompt current = prompt;
            if (current == null)
            {
                return "choice screen closed, state " + ascension.state;
            }

            List<string> names = new List<string>();
            foreach (EventChoice choice in current.choices)
            {
                names.Add(choice.label);
            }

            return string.Join(", ", names) + ", state " + ascension.state;
        }

        // One real touch on the card of the choice, checked to pick it and to leave the battlefield untouched
        public IEnumerator Tap(EventChoice choice)
        {
            EventPrompt before = prompt;
            if (choice == null || !choice.isAvailable || before == null)
            {
                throw new InvalidOperationException("The choice is not available: " + (choice != null ? choice.label : "null"));
            }

            int allies = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            ISelectable selected = StageMapReadout.Selected(_session.interaction);
            UnityEngine.UIElements.Button card = Card(choice.label);
            if (card != null)
            {
                yield return _session.actions.BringIntoView(card);
            }

            Vector2 point = StageInterfaceActions.ScreenPoint(card);
            yield return _session.actions.TouchGesture(point);
            yield return AStageRun.Wait(0.2f);

            _manifest.clicks.Add(choice.label + " at " + point + ": picked " + before.hasChosen);
            _session.output.Check(before.hasChosen, "One touch on " + choice.label + " picked it");
            _session.output.Check(_session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == allies
                && StageMapReadout.Selected(_session.interaction) == selected
                && _session.interaction.GetInteraction() == null,
                "The touch on " + choice.label + " placed, selected or started nothing on the battlefield");
        }
    }
}
