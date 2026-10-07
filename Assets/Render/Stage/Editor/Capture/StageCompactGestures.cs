using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    public sealed class StageCompactGestures
    {
        readonly StageCaptureSession _session;
        public StageCompactGestures(StageCaptureSession session) { _session = session; }
        public IEnumerator Hold(Button button, string image, bool persistent)
        {
            yield return _session.actions.BringIntoView(button);
            Vector2 point = StageInterfaceActions.ScreenPoint(button);
            Vector3 camera = _session.manager.gameCamera.transform.position;
            Rect viewport = _session.actions.ui.normalizedWorldViewport;
            using (StagePresentationTouch touch = new StagePresentationTouch(_session.actions))
            {
                yield return touch.Frame(TouchPhase.Began, point);
                yield return Still(touch, point, 0.55f);
                _session.output.Check(StageInterfaceOutput.IsVisible(_session.actions.root.Q("detail-panel")),
                    "Native held pointer opens details: " + image);
                _session.output.Check(viewport == _session.actions.ui.normalizedWorldViewport
                    && Vector3.Distance(camera, _session.manager.gameCamera.transform.position) < 0.01f,
                    "Inspection leaves viewport and camera stable: " + image);
                yield return _session.Capture(image, "Multi-frame StandaloneInputModule touch hold, real Toolkit pointer callbacks");
                yield return touch.Frame(TouchPhase.Ended, point);
                yield return touch.Frame(TouchPhase.Ended, point); // duplicate release must be harmless
            }
            yield return Wait(0.2f);
            _session.output.Check(StageInterfaceOutput.IsVisible(_session.actions.root.Q("detail-panel")) == persistent,
                "Correct popover lifetime after release: " + image);
        }
        public static IEnumerator Still(StagePresentationTouch touch, Vector2 point, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            do { yield return touch.Frame(TouchPhase.Stationary, point); } while (Time.realtimeSinceStartup < end);
        }
        public Vector2 DropPoint(Vector3 world)
        {
            Vector2 projected = _session.manager.gameCamera.WorldToScreenPoint(world);
            return projected - Vector2.up * (56 * ToolkitScreenLayout.GetScale(Screen.width, Screen.height, false));
        }
        public IEnumerator Drag(Button button, Vector2 destination, string image, TouchPhase release = TouchPhase.Ended)
        {
            yield return _session.actions.BringIntoView(button);
            Vector2 start = StageInterfaceActions.ScreenPoint(button);
            using (StagePresentationTouch touch = new StagePresentationTouch(_session.actions))
            {
                yield return touch.Frame(TouchPhase.Began, start);
                yield return touch.Frame(TouchPhase.Moved, start + Vector2.up * 48);
                yield return touch.Frame(TouchPhase.Moved, destination);
                yield return Still(touch, destination, 0.25f);
                if (image != null)
                {
                    yield return _session.Capture(image, "Owned roster drag through actual Toolkit pointer events");
                }

                StageRosterDrag roster = _session.actions.touch.roster;
                bool legal = release == TouchPhase.Ended && roster.valid && !_session.actions.touch.IsOverInterface(destination);
                Vector3 intended = roster.target;
                int before = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
                yield return touch.Frame(release, destination);
                yield return touch.Frame(release, destination);
                if (legal)
                {
                    var entities = _session.manager.entityManager.GetEntities(Entity.EntityType.Player);
                    _session.output.Check(entities.Count == before + 1
                        && Vector3.Distance(entities[entities.Count - 1].transform.position, intended) < 0.01f,
                        "Valid release creates one actual creature at the highlighted legal cell");
                }
            }
            yield return Wait(0.25f);
        }
        public IEnumerator Scroll(Button button)
        {
            yield return _session.actions.BringIntoView(button);
            Vector2 start = StageInterfaceActions.ScreenPoint(button);
            using (StagePresentationTouch touch = new StagePresentationTouch(_session.actions))
            {
                yield return touch.Frame(TouchPhase.Began, start);
                yield return touch.Frame(TouchPhase.Moved, start + Vector2.left * 80);
                yield return touch.Frame(TouchPhase.Moved, start + Vector2.left * 140 + Vector2.up * 200);
                yield return touch.Frame(TouchPhase.Ended, start + Vector2.left * 140 + Vector2.up * 200);
            }
            yield return Wait(0.2f);
        }
    }
}
