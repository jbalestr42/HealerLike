using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oisif.Editor
{
    // An object field drawn as the picture of its object, like the sprite fields of Unity: an object dropped on it or
    // picked with its Select button, at the bottom, sets it, a click pings the object, a double click opens it, Delete clears it
    public static class PreviewField
    {
        static readonly int Hash = "OisifPreviewField".GetHashCode();
        static readonly MethodInfo ShowPickerMethod = typeof(EditorGUIUtility).GetMethod(nameof(EditorGUIUtility.ShowObjectPicker));
        static readonly GUIContent SelectContent = new GUIContent("Select", "Pick an object");
        static GUIStyle _selectStyle;

        // A mini button with its borders all around (the Select tab of Unity is cut for a corner)
        static GUIStyle selectStyle => _selectStyle ?? (_selectStyle = new GUIStyle(EditorStyles.miniButton) { fontSize = 9, fixedHeight = 14f, margin = new RectOffset(), padding = new RectOffset(4, 4, 0, 1) });

        public static void Draw(Rect rect, SerializedProperty property, Type objectType, Func<Object, Texture> getPreview)
        {
            int id = GUIUtility.GetControlID(Hash, FocusType.Keyboard, rect);
            Object value = property.objectReferenceValue;
            Event current = Event.current;
            // Centered at the bottom
            float selectWidth = selectStyle.CalcSize(SelectContent).x;
            Rect select = new Rect(rect.center.x - selectWidth / 2f, rect.yMax - 17f, selectWidth, 14f);

            switch (current.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    EditorStyles.objectFieldThumb.Draw(rect, GUIContent.none, id, DragAndDrop.activeControlID == id, rect.Contains(current.mousePosition));
                    Rect inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
                    if (value != null)
                    {
                        Texture texture = getPreview(value) ?? AssetPreview.GetMiniThumbnail(value);
                        if (texture != null)
                        {
                            GUI.DrawTexture(inner, texture, ScaleMode.ScaleToFit);
                        }
                    }
                    else
                    {
                        GUI.Label(inner, $"None\n({ObjectNames.NicifyVariableName(objectType.Name)})", EditorStyles.centeredGreyMiniLabel);
                    }
                    selectStyle.Draw(select, SelectContent, select.Contains(current.mousePosition), false, false, false);
                    break;

                case EventType.MouseDown:
                    if (current.button != 0 || !rect.Contains(current.mousePosition))
                    {
                        break;
                    }
                    GUIUtility.keyboardControl = id;
                    if (select.Contains(current.mousePosition))
                    {
                        ShowPicker(objectType, value, id);
                    }
                    else if (value != null && current.clickCount == 2)
                    {
                        AssetDatabase.OpenAsset(value);
                    }
                    else if (value != null)
                    {
                        EditorGUIUtility.PingObject(value);
                    }
                    current.Use();
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id && (current.keyCode == KeyCode.Delete || current.keyCode == KeyCode.Backspace))
                    {
                        property.objectReferenceValue = null;
                        GUI.changed = true;
                        current.Use();
                    }
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!rect.Contains(current.mousePosition))
                    {
                        break;
                    }
                    Object dropped = GetDropped(DragAndDrop.objectReferences, objectType);
                    if (dropped == null)
                    {
                        break;
                    }
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    DragAndDrop.activeControlID = id;
                    if (current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;
                        property.objectReferenceValue = dropped;
                        GUI.changed = true;
                    }
                    current.Use();
                    break;

                case EventType.ExecuteCommand:
                    if (current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == id)
                    {
                        property.objectReferenceValue = GetDropped(new[] { EditorGUIUtility.GetObjectPickerObject() }, objectType);
                        GUI.changed = true;
                        current.Use();
                    }
                    break;
            }

            // The name of the object when the mouse is over it
            GUI.Label(rect, new GUIContent("", value != null ? value.name : ""), GUIStyle.none);
        }

        // The first of the objects the field can hold: the object itself, the component of a game object, the sprite
        // of a texture; null when none can
        public static Object GetDropped(Object[] objects, Type objectType)
        {
            foreach (Object obj in objects)
            {
                if (obj == null)
                {
                    continue;
                }
                if (objectType.IsInstanceOfType(obj))
                {
                    return obj;
                }
                if (typeof(Component).IsAssignableFrom(objectType) && obj is GameObject gameObject && gameObject.GetComponent(objectType) != null)
                {
                    return gameObject.GetComponent(objectType);
                }
                if (objectType == typeof(Sprite) && obj is Texture2D)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(obj));
                    if (sprite != null)
                    {
                        return sprite;
                    }
                }
            }
            return null;
        }

        static void ShowPicker(Type objectType, Object value, int id)
        {
            ShowPickerMethod.MakeGenericMethod(objectType).Invoke(null, new object[] { value, false, "", id });
        }
    }
}
