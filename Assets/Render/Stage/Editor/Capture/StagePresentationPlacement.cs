using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using HealerLike.Render.Creatures;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    public class StagePresentationPlacement
    {
        readonly StageCaptureSession _session;
        readonly StagePresentationOutput _output;
        readonly StagePresentationGrowth _growth;
        readonly StageCompactGestures _gestures;
        EntityData _selectedData;
        public EntityData selectedData
        {
            get
            {
                return _selectedData;
            }
        }

        public StagePresentationPlacement(StageCaptureSession session, StagePresentationOutput output,
            StagePresentationGrowth growth)
        {
            _session = session;
            _output = output;
            _growth = growth;
            _gestures = new StageCompactGestures(session);
        }

        // Starts a roster drag from deployable card index and leaves the finger down over cell. A tap on the card only
        // opens its details, so a placement preview exists only while the drag owner holds the finger.
        public IEnumerator Grab(StagePresentationTouch finger, int index, Vector3 cell)
        {
            yield return Wait(0.15f);
            List<Button> cards = StageRosterCards.Deployable(_session.actions);
            _output.Check(cards.Count > index, "Requested deploy card exists: " + index);
            yield return _session.actions.BringIntoView(cards[index]);
            yield return _gestures.Pull(finger, StageInterfaceActions.ScreenPoint(cards[index]),
                _gestures.DropPoint(cell));
            yield return null;
            _output.Check(_session.interaction.GetInteraction() is EntityGridInteraction,
                "Held roster drag starts the real grid interaction");
            _output.Check(_session.manager.placement.preview != null
                && _session.manager.placement.preview.rig != null,
                "Grid interaction has its generated cosmetic preview");
            _selectedData = _session.manager.placement.data;
            _output.Check(_selectedData != null, "Placement preview resolves selected EntityData");
            _output.Check(_session.manager.placement.legacyModel != null,
                "Original interaction model remains owned by gameplay");
            foreach (Renderer renderer
                in _session.manager.placement.legacyModel.GetComponentsInChildren<Renderer>(true))
            {
                _output.Check(!renderer.enabled || renderer.forceRenderingOff
                    || !renderer.gameObject.activeInHierarchy, "Original placement model renderer is invisible: "
                    + renderer.name);
            }
        }

        public IEnumerator Placement()
        {
            int entities = EntityCount();
            string grid = GridSnapshot();
            Vector3 point = _session.manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2f);
            CreaturePreview preview;
            Transform firstRoot;
            using (StagePresentationTouch finger = new StagePresentationTouch(_session.actions))
            {
                yield return Grab(finger, 0, point);
                preview = _session.manager.placement.preview;
                firstRoot = preview.rig.root;
                _output.Check(Vector3.Distance(preview.rig.root.position, point) < 0.05f,
                    "Held roster drag positions generated preview before release");
                _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                    "Preview creates no gameplay entity and changes no grid occupancy");
                yield return Wait(CreatureAppearance.Duration + 0.15f);
                _output.Check(!preview.rig.isAppearing,
                    "Placement appearance reaches its grown pose while finger remains held");
                float elapsed = preview.rig.appearanceElapsed;
                Vector3 moved = _session.manager.player.grid.GetNearestWalkablePosition(point + Vector3.forward * 2f);
                yield return finger.Frame(TouchPhase.Moved, _gestures.DropPoint(moved));
                yield return finger.Frame(TouchPhase.Stationary, _gestures.DropPoint(moved));
                _output.Check(ReferenceEquals(preview, _session.manager.placement.preview)
                    && preview.rig.appearanceElapsed >= elapsed && !preview.rig.isAppearing,
                    "Moving placement reuses the grown rig without replaying appearance");
                _output.Check(Vector3.Distance(preview.rig.root.position, moved) < 0.05f,
                    "Generated preview follows moved held touch");
                _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                    "Moving cosmetic preview still leaves entity count and occupancy unchanged");
                yield return _session.Capture("03-held-placement");
                yield return finger.Frame(TouchPhase.Canceled, _gestures.DropPoint(moved));
            }

            // The cancel button needs a second finger while the drag is down, so the input system's own
            // cancellation ends this placement
            yield return null;
            yield return Wait(0.2f);
            _output.Check(_session.interaction.GetInteraction() == null
                && _session.manager.placement.preview == null && firstRoot == null,
                "Cancelled drag destroys generated preview and ends interaction");
            _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                "Cancelled placement leaves gameplay unchanged");
            // One finger owns one drag, so switching creature is a cancelled drag of the first and a drag of the second
            CreaturePreview replaced;
            Transform replacedRoot;
            EntityData previous;
            using (StagePresentationTouch first = new StagePresentationTouch(_session.actions))
            {
                yield return Grab(first, 0, point);
                replaced = _session.manager.placement.preview;
                replacedRoot = replaced.rig.root;
                previous = _selectedData;
                yield return first.Frame(TouchPhase.Canceled, _gestures.DropPoint(point));
            }

            yield return Wait(0.2f);
            CreatureRig grown;
            using (StagePresentationTouch second = new StagePresentationTouch(_session.actions))
            {
                yield return Grab(second, 1, point);
                yield return null;
                _output.Check(_selectedData != previous && !ReferenceEquals(replaced,
                    _session.manager.placement.preview) && replacedRoot == null,
                    "Dragging another creature disposes the former preview");
                _output.Check(EntityCount() == entities && GridSnapshot() == grid, "Selection switch is cosmetic only");
                yield return Wait(CreatureAppearance.Duration + 0.1f);
                grown = _session.manager.placement.preview.rig;
                yield return LivePlacement(second, point, grown, entities);
            }

            yield return null;
            _output.Check(_session.interaction.enabled, "Legacy mouse adapter restored after held touch ends");
        }

        // The finger is the caller's, already down over the cell, and is released here
        public IEnumerator LivePlacement(StagePresentationTouch finger, Vector3 point, CreatureRig grown, int entities)
        {
            BattleFocus focus = _session.manager.GetComponentInChildren<BattleFocus>();
            bool wasEnabled = focus.enabled;
            Camera camera = _session.manager.gameCamera;
            Pose previous = new Pose(camera.transform.position, camera.transform.rotation);
            try
            {
                focus.enabled = false;
                _growth.Fit(grown, point - grown.root.position);
                _output.manifest.interventions.Add("Plant appearance camera fitted once to the fully grown "
                    + "placement bounds; live creature spawned by the real held touch release");
                yield return null;
                // The fit moved the camera under the held finger, so the finger is re-aimed at the cell before it lifts.
                Vector2 aim = _gestures.DropPoint(point);
                yield return finger.Frame(TouchPhase.Moved, aim);
                yield return finger.Frame(TouchPhase.Stationary, aim);
                yield return Wait(0.16f);
                yield return finger.Frame(TouchPhase.Ended, aim);
                _output.Check(EntityCount() == entities + 1,
                    "The held roster drag places exactly one gameplay entity on release");
                List<GameObject> allies = _session.manager.entityManager.GetEntities(Entity.EntityType.Player);
                GameObject placed = allies[allies.Count - 1];
                yield return _growth.Appearance(placed, "plant", AssetDatabase.GetAssetPath(_selectedData));
                StagePresentationSelection selection = new StagePresentationSelection(_session, _output);
                yield return selection.Observe(placed, "plant");
                _output.Check(_session.manager.placement.preview == null
                    && _session.interaction.GetInteraction() == null,
                    "Successful deployment releases cosmetic preview and grid interaction");
            }
            finally
            {
                camera.transform.SetPositionAndRotation(previous.position, previous.rotation);
                focus.enabled = wasEnabled;
            }
        }

        public int EntityCount()
        {
            return Object.FindObjectsByType<Entity>().Length;
        }

        public string GridSnapshot()
        {
            GridManager grid = _session.manager.player.grid;
            char[] cells = new char[grid.width * grid.height];
            for (int y = 0; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    cells[y * grid.width + x] = grid.IsWalkable(x, y) ? '1' : '0';
                }
            }

            return new string (cells);
        }
    }
}
