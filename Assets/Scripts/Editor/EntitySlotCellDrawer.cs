#if UNITY_EDITOR
using UnityEngine;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using System.Drawing.Imaging;
using UnityEditor;
using System.CodeDom;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using System;

internal sealed class EntitySlotCellDrawer<T> : TwoDimensionalArrayDrawer<T, EntitySlot> where T : System.Collections.IList
{
    protected override TableMatrixAttribute GetDefaultTableMatrixAttributeSettings()
    {
        return new TableMatrixAttribute()
        {
            SquareCells = true,
            HideColumnIndices = true,
            HideRowIndices = true,
            ResizableColumns = false
        };
    }

    protected override EntitySlot DrawElement(Rect rect, EntitySlot value)
    {
        var id = DragAndDropUtilities.GetDragAndDropId(rect);
        DragAndDropUtilities.DrawDropZone(rect, value.entity ? value.entity.model : null, null, id);
        value.entity = DragAndDropUtilities.ObjectPickerZone(rect, value.entity, false, id);

        if (Event.current.type == EventType.MouseDown && Event.current.clickCount > 1 && rect.Contains(Event.current.mousePosition))
        {
            GUI.changed = true;
            Event.current.Use();

            if (value.entity != null)
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(value.entity);
            }
        }

        if (value.entity != null)
        {
            var countRect = rect.Padding(2).AlignBottom(16);
            GUI.Label(countRect, value.entity.title, SirenixGUIStyles.RightAlignedGreyMiniLabel);
        }

        value = DragAndDropUtilities.DropZone(rect, value); // Drop zone for EntitySlot structs.
        value.entity = DragAndDropUtilities.DropZone<EntityData>(rect, value.entity); // Drop zone for Item types.
        value = DragAndDropUtilities.DragZone(rect, value, true, true); // Enables dragging of the EntitySlot

        return value;
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        base.DrawPropertyLayout(label);

        // Draws a drop-zone where we can destroy items.
        var rect = GUILayoutUtility.GetRect(0, 40);
        var id = DragAndDropUtilities.GetDragAndDropId(rect);
        DragAndDropUtilities.DrawDropZone(rect, null as UnityEngine.Object, null, id);
        DragAndDropUtilities.DropZone<EntitySlot>(rect, new EntitySlot(), false, id);
    }
}

#endif
