using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // One state of the Toolkit design preview in both themes, from batch mode:
    // -executeMethod HealerLike.Render.Stage.DesignPreviewCapture.MapMidrun
    // The window builds its tree as it does on screen; the tree is then moved into a runtime panel in an empty play
    // scene, since a batchmode editor window never presents. Frames and a node readout go to Logs/DesignPreview, or
    // RENDER_CAPTURE_DIR/design-preview when that is set.
    [InitializeOnLoad]
    public static class DesignPreviewCapture
    {
        static readonly string stateKey = "DesignPreviewCapture.State";
        static readonly string themeKey = "DesignPreviewCapture.Theme";
        static readonly string codeKey = "DesignPreviewCapture.Code";
        static readonly string deadlineKey = "DesignPreviewCapture.Deadline";
        static readonly string[] themes = { "Forest", "Moon" };
        static readonly int width = 1440;
        static readonly int height = 900;
        static readonly float scale = 2f;

        static EditorWindow _window;
        static StageGameViewSize _size;
        static int _frame;
        static string _shot;
        static string _topShot;

        static DesignPreviewCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
        }

        public static void MapMidrun()
        {
            Enter("map-midrun", 0);
        }

        static string Folder
        {
            get
            {
                string root = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_DIR");
                string folder = string.IsNullOrEmpty(root)
                    ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "DesignPreview")
                    : Path.Combine(root, "design-preview");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        static void Enter(string state, int theme)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetString(stateKey, state);
            SessionState.SetInt(themeKey, theme);
            SessionState.SetInt(codeKey, 1);
            SessionState.SetFloat(deadlineKey, (float)EditorApplication.timeSinceStartup + 180f);
            EditorApplication.isPlaying = true;
        }

        static ThemeStyleSheet Theme(int index)
        {
            return themes[index] == "Moon"
                ? AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Resources/UI/Toolkit/MoonTheme.tss")
                : AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Resources/UI/Toolkit/RuntimeTheme.tss");
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            string state = SessionState.GetString(stateKey, "");
            if (state == "")
            {
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                Build(state, SessionState.GetInt(themeKey, 0));
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                Release();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                int next = SessionState.GetInt(themeKey, 0) + 1;
                if (SessionState.GetInt(codeKey, 1) == 0 && next < themes.Length)
                {
                    Enter(state, next);
                    return;
                }

                // The plain map after the mid-run one: the same rest rooms with nobody fallen
                if (SessionState.GetInt(codeKey, 1) == 0 && state == "map-midrun")
                {
                    Enter("map", 0);
                    return;
                }

                int code = SessionState.GetInt(codeKey, 1);
                SessionState.SetString(stateKey, "");
                EditorApplication.Exit(code);
            }
        }

        static void Build(string state, int themeIndex)
        {
            ThemeStyleSheet theme = Theme(themeIndex);
            Type type = Type.GetType("ToolkitDesignPreview, HealerLike.Editor");
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            _window = (EditorWindow)ScriptableObject.CreateInstance(type);
            type.GetField("_state", flags).SetValue(_window, state);
            type.GetField("_theme", flags).SetValue(_window, theme);
            type.GetMethod("CreateGUI").Invoke(_window, null);
            VisualElement preview = (VisualElement)type.GetField("_preview", flags).GetValue(_window);

            GameObject camera = new GameObject("Capture camera");
            Camera view = camera.AddComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.black;
            PanelSettings panel = ToolkitTheme.CreatePanelSettings(null, theme);
            panel.scale = scale;
            GameObject host = new GameObject("Design preview");
            UIDocument document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            preview.RemoveFromHierarchy();
            document.rootVisualElement.Add(preview);
            _size = new StageGameViewSize((int)(width * scale), (int)(height * scale));
            _frame = 0;
            _shot = Path.Combine(Folder, state + "-" + themes[themeIndex] + ".png");
            _topShot = Path.Combine(Folder, state + "-" + themes[themeIndex] + "-top.png");
            foreach (string shot in new[] { _shot, _topShot })
            {
                if (File.Exists(shot))
                {
                    File.Delete(shot);
                }
            }

            Debug.Log("[DesignPreviewCapture] " + state + " in " + themes[themeIndex] + " theme "
                + (theme != null ? AssetDatabase.GetAssetPath(theme) : "none"));
        }

        static void OnUpdate()
        {
            string state = SessionState.GetString(stateKey, "");
            if (state == "")
            {
                return;
            }

            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(deadlineKey, 0f))
            {
                Debug.LogError("[DesignPreviewCapture] The session ran past its deadline.");
                SessionState.SetString(stateKey, "");
                EditorApplication.Exit(2);
                return;
            }

            if (!EditorApplication.isPlaying || _shot == null)
            {
                return;
            }

            // Batchmode has no visible Game view, the repaint is what renders the panel
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"), false, null, false)
                .Repaint();
            _frame++;
            if (_frame == 30)
            {
                ScreenCapture.CaptureScreenshot(_shot);
            }
            else if (_frame > 30 && _frame < 1000 && File.Exists(_shot))
            {
                WriteReadout(state);
                Debug.Log("[DesignPreviewCapture] Wrote " + _shot);
                // The graph opens on the current room; the top floors, where elites and the boss sit, need a scroll
                ScrollView scroll = UnityEngine.Object.FindAnyObjectByType<UIDocument>().rootVisualElement
                    .Q<ScrollView>("map-scroll");
                scroll.scrollOffset = Vector2.zero;
                _frame = 1000;
            }
            else if (_frame == 1030)
            {
                ScreenCapture.CaptureScreenshot(_topShot);
            }
            else if (_frame > 1030 && File.Exists(_topShot))
            {
                Debug.Log("[DesignPreviewCapture] Wrote " + _topShot);
                _shot = null;
                SessionState.SetInt(codeKey, 0);
                EditorApplication.isPlaying = false;
            }
        }

        // Every map node's room, state, glyph tint and subtitle, as the panel resolved them
        static void WriteReadout(string state)
        {
            UIDocument document = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            StringBuilder lines = new StringBuilder("room\tstate\tglyph\tsubtitle\tsubtitleShown\tsubtitleStyle\tbounds\n");
            document.rootVisualElement.Query<Button>(className: "map-node").ForEach(node =>
            {
                string room = "";
                string nodeState = "";
                foreach (string name in node.GetClasses())
                {
                    if (name.StartsWith("room-"))
                    {
                        room = name.Substring(5);
                    }
                    else if (name.StartsWith("is-"))
                    {
                        nodeState += name + " ";
                    }
                }

                Color32 glyph = node.Q("map-glyph").resolvedStyle.color;
                Label subtitle = node.Q<Label>("map-subtitle");
                lines.AppendLine(room + "\t" + nodeState.Trim() + "\t" + glyph.r + "," + glyph.g + "," + glyph.b
                    + "\t" + (subtitle != null ? subtitle.text : "MISSING") + "\t"
                    + (subtitle != null && subtitle.resolvedStyle.display == DisplayStyle.Flex) + "\t"
                    + (subtitle != null ? SubtitleStyle(subtitle) : "") + "\t"
                    + node.worldBound);
            });
            File.WriteAllText(Path.ChangeExtension(_shot, ".tsv"), lines.ToString());
        }

        // Whether the subtitle rule reached the label: an unstyled label resolves to the flow defaults
        static string SubtitleStyle(Label subtitle)
        {
            IResolvedStyle style = subtitle.resolvedStyle;
            return style.position + " top " + style.top + " right " + style.right + " font " + style.fontSize
                + " bg " + (Color32)style.backgroundColor + " classes " + string.Join(" ", subtitle.GetClasses())
                + " bounds " + subtitle.worldBound;
        }

        static void Release()
        {
            if (_size != null)
            {
                _size.Dispose();
                _size = null;
            }

            if (_window != null)
            {
                UnityEngine.Object.DestroyImmediate(_window);
                _window = null;
            }
        }
    }
}
