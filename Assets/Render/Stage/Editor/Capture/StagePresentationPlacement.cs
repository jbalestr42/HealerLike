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
        }

        public IEnumerator Select(int index)
        {
            if (!StageInterfaceOutput.IsVisible(_session.actions.root.Q("party-panel")))
            {
                yield return _session.actions.PointerTap("party-button");
            }

            yield return Wait(0.15f);
            List<Button> cards = _session.actions.Cards("party-list").FindAll(card => card.enabledInHierarchy
                && card.Q<Label>("card-status").text == "Deploy");
            _output.Check(cards.Count > index, "Requested deploy card exists: " + index);
            yield return _session.actions.SelectCardByTouch(cards[index]);
            yield return null;
            _output.Check(_session.interaction.GetInteraction() is EntityGridInteraction,
                "Toolkit touch starts the real grid interaction");
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
            yield return Select(0);
            CreaturePreview preview = _session.manager.placement.preview;
            Transform firstRoot = preview.rig.root;
            Vector3 point = _session.manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2f);
            using (StagePresentationTouch finger = new StagePresentationTouch(_session.actions))
            {
                yield return finger.Frame(TouchPhase.Began, _session.manager.gameCamera.WorldToScreenPoint(point));
                yield return finger.Frame(TouchPhase.Stationary, _session.manager.gameCamera.WorldToScreenPoint(point));
                _output.Check(Vector3.Distance(preview.rig.root.position, point) < 0.05f,
                    "Held first board touch positions generated preview before release");
                _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                    "Preview creates no gameplay entity and changes no grid occupancy");
                yield return Wait(CreatureAppearance.Duration + 0.15f);
                _output.Check(!preview.rig.isAppearing,
                    "Placement appearance reaches its grown pose while finger remains held");
                float elapsed = preview.rig.appearanceElapsed;
                Vector3 moved = _session.manager.player.grid.GetNearestWalkablePosition(point + Vector3.forward * 2f);
                yield return finger.Frame(TouchPhase.Moved, _session.manager.gameCamera.WorldToScreenPoint(moved));
                yield return finger.Frame(TouchPhase.Stationary, _session.manager.gameCamera.WorldToScreenPoint(moved));
                _output.Check(ReferenceEquals(preview, _session.manager.placement.preview)
                    && preview.rig.appearanceElapsed >= elapsed && !preview.rig.isAppearing,
                    "Moving placement reuses the grown rig without replaying appearance");
                _output.Check(Vector3.Distance(preview.rig.root.position, moved) < 0.05f,
                    "Generated preview follows moved held touch");
                _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                    "Moving cosmetic preview still leaves entity count and occupancy unchanged");
                yield return _session.Capture("03-held-placement");
                yield return finger.Frame(TouchPhase.Canceled, _session.manager.gameCamera.WorldToScreenPoint(moved));
            }

            yield return null;
            yield return _session.actions.PointerTap("cancel-button");
            yield return Wait(0.2f);
            _output.Check(_session.interaction.GetInteraction() == null
                && _session.manager.placement.preview == null && firstRoot == null,
                "Cancel destroys generated preview and ends interaction");
            _output.Check(EntityCount() == entities && GridSnapshot() == grid,
                "Cancelled placement leaves gameplay unchanged");
            yield return Select(0);
            CreaturePreview replaced = _session.manager.placement.preview;
            Transform replacedRoot = replaced.rig.root;
            EntityData previous = _selectedData;
            yield return Select(1);
            yield return null;
            _output.Check(_selectedData != previous && !ReferenceEquals(replaced,
                _session.manager.placement.preview) && replacedRoot == null,
                "Selecting another creature disposes the former preview");
            _output.Check(EntityCount() == entities && GridSnapshot() == grid, "Selection switch is cosmetic only");
            point = _session.manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2f);
            yield return Wait(CreatureAppearance.Duration + 0.1f);
            CreatureRig grown = _session.manager.placement.preview.rig;
            yield return LivePlacement(point, grown, entities);
            yield return null;
            _output.Check(_session.interaction.enabled, "Legacy mouse adapter restored after held touch ends");
        }

        public IEnumerator LivePlacement(Vector3 point, CreatureRig grown, int entities)
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
                // The camera is fixed before the gesture starts, so the tap threshold measures finger motion only.
                using (StagePresentationTouch finger = new StagePresentationTouch(_session.actions))
                {
                    Vector2 screen = camera.WorldToScreenPoint(point);
                    yield return finger.Frame(TouchPhase.Began, screen);
                    yield return finger.Frame(TouchPhase.Stationary, screen);
                    yield return Wait(0.16f);
                    yield return finger.Frame(TouchPhase.Ended, screen);
                }

                _output.Check(EntityCount() == entities + 1,
                    "First held world touch places exactly one gameplay entity");
                List<GameObject> allies = _session.manager.entityManager.GetEntities(Entity.EntityType.Player);
                GameObject placed = allies[allies.Count - 1];
                yield return _growth.Appearance(placed, "plant", AssetDatabase.GetAssetPath(_selectedData));
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
