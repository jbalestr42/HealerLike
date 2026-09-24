using System;
using System.Reflection;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    // Unity exposes fixed Game-view sizes only through its internal Editor types. The preset stays in memory
    // for this capture and is removed on dispose; the previous selection is restored without saving preferences.
    public sealed class StageGameViewSize : IDisposable
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

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _selection.SetValue(_window, _previous);
            _group.GetType().GetMethod("RemoveCustomSize").Invoke(_group, new object[] { _temporary });
            _window.Repaint();
        }
    }
}
