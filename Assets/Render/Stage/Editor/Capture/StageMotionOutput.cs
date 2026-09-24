using System.Collections;
using System.IO;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageMotionOutput
    {
        public readonly StageMotionManifest manifest = new StageMotionManifest();
        public bool isCaptured { get; private set; }
        public bool hasFailure { get; private set; }
        readonly string _folder;

        public StageMotionOutput(string folder)
        {
            _folder = folder;
            Directory.CreateDirectory(folder);
        }

        // The native screen capture queues a readback at frame end, including overlay UI. No camera render request.
        public IEnumerator Capture(Camera camera, string name, string group)
        {
            isCaptured = false;
            string path = Path.Combine(_folder, name + ".png");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            StageMotionFrame frame = new StageMotionFrame
            {
                file = name + ".png",
                group = group,
                requestedGameSeconds = Time.timeAsDouble,
                requestedRealSeconds = Time.realtimeSinceStartupAsDouble,
                requestedFrame = Time.frameCount,
                cameraPosition = camera.transform.position,
                cameraRotation = camera.transform.rotation,
                fieldOfView = camera.fieldOfView
            };
            ScreenCapture.CaptureScreenshot(path);
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                if (Time.realtimeSinceStartupAsDouble > deadline)
                {
                    hasFailure = true;
                    Debug.LogError("[StageMotionOutput] Game view screenshot did not arrive: " + path);
                    yield break;
                }
                yield return null;
            }

            // Reading after another rendered frame avoids racing the native writer's close.
            int writtenFrame = Time.frameCount;
            while (Time.frameCount <= writtenFrame)
            {
                yield return null;
            }
            Texture2D image = Load(frame.file);
            if (image.width < 64 || image.height < 64)
            {
                hasFailure = true;
                Object.Destroy(image);
                Debug.LogError("[StageMotionOutput] Game view screenshot has no usable dimensions.");
                yield break;
            }
            frame.width = image.width;
            frame.height = image.height;
            frame.completedFrame = Time.frameCount;
            Object.Destroy(image);
            manifest.frames.Add(frame);
            isCaptured = true;
        }

        public Texture2D Load(string file)
        {
            Texture2D image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            image.LoadImage(File.ReadAllBytes(Path.Combine(_folder, file)));
            return image;
        }

        public Vector2 Compare(string first, string second)
        {
            Texture2D a = Load(first);
            Texture2D b = Load(second);
            Vector2 measure = a.width == b.width && a.height == b.height
                ? StageMotionMeasure.Difference(a.GetPixels32(), b.GetPixels32(), a.width, a.height,
                    manifest.grassRegion) : new Vector2(-1f, -1f);
            Object.Destroy(a);
            Object.Destroy(b);
            return measure;
        }

        public void Write()
        {
            File.WriteAllText(Path.Combine(_folder, "motion.json"), JsonUtility.ToJson(manifest, true));
        }
    }
}
