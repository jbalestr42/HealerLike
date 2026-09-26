using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using HealerLike.Render.Creatures;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    public class StagePresentationGrowth
    {
        readonly StageCaptureSession _session;
        readonly StagePresentationOutput _output;
        public StagePresentationGrowth(StageCaptureSession session, StagePresentationOutput output)
        {
            _session = session;
            _output = output;
        }

        public IEnumerator StoneAppearance()
        {
            EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(StagePlayer.Allies[0]);
            Vector3 point = _session.manager.player.grid.GetNearestWalkablePosition(Vector3.right * 2f);
            GameObject measureRoot = null;
            BattleFocus focus = _session.manager.GetComponentInChildren<BattleFocus>();
            bool wasEnabled = focus.enabled;
            Camera camera = _session.manager.gameCamera;
            Pose previous = new Pose(camera.transform.position, camera.transform.rotation);
            try
            {
                focus.enabled = false;
                measureRoot = new GameObject("Stone capture framing");
                using (CreaturePreview measure = new CreaturePreview())
                {
                    _output.Check(measure.Init(_session.manager.creatureLooks, data, Entity.EntityType.Computer,
                        _session.manager.meshes, measureRoot.transform, StageCalibration.CellSize),
                        "Stone framing resolves real shared creature source");
                    measure.CompleteAppearance();
                    measure.Tick(Time.time, 0f, new FootFrame(point, Vector3.up, StageCalibration.CellSize),
                        -camera.transform.forward);
                    Fit(measure.rig);
                }

                Object.Destroy(measureRoot);
                yield return null;
                _output.manifest.interventions.Add("Stone spawned through EntityManager.SpawnEntity during planning "
                    + "because natural enemy startup precedes capture; camera fitted once using fully grown shared "
                    + "preview; production host owns all animation");
                GameObject stone = _session.manager.entityManager.SpawnEntity(data, point, Entity.EntityType.Computer);
                _output.Check(stone != null, "Controlled real enemy spawn succeeds on an open cell");
                // SpawnEntity assigns its final position after Init notifies render observers.
                yield return null;
                yield return Appearance(stone, "stone", AssetDatabase.GetAssetPath(data));
            }
            finally
            {
                if (measureRoot != null)
                {
                    Object.Destroy(measureRoot);
                }

                camera.transform.SetPositionAndRotation(previous.position, previous.rotation);
                focus.enabled = wasEnabled;
            }
        }

        public IEnumerator Appearance(GameObject entity, string subject, string source)
        {
            float started = Time.unscaledTime;
            CreatureBuilder host = entity.GetComponentInChildren<CreatureBuilder>();
            while (host == null || host.rig == null)
            {
                _output.Check(Time.unscaledTime - started < 3f, "Live spawn acquires creature host in time");
                yield return null;
                host = entity.GetComponentInChildren<CreatureBuilder>();
            }

            CreatureRig rig = host.rig;
            float[] samples =
            {
                0f,
                0.12f,
                0.25f,
                0.4f,
                0.6f,
                0.8f,
                1.1f
            };
            int next = 0;
            bool sawSequential = false;
            int partialFrames = 0;
            Pose camera = new Pose(_session.manager.gameCamera.transform.position,
                _session.manager.gameCamera.transform.rotation);
            while (Time.unscaledTime - started < 1.3f || rig.isAppearing)
            {
                float elapsed = Time.unscaledTime - started;
                _output.Check(elapsed < 4f, subject + " appearance completes within four real seconds");
                string file = null;
                if (next < samples.Length && elapsed >= samples[next])
                {
                    file = "growth-" + subject + "-" + next.ToString("00") + ".png";
                    // One screenshot per rendered frame, even if a slow frame crossed multiple thresholds.
                    do
                    {
                        next++;
                    }
                    while (next < samples.Length && elapsed >= samples[next]);
                }

                StagePresentationOutput.GrowthFrame frame = _output.Sample(rig, _session.manager.gameCamera, subject,
                    source, started, file);
                float min = float.MaxValue;
                float max = 0f;
                foreach (StagePresentationOutput.PartScale part in frame.parts)
                {
                    float ratio = part.scale.magnitude / Mathf.Max(0.0001f, part.authoredScale.magnitude);
                    min = Mathf.Min(min, ratio);
                    max = Mathf.Max(max, ratio);
                }

                if (rig.isAppearing)
                {
                    partialFrames++;
                }

                sawSequential |= rig.isAppearing && max - min > 0.2f;
                _output.Check(Vector3.Distance(camera.position,
                    _session.manager.gameCamera.transform.position) < 0.0001f && Quaternion.Angle(camera.rotation,
                    _session.manager.gameCamera.transform.rotation) < 0.001f, subject + " growth camera stays fixed");
                yield return null;
            }

            _output.Check(partialFrames >= 2 && sawSequential, subject
                + " production frames show sequential part scaling, with at least two partial frames");
            _output.Check(!rig.isAppearing && rig.appearanceElapsed >= CreatureAppearance.Duration, subject
                + " production appearance reaches exact timeline completion");
            _output.Sample(rig, _session.manager.gameCamera, subject, source, started, "growth-" + subject
                + "-grown.png");
            CheckVisible(rig, subject);
            yield return null;
        }

        public void Fit(CreatureRig rig, Vector3 offset = default)
        {
            Bounds bounds = RigBounds(rig);
            bounds.center += offset;
            bounds.Expand(0.4f);
            Camera camera = _session.manager.gameCamera;
            Pose pose = StageViewport.Fit(bounds, camera.transform.rotation, camera.fieldOfView, camera.aspect,
                StageViewport.Inset(_session.actions.ui.normalizedWorldViewport, 0.08f));
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        public void CheckVisible(CreatureRig rig, string subject)
        {
            Bounds bounds = RigBounds(rig);
            Rect viewport = _session.actions.ui.normalizedWorldViewport;
            Vector2 min = Vector2.one;
            Vector2 max = Vector2.zero;
            for (int i = 0; i < 8; i++)
            {
                Vector3 screen = _session.manager.gameCamera.WorldToViewportPoint(bounds.center
                    + Vector3.Scale(bounds.extents, RenderMath.CornerSign(i)));
                _output.Check(screen.z > 0f && viewport.Contains(screen), subject
                    + " fully grown body stays inside free Game viewport");
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }

            _output.Check((max.y - min.y) * Screen.height > 300f, subject
                + " body is at least 300 pixels tall for visual judgment");
        }

        static Bounds RigBounds(CreatureRig rig)
        {
            Bounds bounds = new Bounds(rig.root.position, Vector3.zero);
            foreach (Renderer renderer in rig.root.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
