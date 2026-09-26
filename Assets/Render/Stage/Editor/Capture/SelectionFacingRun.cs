using System;
using System.Collections;
using System.IO;
using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Main and Toolkit HUD, with real EntityManager spawns and production CreatureBuilder.LateUpdate.
    public class SelectionFacingRun : AStageRun
    {
        readonly StagePresentationOutput _output = new StagePresentationOutput();
        readonly SelectionFacingProof _proof = new SelectionFacingProof();
        StageCaptureSession _session;
        public static void Capture() => SpellSourcePreparation.Enter("selection-facing", 300f);

        protected override void OnFailed(Exception error)
        {
            _output.Fail(error.ToString());
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            BattleFocus focus = Object.FindAnyObjectByType<BattleFocus>();
            bool focusEnabled = focus && focus.enabled;
            Camera camera = _manager.gameCamera;
            Pose cameraBefore = new Pose(camera.transform.position, camera.transform.rotation);
            try
            {
                _session = new StageCaptureSession(_manager, _output.ui);
                _session.AttachInput();
                _manager.SetLandscape(false);
                yield return _session.Resize(1080, 1920);
                if (focus) focus.enabled = false;
                _output.manifest.interventions.Add(_proof.condition);
                EntityData data = RenderAssets.Load<EntityData>("Assets/Data/Entities/ChannelingEntity/ChannelingEntity.asset");
                foreach (Entity.EntityType side in new[] { Entity.EntityType.Player, Entity.EntityType.Computer })
                {
                    _output.Check(LookDerivation.Channels(data, side).head == HeadKind.Fork,
                        "Real ChannelingEntity derives an asymmetric Fork head");
                    string subject = side == Entity.EntityType.Player ? "plant" : "mineral";
                    Vector3 point = _manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2f);
                    GameObject entity = _manager.entityManager.SpawnEntity(data, point, side);
                    _output.Check(entity, "Real " + subject + " spawn succeeded");
                    yield return Wait(CreatureAppearance.Duration + 0.3f);
                    CreatureBuilder host = entity.GetComponentInChildren<CreatureBuilder>();
                    _output.Check(host && host.rig != null && !host.rig.isAppearing, "Production host fully grown");
                    // Fit a yaw-invariant envelope, so turning cannot clip the same-camera comparison.
                    Bounds bounds = new Bounds(host.rig.root.position, Vector3.zero);
                    foreach (Renderer renderer in host.rig.root.GetComponentsInChildren<Renderer>())
                        bounds.Encapsulate(renderer.bounds);
                    float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.05f;
                    bounds = new Bounds(new Vector3(point.x, bounds.center.y, point.z),
                        new Vector3(radius * 2f, bounds.size.y, radius * 2f));
                    bounds.Expand(0.5f);
                    Pose fit = StageViewport.Fit(bounds, camera.transform.rotation, camera.fieldOfView, camera.aspect,
                        StageViewport.Inset(_session.actions.ui.normalizedWorldViewport, 0.05f));
                    camera.transform.SetPositionAndRotation(fit.position, fit.rotation);
                    Time.timeScale = 0f;
                    yield return null;
                    yield return null;
                    yield return Selection(entity, host, subject);
                    using (var control = new SelectionFacingControl(entity.GetComponent<Entity>(), host, _output))
                    {
                        yield return Turns(control, subject);
                    }
                    _manager.entityManager.DestroyEntity(entity, side);
                    yield return null;
                }
                passed = true;
            }
            finally
            {
                camera.transform.SetPositionAndRotation(cameraBefore.position, cameraBefore.rotation);
                if (focus) focus.enabled = focusEnabled;
                _session?.Dispose();
                _output.Write(passed);
                _proof.passed = passed;
                string folder = Path.Combine(StagePlay.CaptureFolder, "creature-presentation");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "facing-proof.json"), JsonUtility.ToJson(_proof, true));
                StagePlay.Finish(this, passed);
            }
        }

        IEnumerator Selection(GameObject entity, CreatureBuilder host, string subject)
        {
            SelectableEntity source = entity.GetComponent<SelectableEntity>();
            source.SendMessage("OnMouseExit", SendMessageOptions.RequireReceiver);
            yield return null;
            yield return null;
            var observation = new StageSelectionObservation(host, source, _output);
            Vector3 ownerPosition = entity.transform.position;
            Quaternion ownerRotation = entity.transform.rotation;
            foreach (string state in new[] { "idle", "selected", "deselected" })
            {
                if (state == "selected") source.Select();
                if (state == "deselected") source.UnSelect();
                yield return null;
                yield return null;
                var frame = observation.Sample(subject, state, state == "selected");
                _output.Check(entity.transform.position.Equals(ownerPosition)
                    && entity.transform.rotation.Equals(ownerRotation), "Selection keeps gameplay owner pose exact");
                yield return _session.Capture(frame.file.Replace(".png", ""),
                    "Explicit SelectableEntity.Select/UnSelect calls; actual production selection readout");
            }
        }

        IEnumerator Turns(SelectionFacingControl control, string subject)
        {
            Vector3 cameraForward = -_manager.gameCamera.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            Vector3[] directions = { Quaternion.Euler(0f, 90f, 0f) * cameraForward,
                Quaternion.Euler(0f, -60f, 0f) * cameraForward };
            for (int turn = 0; turn < directions.Length; turn++)
            {
                control.Target(directions[turn], turn);
                float elapsed = 0f;
                foreach (float sample in new[] { 0f, 0.12f, 1f })
                {
                    Time.timeScale = 1f;
                    float start = Time.time;
                    while (Time.time - start < sample - elapsed) yield return null;
                    elapsed += Time.time - start;
                    Time.timeScale = 0f;
                    // Let production LateUpdate finish before reading the pose and held delivery root.
                    yield return null;
                    string file = "facing-" + subject + "-" + turn + "-" + sample.ToString("0.00",
                        System.Globalization.CultureInfo.InvariantCulture);
                    var frame = control.Sample(subject, turn, elapsed, sample > 0f ? file : null, _manager.gameCamera);
                    _proof.frames.Add(frame);
                    if (sample > 0f)
                        yield return _session.Capture(file, "Synthetic target provider control; production live LateUpdate; paused PNG");
                    if (sample == 1f) _output.Check(frame.targetAngle < 8f, subject + " visibly faces target " + turn);
                }
            }
            control.ClearTarget();
            Time.timeScale = 1f;
            float recovery = Time.time;
            while (Time.time - recovery < 1f) yield return null;
            Time.timeScale = 0f;
            yield return null;
            _proof.frames.Add(control.Sample(subject, -1, 1f, "facing-" + subject + "-rest", _manager.gameCamera));
            yield return _session.Capture("facing-" + subject + "-rest", "Target cleared; production camera-readable rest");
        }
    }
}
