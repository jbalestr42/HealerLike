#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class EntityDrawer<T> : OdinValueDrawer<T> where T : EntityData
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var rect = EditorGUILayout.GetControlRect(label != null, 45);

        if (label != null)
        {
            rect.xMin = EditorGUI.PrefixLabel(rect.AlignCenterY(15), label).xMin;
        }
        else
        {
            rect = EditorGUI.IndentedRect(rect);
        }

        EntityData data = this.ValueEntry.SmartValue;
        Texture texture = null;

        if (data)
        {
            // texture = GUIHelper.GetAssetThumbnail(data.Icon, typeof(T), true);
            GUI.Label(rect.AddXMin(50).AlignMiddle(16), EditorGUI.showMixedValue ? "-" : data.title);
        }

        this.ValueEntry.WeakSmartValue = SirenixEditorFields.UnityPreviewObjectField(rect.AlignLeft(45), data, texture, this.ValueEntry.BaseValueType);
    }
}
#endif
