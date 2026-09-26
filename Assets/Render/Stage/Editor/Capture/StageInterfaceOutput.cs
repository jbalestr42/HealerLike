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
            public string revision = StagePlay.ReadRevision();
            public string unityVersion = Application.unityVersion;
            public string inputMethod
                = "Actual Toolkit button/focus events, Render touch adapter world taps; no physical device";
            public bool isPassed;
            public StageCaptureTheme.Identity theme;
            public List<string> checks = new List<string>();
            public List<string> failures = new List<string>();
            public List<Frame> frames = new List<Frame>();
        }

        [Serializable]
        public class Frame
        {
            public string file;
            public string inputMethod;
            public int width;
            public int height;
            public StageCaptureTheme.Identity theme;
            public Rect safeArea;
            public bool hasSafeAreaOverride;
            public string safeAreaSource;
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
            _folder = folder != null ? folder : Path.Combine(StagePlay.CaptureFolder, "mobile-interface");
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

        public IEnumerator Capture(ToolkitGameUI ui, string name, string inputMethod = null)
        {
            Directory.CreateDirectory(_folder);
            string path = Path.Combine(_folder, name + ".png");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            UIDocument document = ui.GetComponent<UIDocument>();
            VisualElement root = document.rootVisualElement;
            Frame frame = new Frame();
            frame.file = name + ".png";
            frame.inputMethod = inputMethod != null ? inputMethod : manifest.inputMethod;
            frame.width = Screen.width;
            frame.height = Screen.height;
            frame.theme = StageCaptureTheme.Describe(document.panelSettings.themeStyleSheet);
            Check(root.styleSheets.Contains(document.panelSettings.themeStyleSheet),
                "Root and owned panel share the captured theme: " + name);
            if (manifest.theme != null)
            {
                Check(frame.theme.guid == manifest.theme.guid && frame.theme.path == manifest.theme.path,
                    "Requested theme remains active on this live host: " + name);
            }

            frame.safeArea = Screen.safeArea;
            frame.hasSafeAreaOverride = ui.safeAreaProvider != null;
            frame.safeAreaSource = frame.hasSafeAreaOverride ? "simulated-insets" : "device-screen";
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

            if (frame.hasSafeAreaOverride)
            {
                Rect normalized = frame.normalizedSafeAreaOverride;
                Rect panel = root.worldBound;
                Rect safe = new Rect(panel.x + normalized.x * panel.width, panel.y
                    + (1f - normalized.yMax) * panel.height,
                    normalized.width * panel.width, normalized.height * panel.height);
                foreach (VisualElement control in root.Query<Button>().ToList())
                {
                    CheckSafe(control, safe);
                }

                foreach (VisualElement control in root.Query<DropdownField>().ToList())
                {
                    CheckSafe(control, safe);
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

            bool matches = StageCaptureImage.MatchesSize(path, frame.width, frame.height);
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

        void CheckSafe(VisualElement control, Rect safe)
        {
            if (!IsVisible(control))
            {
                return;
            }

            VisualElement picked = control.panel.Pick(control.worldBound.center);
            bool reachable = false;
            for (VisualElement current = picked; current != null; current = current.parent)
            {
                reachable |= current == control;
            }

            if (reachable)
            {
                Rect bounds = control.worldBound;
                Check(bounds.xMin >= safe.xMin - 1f && bounds.yMin >= safe.yMin - 1f
                    && bounds.xMax <= safe.xMax + 1f && bounds.yMax <= safe.yMax + 1f,
                    "Reachable control inside simulated safe area: " + control.name);
            }
        }

        public static bool IsVisible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width < 1f
                || element.worldBound.height < 1f)
            {
                return false;
            }

            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None
                    || current.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
