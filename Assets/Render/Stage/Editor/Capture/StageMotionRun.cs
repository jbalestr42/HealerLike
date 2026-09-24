using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Grass;
using HealerLike.Render.Look;

namespace HealerLike.Render.Stage
{
    // One real game session: frozen control, ambient filmstrip, then paused battle comparisons.
    public class StageMotionRun : AStageRun
    {
        StageMotionOutput _output;
        Pose _cameraPose;
        float _cameraFov;

        protected override IEnumerator Run()
        {
            _output = new StageMotionOutput(Path.Combine(StagePlay.CaptureFolder, "motion"));
            StageMotionManifest manifest = _output.manifest;
            StageGameViewSize gameViewSize = new StageGameViewSize(StageCalibration.PortraitWidth,
                StageCalibration.PortraitHeight);
            manifest.unityVersion = Application.unityVersion;
            manifest.gpu = SystemInfo.graphicsDeviceName;
            manifest.revision = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION") ?? "unspecified";
            _manager.SetLandscape(false);
            yield return Wait(0.5f);
            _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
            yield return Wait(2f);
            BattleFocus focus = Object.FindAnyObjectByType<BattleFocus>();
            bool focusEnabled = focus != null && focus.enabled;
            if (focus != null)
            {
                focus.enabled = false;
            }
            Camera camera = _manager.gameCamera;
            _cameraPose = new Pose(camera.transform.position, camera.transform.rotation);
            _cameraFov = camera.fieldOfView;
            manifest.grassRegion = GrassRegion(camera);
            float timeScale = Time.timeScale;
            LookSettings settings = _manager.look.settings;
            manifest.inkScale = settings.inkScale;
            manifest.inkStart = settings.inkStart;
            manifest.grassNormalEdges = _manager.grass.lookMaterial.GetFloat("_HLNormalEdges") > 0f;
            manifest.grassCastsShadows = _manager.grass.tuftDraw.shadowCastingMode != ShadowCastingMode.Off;
            try
            {
                Time.timeScale = 0f;
                yield return Wait(0.3f);
                yield return _output.Capture(camera, "control-00", "control");
                if (!_output.isCaptured) yield break;
                yield return Wait(0.5f);
                yield return _output.Capture(camera, "control-01", "control");
                if (!_output.isCaptured) yield break;
                manifest.controlMeanDifference = _output.Compare("control-00.png", "control-01.png").x;
                Time.timeScale = 1f;
                double started = Time.timeAsDouble;
                Vector2 largest = Vector2.zero;
                for (int i = 0; i < 9; i++)
                {
                    while (Time.timeAsDouble - started < i * 0.5)
                    {
                        CheckCamera();
                        yield return null;
                    }
                    CheckCamera();
                    string name = "frame-" + i.ToString("00");
                    yield return _output.Capture(camera, name, "motion");
                    if (!_output.isCaptured) yield break;
                    if (i > 0)
                    {
                        Vector2 difference = _output.Compare("frame-00.png", name + ".png");
                        if (difference.x > largest.x) largest = difference;
                    }
                }
                manifest.motionMeanDifference = largest.x;
                manifest.motionChangedFraction = largest.y;
                manifest.cameraFixed = manifest.maximumCameraPositionError < 0.00001f
                    && manifest.maximumCameraRotationError < 0.001f;
                manifest.isPassed = StageMotionMeasure.Pass(manifest.controlMeanDifference, largest, manifest.cameraFixed);

                // The game's own wave button reaches battle; freeze the settled playable framing for all variants.
                if (focus != null) focus.enabled = focusEnabled;
                _hud.nextWaveButton.onClick.Invoke();
                yield return Wait(5f);
                Time.timeScale = 0f;
                if (focus != null) focus.enabled = false;
                yield return Wait(0.3f);
                yield return Variants(camera, settings);
            }
            finally
            {
                Time.timeScale = timeScale;
                _manager.look.settings = settings;
                if (focus != null) focus.enabled = focusEnabled;
                gameViewSize.Dispose();
                manifest.isPassed &= !_output.hasFailure && manifest.frames.Count == 18;
                _output.Write();
                Debug.Log($"[StageMotionRun] motion={manifest.motionMeanDifference:F4} control={manifest.controlMeanDifference:F4}"
                    + $" changed={manifest.motionChangedFraction:F4} cameraFixed={manifest.cameraFixed} passed={manifest.isPassed}");
                StagePlay.Finish(this, manifest.isPassed);
            }
        }

        void CheckCamera()
        {
            Camera camera = _manager.gameCamera;
            StageMotionManifest manifest = _output.manifest;
            manifest.maximumCameraPositionError = Mathf.Max(manifest.maximumCameraPositionError,
                Vector3.Distance(camera.transform.position, _cameraPose.position));
            manifest.maximumCameraRotationError = Mathf.Max(manifest.maximumCameraRotationError,
                Quaternion.Angle(camera.transform.rotation, _cameraPose.rotation));
            if (Mathf.Abs(camera.fieldOfView - _cameraFov) > 0.00001f)
            {
                manifest.maximumCameraPositionError = 1f;
            }
        }

        Rect GrassRegion(Camera camera)
        {
            Bounds board = _manager.board;
            Vector3 centre = new Vector3(Mathf.Lerp(board.min.x, board.max.x, 0.18f), board.max.y,
                Mathf.Lerp(board.min.z, board.max.z, 0.72f));
            Vector3 point = camera.WorldToViewportPoint(centre);
            return new Rect(point.x - 0.045f, point.y - 0.035f, 0.09f, 0.07f);
        }

        IEnumerator Variants(Camera camera, LookSettings original)
        {
            GrassField[] fields = Object.FindObjectsByType<GrassField>(FindObjectsSortMode.None);
            foreach (GrassField field in fields)
            {
                if (field.tuftDraw != null) field.tuftDraw.properties.SetFloat("_HLNormalEdges", 1f);
            }
            LookSettings settings = original;
            settings.inkScale = 0.05f;
            settings.inkStart = 0.46f;
            _manager.look.settings = settings;
            yield return Wait(0.2f);
            yield return _output.Capture(camera, "look-ink-050", "comparison");
            settings.inkScale = 0.025f;
            _manager.look.settings = settings;
            yield return Wait(0.2f);
            yield return _output.Capture(camera, "look-ink-025", "comparison");
            settings.inkScale = 0.05f;
            settings.inkStart = 0.6f;
            _manager.look.settings = settings;
            yield return Wait(0.2f);
            yield return _output.Capture(camera, "look-ink-start-060", "comparison");
            settings.inkStart = 0.46f;
            _manager.look.settings = settings;
            foreach (GrassField field in fields)
            {
                if (field.tuftDraw != null) field.tuftDraw.properties.SetFloat("_HLNormalEdges", 0f);
            }
            yield return Wait(0.2f);
            yield return _output.Capture(camera, "look-grass-edges-off", "comparison");
            foreach (GrassField field in fields)
            {
                if (field.tuftDraw != null) field.tuftDraw.properties.SetFloat("_HLNormalEdges", 1f);
            }
            yield return Wait(0.2f);
            yield return _output.Capture(camera, "look-grass-edges-on", "comparison");
            foreach (GrassField field in fields)
            {
                if (field.tuftDraw == null) continue;
                field.tuftDraw.shadowCastingMode = ShadowCastingMode.Off;
            }
            yield return Wait(0.2f);
            yield return _output.Capture(camera, "look-grass-shadows-off", "comparison");
            _manager.look.settings = original;
            foreach (GrassField field in fields)
            {
                if (field.tuftDraw == null) continue;
                field.tuftDraw.properties.SetFloat("_HLNormalEdges", field.lookMaterial.GetFloat("_HLNormalEdges"));
                field.tuftDraw.shadowCastingMode = ShadowCastingMode.On;
            }
            yield return Wait(0.2f);
            yield return _output.Capture(camera, "look-selected", "selected");
        }
    }
}
