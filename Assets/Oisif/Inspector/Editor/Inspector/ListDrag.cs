using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor
{
    // Reordering the elements of a list drawn with GUILayout by dragging them by a handle on their left:
    // Begin() before the elements, Handle() at the start of each one, End() after them with their rects
    public static class ListDrag
    {
        public const float HandleWidth = 14f;

        static readonly Color DropLineColor = new Color(0.24f, 0.49f, 0.91f);
        static GUIStyle _handleStyle;

        // The list being dragged in: its object and path, the element dragged, the slot it would be dropped in
        static Object _draggedObject;
        static string _draggedPath;
        static int _draggedIndex = -1;
        static int _dropSlot = -1;

        // The slot (0 before the first element, count after the last one) under the mouse: the number of elements
        // whose middle is above it
        public static int GetDropSlot(IList<Rect> rects, float mouseY)
        {
            int slot = 0;
            while (slot < rects.Count && rects[slot].center.y < mouseY)
            {
                slot++;
            }
            return slot;
        }

        // The index the dragged element ends at once dropped in the slot: the slots after it count one less, as
        // the element leaves its place
        public static int GetDropIndex(int draggedIndex, int slot)
        {
            return slot > draggedIndex ? slot - 1 : slot;
        }

        // The index the last element dropped ended at
        public static int lastDropIndex { get; private set; } = -1;

        // The element of the list being dragged, -1 when none
        public static int GetDraggedIndex(SerializedProperty list)
        {
            return IsDragging(list) ? _draggedIndex : -1;
        }

        static bool IsDragging(SerializedProperty list)
        {
            return _draggedIndex >= 0 && _draggedObject == list.serializedObject.targetObject && _draggedPath == list.propertyPath;
        }

        // The handle of the element, at the indent; a click on it starts dragging the element
        public static void Handle(SerializedProperty list, int index)
        {
            float indent = EditorGUI.indentLevel * 15f;
            Rect rect = GUILayoutUtility.GetRect(indent + HandleWidth, EditorGUIUtility.singleLineHeight, GUILayout.Width(indent + HandleWidth));
            rect.xMin += indent;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);

            Event current = Event.current;
            if (current.type == EventType.Repaint)
            {
                _handleStyle = _handleStyle ?? new GUIStyle("RL DragHandle");
                _handleStyle.Draw(new Rect(rect.x + 2f, rect.y + 7f, 10f, 6f), false, false, false, false);
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                _draggedObject = list.serializedObject.targetObject;
                _draggedPath = list.propertyPath;
                _draggedIndex = index;
                _dropSlot = index;
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                current.Use();
            }
        }

        // After the elements, with the rect of each: follows the mouse, shows where the element goes, moves it on
        // release. True when the list changed
        public static bool End(SerializedProperty list, IList<Rect> rects)
        {
            if (!IsDragging(list))
            {
                return false;
            }

            Event current = Event.current;
            switch (current.rawType)
            {
                case EventType.MouseDrag:
                    if (rects.Count > 0)
                    {
                        _dropSlot = GetDropSlot(rects, current.mousePosition.y);
                    }
                    current.Use();
                    if (EditorWindow.focusedWindow != null)
                    {
                        EditorWindow.focusedWindow.Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    int from = _draggedIndex;
                    int to = GetDropIndex(from, _dropSlot);
                    Stop();
                    current.Use();
                    if (to != from && from < list.arraySize && to >= 0 && to < list.arraySize)
                    {
                        list.MoveArrayElement(from, to);
                        lastDropIndex = to;
                        GUI.changed = true;
                        return true;
                    }
                    break;
                case EventType.Repaint:
                    if (_dropSlot >= 0 && rects.Count > 0)
                    {
                        float y = _dropSlot < rects.Count ? rects[_dropSlot].y - 2f : rects[rects.Count - 1].yMax;
                        Rect first = rects[0];
                        EditorGUI.DrawRect(new Rect(first.x, y, first.width, 2f), DropLineColor);
                    }
                    break;
            }
            return false;
        }

        static void Stop()
        {
            _draggedIndex = -1;
            _dropSlot = -1;
            _draggedPath = null;
            _draggedObject = null;
            GUIUtility.hotControl = 0;
        }
    }
}
