using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // A capture borrows only a live host's owned panel and restores its theme when that host is detached.
    public class StageCaptureTheme : IDisposable
    {
        [Serializable]
        public class Identity
        {
            public string name;
            public string path;
            public string guid;
        }

        static readonly string selectionKey = "StageCaptureTheme.MobileInterface";
        readonly PanelSettings _panel;
        readonly VisualElement _root;
        readonly ThemeStyleSheet _selected;
        readonly ThemeStyleSheet _previous;
        readonly bool _hadThemeClass;
        readonly List<ThemeStyleSheet> _previousSheets = new List<ThemeStyleSheet>();
        bool _disposed;

        public StageCaptureTheme(PanelSettings panel, VisualElement root, ThemeStyleSheet selected)
        {
            if (panel == null || EditorUtility.IsPersistent(panel) || root == null || selected == null)
            {
                throw new ArgumentException("Capture theme requires a live owned panel, root and selected TSS.");
            }

            _panel = panel;
            _root = root;
            _selected = selected;
            _previous = panel.themeStyleSheet;
            _hadThemeClass = root.ClassListContains("toolkit-theme");
            for (int i = 0; i < root.styleSheets.count; i++)
            {
                if (root.styleSheets[i] is ThemeStyleSheet theme)
                {
                    _previousSheets.Add(theme);
                }
            }

            try
            {
                panel.themeStyleSheet = selected;
                ToolkitTheme.Apply(root, selected);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_panel != null && _panel.themeStyleSheet == _selected)
            {
                _panel.themeStyleSheet = _previous;
            }

            if (_root.styleSheets.Contains(_selected))
            {
                _root.styleSheets.Remove(_selected);
                foreach (ThemeStyleSheet theme in _previousSheets)
                {
                    _root.styleSheets.Add(theme);
                }

                if (!_hadThemeClass)
                {
                    _root.RemoveFromClassList("toolkit-theme");
                }
            }
        }

        public static ThemeStyleSheet Resolve(string path)
        {
            ThemeStyleSheet theme = string.IsNullOrEmpty(path) ? ToolkitTheme.Resolve(null)
                : AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
            if (theme == null)
            {
                throw new ArgumentException("RENDER_CAPTURE_THEME must name an existing ThemeStyleSheet asset: "
                    + path);
            }

            return theme;
        }

        public static void Prepare()
        {
            SessionState.EraseString(selectionKey);
            ThemeStyleSheet theme = Resolve(System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_THEME"));
            SessionState.SetString(selectionKey, AssetDatabase.GetAssetPath(theme));
        }

        public static ThemeStyleSheet TakeSelection()
        {
            string path = SessionState.GetString(selectionKey, "");
            SessionState.EraseString(selectionKey);
            return Resolve(path);
        }

        public static Identity Describe(ThemeStyleSheet theme)
        {
            string path = AssetDatabase.GetAssetPath(theme);
            if (theme == null || string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Capture theme identity requires a saved TSS asset.");
            }

            return new Identity { name = theme.name, path = path, guid = AssetDatabase.AssetPathToGUID(path) };
        }
    }
}
