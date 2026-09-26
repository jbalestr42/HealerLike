using System;
using UnityEditor;

namespace HealerLike.Render.Studio.Editor
{
    // IMGUI can leave a draw early through ExitGUI; each pane restores the label width it borrowed.
    public class StudioLabelWidthScope : IDisposable
    {
        readonly float _previous;

        public StudioLabelWidthScope(float width)
        {
            _previous = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
        }

        public void Dispose()
        {
            EditorGUIUtility.labelWidth = _previous;
        }
    }
}
