using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor
{
    // How GridGUI draws a grid
    public class GridGUIOptions
    {
        public float cellSize = 72f;
        // Written above the first column (e.g. "Front"), nothing when empty
        public string firstColumnLabel = "";
        // Picture of a cell content, the icon of its type when null or giving null
        public Func<UnityEngine.Object, Texture> getPreview;
        // Name written under the picture, the object name when null
        public Func<UnityEngine.Object, string> getLabel;
    }

    // Draws a list of object references as a grid of width columns and height rows, the cells column after column
    // (index x * height + y), the row 0 at the bottom. A cell takes an object dropped on it or picked with its Select
    // button (at the bottom of an empty cell, or of a filled one under the mouse), a click shows its object in the
    // Project window, a drag moves it to another cell, a right click empties it. Changing the size keeps each object
    // in its cell
    public static class GridGUI
    {
        static readonly Color EmptyColor = new Color(0f, 0f, 0f, 0.15f);
        static readonly Color FilledColor = new Color(0.3f, 0.5f, 0.8f, 0.25f);
        static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.15f);
        static readonly int CellHash = "OisifGridCell".GetHashCode();
        // Pixels the mouse moves, pressed on a cell, before dragging its object
        const float DragThreshold = 4f;
        static Vector2 _pressPosition;
        // The cell under the mouse at the last move, -1 out of the grid
        static int _hoveredIndex = -1;

        public static int GetIndex(int x, int y, int height)
        {
            return x * height + y;
        }

        // The cells of a width x height grid resized to newWidth x newHeight, each one at the same x and y, the new
        // ones empty (default)
        public static List<T> Resize<T>(IReadOnlyList<T> cells, int width, int height, int newWidth, int newHeight)
        {
            newWidth = Mathf.Max(0, newWidth);
            newHeight = Mathf.Max(0, newHeight);
            List<T> resized = new List<T>(new T[newWidth * newHeight]);
            for (int x = 0; x < Mathf.Min(width, newWidth); x++)
            {
                for (int y = 0; y < Mathf.Min(height, newHeight); y++)
                {
                    int index = GetIndex(x, y, height);
                    if (index < cells.Count)
                    {
                        resized[GetIndex(x, y, newHeight)] = cells[index];
                    }
                }
            }
            return resized;
        }

        // Draws the size fields and the grid of the list property, the objects being of objectType
        public static void Draw(SerializedProperty cells, SerializedProperty width, SerializedProperty height, Type objectType, GridGUIOptions options = null)
        {
            options = options ?? new GridGUIOptions();
            DrawSize(cells, width, height);
            EnsureSize(cells, width.intValue * height.intValue);

            if (!string.IsNullOrEmpty(options.firstColumnLabel))
            {
                EditorGUILayout.LabelField($"← {options.firstColumnLabel}", EditorStyles.miniBoldLabel);
            }

            Rect area = GUILayoutUtility.GetRect(width.intValue * options.cellSize, height.intValue * options.cellSize, GUILayout.ExpandWidth(false));
            RepaintOnHover(area, height.intValue, options.cellSize);
            for (int x = 0; x < width.intValue; x++)
            {
                for (int y = 0; y < height.intValue; y++)
                {
                    // The row 0 at the bottom
                    Rect cell = new Rect(area.x + x * options.cellSize, area.y + (height.intValue - 1 - y) * options.cellSize, options.cellSize, options.cellSize);
                    DrawCell(cell, cells.GetArrayElementAtIndex(GetIndex(x, y, height.intValue)), objectType, options);
                }
            }
        }

        // The window repaints when the mouse goes from a cell to another, for the hovered cell to light up at once
        // rather than at the next repaint: it is asked the mouse moves when the mouse is over the grid
        static void RepaintOnHover(Rect area, int height, float cellSize)
        {
            Event current = Event.current;
            EditorWindow window = EditorWindow.mouseOverWindow;
            if (window == null)
            {
                return;
            }
            bool isOver = area.Contains(current.mousePosition);
            if (isOver && !window.wantsMouseMove)
            {
                window.wantsMouseMove = true;
            }
            if (current.type != EventType.MouseMove)
            {
                return;
            }
            int hovered = -1;
            if (isOver)
            {
                int x = Mathf.FloorToInt((current.mousePosition.x - area.x) / cellSize);
                int row = Mathf.FloorToInt((current.mousePosition.y - area.y) / cellSize);
                hovered = GetIndex(x, height - 1 - row, height);
            }
            if (hovered != _hoveredIndex)
            {
                _hoveredIndex = hovered;
                window.Repaint();
            }
        }

        static void DrawSize(SerializedProperty cells, SerializedProperty width, SerializedProperty height)
        {
            EditorGUILayout.BeginHorizontal();
            int newWidth = Mathf.Max(1, EditorGUILayout.IntField("Columns", width.intValue));
            int newHeight = Mathf.Max(1, EditorGUILayout.IntField("Rows", height.intValue));
            if (GUILayout.Button("Clear", GUILayout.Width(60f)))
            {
                for (int i = 0; i < cells.arraySize; i++)
                {
                    cells.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (newWidth == width.intValue && newHeight == height.intValue)
            {
                return;
            }

            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            for (int i = 0; i < cells.arraySize; i++)
            {
                objects.Add(cells.GetArrayElementAtIndex(i).objectReferenceValue);
            }
            List<UnityEngine.Object> resized = Resize(objects, width.intValue, height.intValue, newWidth, newHeight);
            cells.arraySize = resized.Count;
            for (int i = 0; i < resized.Count; i++)
            {
                cells.GetArrayElementAtIndex(i).objectReferenceValue = resized[i];
            }
            width.intValue = newWidth;
            height.intValue = newHeight;
        }

        // A list shorter than the grid gets empty cells
        static void EnsureSize(SerializedProperty cells, int count)
        {
            while (cells.arraySize < count)
            {
                cells.arraySize++;
                cells.GetArrayElementAtIndex(cells.arraySize - 1).objectReferenceValue = null;
            }
        }

        static void DrawCell(Rect rect, SerializedProperty cell, Type objectType, GridGUIOptions options)
        {
            Rect inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
            int id = GUIUtility.GetControlID(CellHash, FocusType.Passive, inner);
            UnityEngine.Object value = cell.objectReferenceValue;
            Event current = Event.current;
            bool isHovered = inner.Contains(current.mousePosition);
            // At the bottom: always on an empty cell, over the name of a filled one under the mouse
            Rect selectRect = PreviewField.GetSelectRect(new Vector2(inner.center.x, inner.yMax - 9f));
            bool showSelect = value == null || isHovered;

            if (current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(inner, value != null ? FilledColor : EmptyColor);
                if (isHovered)
                {
                    EditorGUI.DrawRect(inner, HoverColor);
                }
            }

            if (value != null)
            {
                Texture preview = options.getPreview?.Invoke(value) ?? AssetPreview.GetMiniThumbnail(value);
                Rect pictureRect = new Rect(inner.x + 4f, inner.y + 2f, inner.width - 8f, inner.height - 20f);
                if (preview != null)
                {
                    GUI.DrawTexture(pictureRect, preview, ScaleMode.ScaleToFit);
                }
                if (!isHovered)
                {
                    string label = options.getLabel?.Invoke(value) ?? value.name;
                    GUI.Label(new Rect(inner.x, inner.yMax - 16f, inner.width, 16f), new GUIContent(label, label), CenteredMiniLabel);
                }
            }

            if (showSelect && current.type == EventType.Repaint)
            {
                PreviewField.DrawSelectButton(selectRect);
            }
            if (PreviewField.TryTakePicked(id, objectType, out UnityEngine.Object picked))
            {
                cell.objectReferenceValue = picked;
            }

            HandleDragAndDrop(inner, cell, objectType, current);

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (!isHovered)
                    {
                        break;
                    }
                    if (current.button == 0 && showSelect && selectRect.Contains(current.mousePosition))
                    {
                        PreviewField.ShowPicker(objectType, value, id);
                        current.Use();
                    }
                    else if (current.button == 1 && value != null)
                    {
                        cell.objectReferenceValue = null;
                        current.Use();
                    }
                    else if (current.button == 0 && value != null)
                    {
                        // A click or the start of a drag: told apart when the mouse moves
                        GUIUtility.hotControl = id;
                        _pressPosition = current.mousePosition;
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && value != null && Vector2.Distance(current.mousePosition, _pressPosition) > DragThreshold)
                    {
                        // Drags the object to another cell or elsewhere
                        GUIUtility.hotControl = 0;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new[] { value };
                        DragAndDrop.SetGenericData(DragSourceKey, cell.propertyPath);
                        DragAndDrop.StartDrag(value.name);
                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        // A click shows where the object is in the Project window, without selecting it
                        GUIUtility.hotControl = 0;
                        if (value != null)
                        {
                            EditorGUIUtility.PingObject(value);
                        }
                        current.Use();
                    }
                    break;
            }
        }

        const string DragSourceKey = "Oisif.Inspector.GridGUI.Source";

        static void HandleDragAndDrop(Rect rect, SerializedProperty cell, Type objectType, Event current)
        {
            if ((current.type != EventType.DragUpdated && current.type != EventType.DragPerform) || !rect.Contains(current.mousePosition))
            {
                return;
            }

            UnityEngine.Object dropped = Array.Find(DragAndDrop.objectReferences, obj => obj != null && objectType.IsInstanceOfType(obj));
            if (dropped == null)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                // Moved from another cell of the same grid: the two cells swap their content
                string sourcePath = DragAndDrop.GetGenericData(DragSourceKey) as string;
                if (!string.IsNullOrEmpty(sourcePath) && sourcePath != cell.propertyPath)
                {
                    SerializedProperty source = cell.serializedObject.FindProperty(sourcePath);
                    if (source != null)
                    {
                        source.objectReferenceValue = cell.objectReferenceValue;
                    }
                }
                cell.objectReferenceValue = dropped;
            }
            current.Use();
        }

        static GUIStyle _centeredMiniLabel;
        static GUIStyle CenteredMiniLabel
        {
            get
            {
                if (_centeredMiniLabel == null)
                {
                    _centeredMiniLabel = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip };
                }
                return _centeredMiniLabel;
            }
        }
    }
}
