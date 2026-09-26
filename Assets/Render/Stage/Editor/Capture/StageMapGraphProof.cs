using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using BattleEvidence = HealerLike.Render.Stage.StageMapRun.BattleEvidence;
using SpellEvidence = HealerLike.Render.Stage.StageMapRun.SpellEvidence;
using MapFrame = HealerLike.Render.Stage.StageMapRun.MapFrame;
using Room = HealerLike.Render.Stage.StageMapRun.Room;

namespace HealerLike.Render.Stage
{
    public class StageMapGraphProof
    {
        readonly StageMapSession _session;
        public StageMapGraphProof(StageMapSession session)
        {
            _session = session;
        }

        public IEnumerator Map(string name, string scenario, bool canTravel)
        {
            yield return Wait(0.25f);
            // Retain the actual frame even when a later geometry or ownership assertion fails.
            yield return _session.Capture(name);
            VisualElement dialog = _session.actions.root.Q("map-dialog");
            ScrollView scroll = _session.actions.root.Q<ScrollView>("map-scroll");
            VisualElement canvas = _session.actions.root.Q("map-canvas");
            _session.output.Check(StageInterfaceOutput.IsVisible(dialog) && scroll != null && canvas != null,
                "Toolkit graph and map dialog are visible: " + name);
            _session.output.Check(Contains(_session.actions.root.worldBound, dialog.worldBound),
                "Map dialog remains within screen: " + name);
            _session.output.Check(
                LegacyUiReader.CurrentView(Object.FindAnyObjectByType<UIManager>()) == ViewType.Map,
                    "Map is backed by the original UI view stack: " + name);
            // The authored MapView owns MapCanvas as a child. ToolkitLegacyCanvases suppresses
            // screen canvases below UIManager, including inactive views before they are opened.
            UIManager owner = Object.FindAnyObjectByType<UIManager>();
            int screenCanvases = 0;
            int raycasters = 0;
            foreach (Canvas legacy in _session.mapView.GetComponentsInChildren<Canvas>(true))
            {
                if (legacy.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                screenCanvases++;
                _session.output.Check(legacy.transform.IsChildOf(owner.transform) && !legacy.enabled,
                    "Original map screen canvas belongs to UIManager and stays disabled: " + legacy.name);
                foreach (UnityEngine.UI.GraphicRaycaster raycaster
                    in legacy.GetComponents<UnityEngine.UI.GraphicRaycaster>())
                {
                    raycasters++;
                    _session.output.Check(!raycaster.enabled, "Original map input raycaster stays disabled: "
                        + raycaster.name);
                }
            }

            _session.output.Check(screenCanvases > 0 && raycasters > 0,
                "Authored map supplies screen canvas and input raycaster descendants for suppression checks");
            RunState run = _session.ascension.run;
            MapFrame frame = new MapFrame
            {
                name = name,
                scenario = scenario,
                width = Screen.width,
                height = Screen.height,
                dialog = dialog.worldBound,
                viewport = scroll.contentViewport.worldBound,
                graph = canvas.worldBound,
                currentFloor = run.currentFloor,
                visited = run.visitedNodes.Count
            };
            int count = 0;
            foreach (MapNode node in run.map.GetAllNodes())
            {
                Button button = StageMapActions.ButtonFor(_session.actions, node);
                MapNodeState state = run.GetNodeState(node);
                _session.output.Check(button != null && ReferenceEquals(button.userData, node),
                    "Room button retains original gameplay node identity: " + node);
                _session.output.Check(button.enabledInHierarchy == (canTravel && run.CanTravelTo(node)),
                    "Room availability matches live RunState: " + node);
                _session.output.Check(button.ClassListContains("is-" + state.ToString().ToLowerInvariant()),
                    "Room style matches live visit state: " + node);
                _session.output.Check(Contains(canvas.worldBound, button.worldBound),
                    "Room button remains within scrollable graph: " + node);
                _session.output.Check(button.worldBound.width >= 44f && button.worldBound.height >= 44f,
                    "Room touch target is at least 44 panel units: " + node);
                frame.rooms.Add(new Room { floor = node.floor, column = node.column, type = node.type.ToString(),
                    state = state.ToString(), enabled = button.enabledInHierarchy, bounds = button.worldBound });
                frame.expectedEdges += node.next.Count;
                count++;
            }

            _session.output.Check(canvas.Query<Button>(className: "map-node").ToList().Count == count,
                "Map renders exactly one button per live room");
            ToolkitMapConnections connections
                = canvas.Q<VisualElement>("map-connections").userData as ToolkitMapConnections;
            _session.output.Check(connections != null && connections.edgeCount == frame.expectedEdges,
                "Visible path painter carries every live directed map connection");
            frame.drawnEdges = connections.edgeCount;
            _session.manifest.maps.Add(frame);
        }

        public IEnumerator LockedRoom()
        {
            MapNode locked = _session.ascension.run.map.floors[1][0];
            Button button = StageMapActions.ButtonFor(_session.actions, locked);
            yield return _session.actions.BringIntoView(button);
            _session.output.Check(!button.enabledInHierarchy, "Unreachable room is disabled");
            yield return _session.actions.TouchGesture(StageInterfaceActions.ScreenPoint(button));
            yield return Wait(0.2f);
            _session.output.Check(_session.ascension.run.currentNode == null
                && _session.ascension.run.visitedNodes.Count == 0 && _session.manifest.roomSelections == 0,
                "Touching a locked room cannot travel or dispatch a room selection");
        }

        public IEnumerator SelectRoom(MapNodeType expected)
        {
            RunState run = _session.ascension.run;
            int visited = run.visitedNodes.Count;
            int events = _session.manifest.roomSelections;
            _session.output.Check(run.GetAvailableNodes().Count > 0 && run.GetAvailableNodes()[0].type == expected,
                "Next capture room is " + expected);
            yield return StageMapActions.SelectFirst(_session.actions, true);
            _session.output.Check(run.currentNode.type == expected && run.visitedNodes.Count == visited + 1
                && _session.manifest.roomSelections == events + 1, "One eligible touch selects exactly one "
                + expected + " room through original MapView event");
        }

        public IEnumerator PlanningMap()
        {
            int visited = _session.ascension.run.visitedNodes.Count;
            int events = _session.manifest.roomSelections;
            yield return _session.actions.PointerTap("map-button");
            yield return Wait(0.3f);
            yield return Map("planning-map-" + _session.manifest.roundsStarted + "-" + Screen.width, "inspect-only",
                false);
            Button current = StageMapActions.ButtonFor(_session.actions, _session.ascension.run.currentNode);
            yield return _session.actions.BringIntoView(current);
            InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
            ISelectable selected = StageMapReadout.Selected(interaction);
            int entities = Object.FindObjectsByType<Entity>().Length;
            yield return _session.actions.TouchGesture(StageInterfaceActions.ScreenPoint(current));
            _session.output.Check(_session.ascension.run.visitedNodes.Count == visited
                && _session.manifest.roomSelections == events, "Inspect-only map touch cannot advance travel");
            _session.output.Check(Object.FindObjectsByType<Entity>().Length == entities
                && interaction.GetInteraction() == null,
                "Map-origin input neither deploys nor begins a battlefield interaction");
            _session.output.Check(ReferenceEquals(StageMapReadout.Selected(interaction), selected),
                "Map-origin input preserves the actual battlefield selection");
            yield return _session.actions.PointerTap("map-close-button");
            yield return Wait(0.3f);
            _session.output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("map-panel"))
                && LegacyUiReader.AscensionState(_session.ascension) == AscensionGameType.State.WaitForRoundToStart,
                "Map Back returns to the same combat preparation");
        }

        public IEnumerator PlacementBlocksMap()
        {
            yield return _session.actions.PointerTap("party-button");
            yield return Wait(0.2f);
            yield return _session.actions.SelectCardByTouch(_session.actions.Cards("party-list")[0]);
            yield return Wait(0.2f);
            InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
            _session.output.Check(interaction.GetInteraction() is EntityGridInteraction
                && _session.manager.placement.preview != null,
                "Party touch begins creature placement with the generated preview");
            Button map = _session.actions.root.Q<Button>("map-button");
            _session.output.Check(!map.enabledInHierarchy, "Map inspection is disabled during active placement");
            ISelectable selected = StageMapReadout.Selected(interaction);
            int entities = Object.FindObjectsByType<Entity>().Length;
            yield return _session.actions.TouchGesture(StageInterfaceActions.ScreenPoint(map));
            _session.output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("map-panel"))
                && Object.FindObjectsByType<Entity>().Length == entities,
                "Touching unavailable Map neither opens a modal nor deploys through the HUD");
            _session.output.Check(ReferenceEquals(StageMapReadout.Selected(interaction), selected),
                "Touching unavailable Map preserves the actual battlefield selection");
            yield return _session.actions.PointerTap("cancel-button");
            yield return Wait(0.2f);
            _session.output.Check(interaction.GetInteraction() == null && _session.manager.placement.preview == null,
                "Cancel releases creature placement before map inspection");
        }

        static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - 1f && inner.yMin >= outer.yMin - 1f && inner.xMax <= outer.xMax + 1f
                && inner.yMax <= outer.yMax + 1f;
        }
    }
}
