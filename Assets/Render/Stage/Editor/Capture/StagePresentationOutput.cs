using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Screenshot requests do not wait during growth. Production clocks keep ticking between samples.
    public class StagePresentationOutput
    {
        [Serializable]
        public class Manifest
        {
            public string revision = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION");
            public string unityVersion = Application.unityVersion;
            public string source
                = "RenderStage actual Game view; production LateUpdate; held samples through StandaloneInputModule "
                + "and StageTouchInput.Update";
            public string blindSpot
                = "Editor Metal capture with synthetic OS touch samples; no physical Android input, appearance or "
                + "device performance evidence";
            public bool isPassed;
            public List<string> interventions = new List<string>();
            public List<string> checks = new List<string>();
            public List<string> failures = new List<string>();
            public List<GrowthFrame> growth = new List<GrowthFrame>();
            public List<string> portraitFiles = new List<string>();
        }

        [Serializable]
        public class PartScale
        {
            public string id;
            public string role;
            public Vector3 scale;
            public Vector3 authoredScale;
        }

        [Serializable]
        public class GrowthFrame
        {
            public string subject;
            public string sourceAsset;
            public string file;
            public int gameFrame;
            public int width;
            public int height;
            public double realTime;
            public float elapsed;
            public float appearanceElapsed;
            public float unscaledDeltaTime;
            public float timeScale;
            public bool isAppearing;
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public List<PartScale> parts = new List<PartScale>();
        }

        public readonly Manifest manifest = new Manifest();
        public readonly StageInterfaceOutput ui;
        readonly string _folder;
        public StagePresentationOutput()
        {
            _folder = Path.Combine(StagePlay.CaptureFolder, "creature-presentation");
            ui = new StageInterfaceOutput(_folder);
            manifest.checks = ui.manifest.checks;
            manifest.failures = ui.manifest.failures;
        }

        public void Check(bool result, string message)
        {
            ui.Check(result, message);
        }

        public void ExportPortrait(Texture2D source, string name)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = null;
            Texture2D copy = null;
            try
            {
                target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply();
                Color32[] pixels = copy.GetPixels32();
                Color32 corner = pixels[0];
                int foreground = 0;
                foreach (Color32 pixel in pixels)
                {
                    if (Mathf.Abs(pixel.r - corner.r) + Mathf.Abs(pixel.g - corner.g)
                        + Mathf.Abs(pixel.b - corner.b) > 24)
                    {
                        foreground++;
                    }
                }

                Check(foreground > pixels.Length / 100, "Actual cached portrait contains visible rendered detail: "
                    + name);
                Directory.CreateDirectory(_folder);
                File.WriteAllBytes(Path.Combine(_folder, name + ".png"), copy.EncodeToPNG());
                manifest.portraitFiles.Add(name + ".png");
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null)
                {
                    RenderTexture.ReleaseTemporary(target);
                }

                UnityEngine.Object.Destroy(copy);
            }
        }

        public GrowthFrame Sample(CreatureRig rig, Camera camera, string subject, string sourceAsset, float started,
            string screenshot)
        {
            GrowthFrame frame = new GrowthFrame
            {
                subject = subject,
                sourceAsset = sourceAsset,
                file = screenshot,
                gameFrame = Time.frameCount,
                width = Screen.width,
                height = Screen.height,
                realTime = Time.realtimeSinceStartupAsDouble,
                elapsed = Time.unscaledTime - started,
                appearanceElapsed = rig.appearanceElapsed,
                isAppearing = rig.isAppearing,
                unscaledDeltaTime = Time.unscaledDeltaTime,
                timeScale = Time.timeScale,
                cameraPosition = camera.transform.position,
                cameraRotation = camera.transform.rotation
            };
            for (int i = 0; i < rig.partTransforms.Count; i++)
            {
                CreaturePart part = rig.parts[i];
                frame.parts.Add(new PartScale { id = part.id, role = part.role.ToString(), scale
                    = rig.partTransforms[i].localScale, authoredScale = part.dimensions * rig.cellSize });
            }

            manifest.growth.Add(frame);
            if (!string.IsNullOrEmpty(screenshot))
            {
                Directory.CreateDirectory(_folder);
                string path = Path.Combine(_folder, screenshot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ScreenCapture.CaptureScreenshot(path);
            }

            return frame;
        }

        public IEnumerator VerifyImages()
        {
            foreach (GrowthFrame frame in manifest.growth)
            {
                if (string.IsNullOrEmpty(frame.file))
                {
                    continue;
                }

                string path = Path.Combine(_folder, frame.file);
                float deadline = Time.realtimeSinceStartup + 20f;
                while (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        Check(false, "Timed growth screenshot missing: " + frame.file);
                    }

                    yield return null;
                }

                bool matches = StageCaptureImage.MatchesSize(path, frame.width, frame.height);
                Check(matches, "Timed Game image dimensions match sampled frame: " + frame.file);
            }
        }

        public void Write(bool passed)
        {
            manifest.isPassed = passed && manifest.failures.Count == 0;
            ui.Write(manifest.isPassed);
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "presentation.json"), JsonUtility.ToJson(manifest, true));
        }

        public void Fail(string message)
        {
            if (!manifest.failures.Contains(message))
            {
                manifest.failures.Add(message);
            }

            Write(false);
        }
    }
}
