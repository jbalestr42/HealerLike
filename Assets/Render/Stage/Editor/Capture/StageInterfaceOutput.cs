using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceOutput
    {
        [Serializable]
        public class Manifest
        {
            public string revision = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION") ?? "unspecified";
            public string unityVersion = Application.unityVersion;
            public string inputMethod = "Actual Toolkit button events, Render touch adapter world taps; no physical device";
            public bool isPassed;
            public List<string> checks = new List<string>();
            public List<string> failures = new List<string>();
            public List<Frame> frames = new List<Frame>();
        }

        [Serializable]
        public class Frame
        {
            public string file;
            public int width;
            public int height;
            public Rect safeArea;
            public bool hasSafeAreaOverride;
            public Rect normalizedSafeAreaOverride;
            public Rect battlefieldViewport;
            public Rect hud;
            public Rect world;
            public Rect dock;
            public List<Control> controls = new List<Control>();
        }

        [Serializable]
        public class Control
        {
            public string name;
            public string text;
            public Rect panelBounds;
            public bool enabled;
        }

        public readonly Manifest manifest = new Manifest();
        readonly string _folder;

        public StageInterfaceOutput(string folder = null)
        {
            _folder = folder ?? Path.Combine(StagePlay.CaptureFolder, "mobile-interface");
        }

        public void Check(bool passed, string detail)
        {
            if (!passed)
            {
                manifest.failures.Add(detail);
                throw new InvalidOperationException("[StageInterfaceRun] " + detail);
            }
            manifest.checks.Add(detail);
        }

        public IEnumerator Capture(ToolkitGameUI ui, string name)
        {
            Directory.CreateDirectory(_folder);
            string path = Path.Combine(_folder, name + ".png");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            VisualElement root = ui.GetComponent<UIDocument>().rootVisualElement;
            Frame frame = new Frame();
            frame.file = name + ".png";
            frame.width = Screen.width;
            frame.height = Screen.height;
            frame.safeArea = Screen.safeArea;
            frame.hasSafeAreaOverride = ui.safeAreaProvider != null;
            if (frame.hasSafeAreaOverride)
            {
                frame.normalizedSafeAreaOverride = ui.safeAreaProvider();
            }
            frame.battlefieldViewport = ui.normalizedWorldViewport;
            frame.hud = root.Q("hud-root").worldBound;
            frame.world = root.Q("world-space").worldBound;
            frame.dock = root.Q("command-dock").worldBound;
            foreach (Button button in root.Query<Button>().ToList())
            {
                if (IsVisible(button))
                {
                    frame.controls.Add(new Control { name = button.name, text = button.text,
                        panelBounds = button.worldBound, enabled = button.enabledInHierarchy });
                }
            }
            ScreenCapture.CaptureScreenshot(path);
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    throw new InvalidOperationException("Screenshot timed out: " + name);
                }
                yield return null;
            }
            int writtenFrame = Time.frameCount;
            while (Time.frameCount <= writtenFrame)
            {
                yield return null;
            }
            Texture2D image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            image.LoadImage(File.ReadAllBytes(path));
            bool matches = image.width == frame.width && image.height == frame.height;
            UnityEngine.Object.Destroy(image);
            Check(matches, "Native image dimensions match game-frame Screen: " + name);
            manifest.frames.Add(frame);
        }

        public void Fail(string detail)
        {
            manifest.failures.Add(detail);
            Write(false);
        }

        public void Write(bool passed)
        {
            manifest.isPassed = passed && manifest.failures.Count == 0;
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "interface.json"), JsonUtility.ToJson(manifest, true));
        }

        public static bool IsVisible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width < 1f || element.worldBound.height < 1f)
            {
                return false;
            }
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
