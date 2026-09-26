using System;
using System.Reflection;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    // Unity exposes fixed Game-view sizes only through its internal Editor types. The preset stays in memory
    // for this capture and is removed on dispose; the previous selection is restored without saving preferences.
    public class StageGameViewSize : IDisposable
    {
        readonly EditorWindow _window;
        readonly object _group;
        readonly PropertyInfo _selection;
        readonly int _previous;
        readonly int _temporary;
        bool _isDisposed;

        public StageGameViewSize(int width, int height)
        {
            Assembly editor = typeof(Editor).Assembly;
            Type windowType = editor.GetType("UnityEditor.GameView");
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editor.GetType("UnityEditor.GameViewSize");
            Type kindType = editor.GetType("UnityEditor.GameViewSizeType");
            object sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
            _group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            _window = EditorWindow.GetWindow(windowType, false, null, true);
            _selection = windowType.GetProperty("selectedSizeIndex");
            _previous = (int)_selection.GetValue(_window);
            _temporary = (int)_group.GetType().GetMethod("GetTotalCount").Invoke(_group, null);
            object size = Activator.CreateInstance(sizeType, Enum.Parse(kindType, "FixedResolution"),
                width, height, "Render capture (temporary)");
            _group.GetType().GetMethod("AddCustomSize").Invoke(_group, new[] { size });
            _selection.SetValue(_window, _temporary);
            _window.Focus();
            _window.Repaint();
        }

        // A workspace preset survives capture disposal and Editor restarts. Reuse a fixed size already present.
        public static void SelectPersistent(int width, int height)
        {
            Assembly editor = typeof(Editor).Assembly;
            Type windowType = editor.GetType("UnityEditor.GameView");
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editor.GetType("UnityEditor.GameViewSize");
            Type kindType = editor.GetType("UnityEditor.GameViewSizeType");
            object sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            int selected = total;
            object fixedResolution = Enum.Parse(kindType, "FixedResolution");
            for (int i = 0; i < total; i++)
            {
                object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)sizeType.GetProperty("width").GetValue(existing) == width
                    && (int)sizeType.GetProperty("height").GetValue(existing) == height
                    && sizeType.GetProperty("sizeType").GetValue(existing).Equals(fixedResolution))
                {
                    selected = i;
                    break;
                }
            }
            if (selected == total)
            {
                object size = Activator.CreateInstance(sizeType, fixedResolution, width, height, "HealerLike Portrait");
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            }
            sizesType.GetMethod("SaveToHDD", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(sizes, null);
            EditorWindow window = EditorWindow.GetWindow(windowType, false, null, true);
            windowType.GetProperty("selectedSizeIndex").SetValue(window, selected);
            window.Focus();
            window.Repaint();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _selection.SetValue(_window, _previous);
            _group.GetType().GetMethod("RemoveCustomSize").Invoke(_group, new object[] { _temporary });
            _window.Repaint();
        }
    }
}
