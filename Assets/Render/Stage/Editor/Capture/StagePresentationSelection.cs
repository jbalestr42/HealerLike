using System;
using System.Collections;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Hover uses explicit Unity messages; selection and clearing use held touch through the real physics path.
    public class StagePresentationSelection
    {
        readonly StageCaptureSession _session;
        readonly StagePresentationOutput _output;

        public StagePresentationSelection(StageCaptureSession session, StagePresentationOutput output)
        {
            _session = session;
            _output = output;
        }

        public IEnumerator Observe(GameObject entity, string subject)
        {
            CreatureBuilder host = entity.GetComponentInChildren<CreatureBuilder>();
            SelectableEntity source = entity.GetComponent<SelectableEntity>();
            _output.Check(host != null && host.rig != null && source != null,
                subject + " live gameplay entity owns its selection source and creature host");
            _output.Check(host.presentation != null && !host.presentation.IsChildOf(entity.transform),
                subject + " generated presentation is outside legacy selection descendants");
            _output.Check(!host.rig.isAppearing && _session.interaction.GetInteraction() == null
                && StageMapReadout.Selected(_session.interaction) == null,
                subject + " selection proof begins after placement with idle gameplay selection");
            _output.manifest.interventions.Add(subject + " hover uses explicit OnMouseEnter/OnMouseExit messages "
                + "on the real SelectableEntity after Start; OS mouse hover is not covered. Selection and "
                + "deselection use held legacy touch samples, live physics hits and StageTouchInput.Update. "
                + "After production growth completes, simulation time is temporarily frozen to isolate selection "
                + "from changing cooldown glow/health; real frames and touch processing continue.");
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                source.SendMessage("OnMouseExit", SendMessageOptions.RequireReceiver);
                yield return null;
                yield return null;
                _output.Check(!source.isHighlighted, subject + " settled idle baseline is unhighlighted");
                StageSelectionObservation observation = new StageSelectionObservation(host, source, _output);
                yield return Capture(observation.Sample(subject, "idle", false));
                source.SendMessage("OnMouseEnter", SendMessageOptions.RequireReceiver);
                yield return null;
                yield return Capture(observation.Sample(subject, "hovered", true));
                source.SendMessage("OnMouseExit", SendMessageOptions.RequireReceiver);
                yield return null;
                yield return Capture(observation.Sample(subject, "hover-exited", false));

                Vector2 selected = EntityPoint(entity, source, out RaycastHit entityHit);
                yield return _session.actions.TouchGesture(selected);
                yield return null;
                _output.Check(ReferenceEquals(StageMapReadout.Selected(_session.interaction), source),
                    subject + " held world touch selected the actual gameplay entity");
                yield return Capture(observation.Sample(subject, "selected", true, selected,
                    entityHit.collider.name));

                Vector2 empty = EmptyPoint(out RaycastHit emptyHit);
                yield return _session.actions.TouchGesture(empty);
                yield return null;
                _output.Check(StageMapReadout.Selected(_session.interaction) == null,
                    subject + " held touch on an empty board cell deselected through gameplay");
                yield return Capture(observation.Sample(subject, "deselected", false, empty,
                    emptyHit.collider.name));
                _output.Check(_session.interaction.enabled, subject + " touch selection restores the mouse adapter");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                if (_session.interaction != null
                    && ReferenceEquals(StageMapReadout.Selected(_session.interaction), source))
                {
                    _session.interaction.CancelSelection();
                }

                if (source != null)
                {
                    source.SendMessage("OnMouseExit", SendMessageOptions.RequireReceiver);
                }
            }
        }

        IEnumerator Capture(StageSelectionObservation.Frame frame)
        {
            yield return _session.Capture(frame.file.Substring(0, frame.file.Length - 4));
        }

        Vector2 EntityPoint(GameObject entity, SelectableEntity source, out RaycastHit hit)
        {
            Physics.SyncTransforms();
            foreach (Collider collider in entity.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                for (int i = 0; i < 9; i++)
                {
                    Vector3 world = bounds.center;
                    if (i > 0)
                    {
                        world += Vector3.Scale(bounds.extents * 0.7f, RenderMath.CornerSign(i - 1));
                    }

                    if (TryPoint(world, out Vector2 point, out hit)
                        && ReferenceEquals(hit.collider.GetComponentInParent<ISelectable>(), source))
                    {
                        return point;
                    }
                }
            }

            throw new InvalidOperationException("No unobstructed live creature collider point in the Game viewport.");
        }

        Vector2 EmptyPoint(out RaycastHit hit)
        {
            Physics.SyncTransforms();
            GridManager grid = _session.manager.player.grid;
            foreach (GridCell cell in grid.cells)
            {
                if (!cell.walkable || !TryPoint(cell.center, out Vector2 point, out hit))
                {
                    continue;
                }

                Vector2Int coordinate = grid.GetCoordFromPosition(hit.point);
                if (hit.collider.GetComponentInParent<ISelectable>() == null
                    && hit.collider.gameObject.layer == Layers.Terrain
                    && grid.IsWalkable(coordinate.x, coordinate.y))
                {
                    return point;
                }
            }

            throw new InvalidOperationException("No unobstructed empty walkable board cell in the Game viewport.");
        }

        bool TryPoint(Vector3 world, out Vector2 point, out RaycastHit hit)
        {
            Camera camera = _session.manager.gameCamera;
            Vector3 screen = camera.WorldToScreenPoint(world);
            point = screen;
            hit = default;
            Vector2 viewport = new Vector2(screen.x / Screen.width, screen.y / Screen.height);
            return screen.z > 0f && _session.actions.ui.normalizedWorldViewport.Contains(viewport)
                && !_session.actions.touch.IsOverInterface(point)
                && Physics.Raycast(camera.ScreenPointToRay(point), out hit, Mathf.Infinity);
        }
    }
}
