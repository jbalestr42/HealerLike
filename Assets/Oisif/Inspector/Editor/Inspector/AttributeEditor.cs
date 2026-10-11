using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Oisif.Inspector;

namespace Oisif.Editor
{
    // Inspector drawing the Oisif Inspector attributes (InlineEditor, CreateDataButton, ShowIf, Grid, Button...).
    // Not registered for any type by itself: a project derives from it with the CustomEditor attribute it wants, e.g.
    // [CustomEditor(typeof(ScriptableObject), true, isFallback = true)] class DataInspector : AttributeEditor {}
    // The types of the assemblies not referencing the Oisif Inspector keep the default inspector
    public class AttributeEditor : UnityEditor.Editor
    {
        // Inline editors of the referenced objects, by object
        readonly Dictionary<UnityEngine.Object, UnityEditor.Editor> _inlineEditors = new Dictionary<UnityEngine.Object, UnityEditor.Editor>();

        static readonly Dictionary<Assembly, bool> UsesInspectorByAssembly = new Dictionary<Assembly, bool>();

        // Objects drawn inline above this one, against an object drawing itself forever
        static readonly List<UnityEngine.Object> InlineStack = new List<UnityEngine.Object>();
        const int MaxInlineDepth = 6;

        // Frames being drawn, one inside the other: the second, the fourth... are darker
        static int FrameDepth;
        static readonly Color DarkerFrame = new Color(0f, 0f, 0f, 0.12f);

        // True while the object is drawn inside the inspector of another one
        public static bool isDrawingInline => InlineStack.Count > 0;

        public static bool UsesOisifInspector(Type type)
        {
            Assembly assembly = type.Assembly;
            if (!UsesInspectorByAssembly.TryGetValue(assembly, out bool usesKit))
            {
                string kit = typeof(InlineEditorAttribute).Assembly.GetName().Name;
                usesKit = assembly == typeof(InlineEditorAttribute).Assembly || assembly.GetReferencedAssemblies().Any(name => name.Name == kit);
                UsesInspectorByAssembly[assembly] = usesKit;
            }
            return usesKit;
        }

        protected virtual void OnDisable()
        {
            foreach (UnityEditor.Editor editor in _inlineEditors.Values)
            {
                if (editor != null)
                {
                    DestroyImmediate(editor);
                }
            }
            _inlineEditors.Clear();
        }

        public override void OnInspectorGUI()
        {
            if (target == null || !UsesOisifInspector(target.GetType()))
            {
                DrawDefaultInspector();
                return;
            }

            serializedObject.Update();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script")
                {
                    if (!isDrawingInline)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.PropertyField(iterator);
                        }
                    }
                    continue;
                }
                DrawProperty(iterator.Copy());
            }
            serializedObject.ApplyModifiedProperties();

            DrawShownMembers();
            DrawButtons();

            if (AssetPreview.IsLoadingAssetPreviews())
            {
                Repaint();
            }
        }

        // A property with the attributes of its field, then its children for a class, its elements for a list
        public void DrawProperty(SerializedProperty property, GUIContent label = null)
        {
            PropertyInfo info = PropertyReflection.Find(property);
            FieldInfo field = info.field;
            label = label ?? new GUIContent(property.displayName, property.tooltip);

            if (field != null)
            {
                if (isDrawingInline && field.IsDefined(typeof(HideInInlineEditorsAttribute), true) || !IsShown(field, info.owner))
                {
                    return;
                }
                InfoBoxAttribute infoBox = field.GetCustomAttribute<InfoBoxAttribute>(true);
                if (infoBox != null)
                {
                    string text = PropertyReflection.GetMemberValue(info.owner, infoBox.member, out _) as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        InfoBoxGUI.Draw(text);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(field != null && field.IsDefined(typeof(ReadOnlyAttribute), true)))
            {
                GridAttribute grid = field?.GetCustomAttribute<GridAttribute>(true);
                if (grid != null && property.isArray)
                {
                    DrawGrid(property, grid, info);
                }
                else if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    DrawList(property, label, field, info);
                }
                else if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    DrawObjectField(property, label, field, PropertyReflection.Find(property).type ?? typeof(UnityEngine.Object));
                }
                else if (property.propertyType == SerializedPropertyType.ManagedReference)
                {
                    DrawManagedReference(property, label, field);
                }
                else if (property.propertyType == SerializedPropertyType.Generic && field != null && field.IsDefined(typeof(InlinePropertyAttribute), true))
                {
                    DrawDecorators(field);
                    DrawChildrenInPlace(property);
                }
                else if (property.propertyType == SerializedPropertyType.Generic)
                {
                    DrawDecorators(field);
                    DrawChildren(property, label);
                }
                else
                {
                    // Unity draws the headers, the spaces and the property drawers of the simple values
                    EditorGUILayout.PropertyField(property, label, true);
                }
            }
        }

        static bool IsShown(FieldInfo field, object owner)
        {
            foreach (ShowIfAttribute condition in field.GetCustomAttributes<ShowIfAttribute>(true))
            {
                object value = PropertyReflection.GetMemberValue(owner, condition.member, out bool found);
                if (!found)
                {
                    continue;
                }
                bool isMet = condition.hasValue ? Equals(value, condition.value) : PropertyReflection.IsTruthy(value);
                bool isHide = condition is HideIfAttribute;
                if (isMet == isHide)
                {
                    return false;
                }
            }
            return true;
        }

        // The headers and spaces of a field Unity doesn't draw itself
        static void DrawDecorators(FieldInfo field)
        {
            if (field == null)
            {
                return;
            }
            foreach (SpaceAttribute space in field.GetCustomAttributes<SpaceAttribute>(true))
            {
                GUILayout.Space(space.height);
            }
            foreach (HeaderAttribute header in field.GetCustomAttributes<HeaderAttribute>(true))
            {
                EditorGUILayout.LabelField(header.header, EditorStyles.boldLabel);
            }
        }

        void DrawChildren(SerializedProperty property, GUIContent label)
        {
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                return;
            }

            DrawFramedChildren(property);
        }

        // The fields of a nested object in a frame, to see which object they belong to
        void DrawFramedChildren(SerializedProperty property)
        {
            int indent = BeginFrame();
            try
            {
                DrawChildrenInPlace(property);
            }
            finally
            {
                EndFrame(indent);
            }
        }

        void DrawChildrenInPlace(SerializedProperty property)
        {
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                DrawProperty(child.Copy());
            }
        }

        // A frame shifted by the indent, its content starting again from no indent, so that the frames nest
        static int BeginFrame()
        {
            int indent = EditorGUI.indentLevel;
            // Apart from the field it opens
            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 15f + 12f);
            Rect frame = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            FrameDepth++;
            if (FrameDepth % 2 == 0 && Event.current.type == EventType.Repaint)
            {
                // Inside the border of the box
                EditorGUI.DrawRect(new Rect(frame.x + 1f, frame.y + 1f, frame.width - 2f, frame.height - 2f), DarkerFrame);
            }
            EditorGUI.indentLevel = 0;
            return indent;
        }

        static void EndFrame(int indent)
        {
            FrameDepth--;
            EditorGUI.indentLevel = indent;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            // Apart from the field after the frame
            GUILayout.Space(4f);
        }

        // A list looking like the ones of Unity: a header with the foldout and the size, the elements in a box with a
        // handle to drag them, a footer to add one and remove the selected one
        void DrawList(SerializedProperty list, GUIContent label, FieldInfo field, PropertyInfo info)
        {
            DrawDecorators(field);
            bool canCreate = field != null && field.IsDefined(typeof(CreateDataButtonAttribute), true);
            Type elementType = PropertyReflection.GetElementType(info.type);
            ListStyles styles = ListStyles.instance;
            int indentLevel = EditorGUI.indentLevel;
            float indent = indentLevel * 15f;

            Rect header = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            header.xMin += indent;
            if (Event.current.type == EventType.Repaint)
            {
                styles.header.Draw(header, false, false, false, false);
                if (!list.isExpanded)
                {
                    // The header alone has no bottom border
                    EditorGUI.DrawRect(new Rect(header.x + 1f, header.yMax - 1f, header.width - 2f, 1f), styles.borderColor);
                }
            }
            EditorGUI.indentLevel = 0;
            try
            {
                Rect foldout = new Rect(header.x + 18f, header.y + 1f, header.width - 76f, EditorGUIUtility.singleLineHeight);
                list.isExpanded = EditorGUI.Foldout(foldout, list.isExpanded, label, true);
                // A margin around it, inside the header
                Rect size = new Rect(header.xMax - 56f, header.y + 2f, 48f, header.height - 4f);
                int newSize = EditorGUI.DelayedIntField(size, list.arraySize);
                if (newSize != list.arraySize)
                {
                    SetSize(list, Mathf.Max(0, newSize));
                }
            }
            finally
            {
                EditorGUI.indentLevel = indentLevel;
            }

            if (list.isExpanded)
            {
                Rect box = DrawListElements(list, header, indent, styles);
                Rect footer = DrawListFooter(list, box, canCreate, elementType, styles);
                // Only the handle selects an element: any other click, even used by a field, deselects it, but the
                // footer, whose - removes it
                if (Event.current.rawType == EventType.MouseDown && ListDrag.GetDraggedIndex(list) < 0 && !footer.Contains(Event.current.mousePosition))
                {
                    SetSelected(list, -1);
                }
            }
            // Apart from the field after the list
            GUILayout.Space(4f);
        }

        // The rect of the box holding the elements
        Rect DrawListElements(SerializedProperty list, Rect header, float indent, ListStyles styles)
        {
            int indentLevel = EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            Rect box = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                // As wide as the header, the layout giving a few pixels less
                styles.background.Draw(new Rect(header.x, box.y, header.width, box.height), false, false, false, false);
            }
            EditorGUI.indentLevel = 0;
            try
            {
                GUILayout.Space(3f);
                if (list.arraySize == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(6f);
                    EditorGUILayout.LabelField("List is Empty");
                    EditorGUILayout.EndHorizontal();
                }

                // The order of a dictionary doesn't matter: no handle
                bool canReorder = list.arraySize == 0 || !IsKeyValue(list.GetArrayElementAtIndex(0));
                int selected = GetSelected(list);
                List<Rect> rects = new List<Rect>();
                for (int i = 0; i < list.arraySize; i++)
                {
                    SerializedProperty element = list.GetArrayElementAtIndex(i);
                    Rect row = EditorGUILayout.BeginHorizontal();
                    rects.Add(row);
                    if (Event.current.type == EventType.Repaint && (i == selected || i == ListDrag.GetDraggedIndex(list)))
                    {
                        styles.element.Draw(row, false, true, true, true);
                    }
                    GUILayout.Space(2f);
                    if (canReorder)
                    {
                        ListDrag.Handle(list, i);
                        if (ListDrag.GetDraggedIndex(list) == i)
                        {
                            SetSelected(list, i);
                        }
                    }
                    else
                    {
                        GUILayout.Space(ListDrag.HandleWidth);
                    }
                    EditorGUILayout.BeginVertical();
                    // The fields leave a margin above them only: a bit less above, more under them, for the selection
                    // to frame them
                    GUILayout.Space(-1f);
                    if (!DrawKeyValue(element))
                    {
                        DrawProperty(element, GetElementLabel(element));
                    }
                    GUILayout.Space(3f);
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(4f);
                    EditorGUILayout.EndHorizontal();
                }
                if (canReorder && ListDrag.End(list, rects))
                {
                    SetSelected(list, ListDrag.lastDropIndex);
                }
                // As much space under the last element as above the first one, the elements leaving 3 pixels under
                // them
                GUILayout.Space(list.arraySize > 0 ? 1f : 3f);
            }
            finally
            {
                EditorGUI.indentLevel = indentLevel;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            return new Rect(header.x, box.y, header.width, box.height);
        }

        // + to add an element (or create an asset), - to remove the selected one, the last one without selection;
        // the rect of the footer
        Rect DrawListFooter(SerializedProperty list, Rect box, bool canCreate, Type elementType, ListStyles styles)
        {
            GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            // Under the right of the box, whatever the space the layout leaves between them
            Rect footer = new Rect(box.xMax - 58f, box.yMax, 58f, 20f);
            if (Event.current.type == EventType.Repaint)
            {
                styles.footer.Draw(footer, false, false, false, false);
            }

            Rect add = new Rect(footer.x + 4f, footer.y, 25f, 16f);
            Rect remove = new Rect(footer.xMax - 29f, footer.y, 25f, 16f);
            using (new EditorGUI.DisabledScope(canCreate && elementType == null))
            {
                if (canCreate)
                {
                    // Opens its menu as soon as it is pressed, like the dropdown buttons of Unity
                    if (EditorGUI.DropdownButton(add, styles.createContent, FocusType.Passive, styles.footerButton))
                    {
                        CreateIntoList(list, elementType, add);
                    }
                }
                else if (GUI.Button(add, styles.addContent, styles.footerButton))
                {
                    AddElement(list);
                    SetSelected(list, list.arraySize - 1);
                }
            }
            using (new EditorGUI.DisabledScope(list.arraySize == 0))
            {
                if (GUI.Button(remove, styles.removeContent, styles.footerButton))
                {
                    int selected = GetSelected(list);
                    int index = selected >= 0 && selected < list.arraySize ? selected : list.arraySize - 1;
                    RemoveElement(list, index);
                    SetSelected(list, Mathf.Min(index, list.arraySize - 1));
                }
            }
            return footer;
        }

        static void SetSize(SerializedProperty list, int size)
        {
            while (list.arraySize < size)
            {
                AddElement(list);
            }
            while (list.arraySize > size)
            {
                RemoveElement(list, list.arraySize - 1);
            }
        }

        // The selected element of each list shown, by object and path
        static readonly Dictionary<(UnityEngine.Object, string), int> SelectedByList = new Dictionary<(UnityEngine.Object, string), int>();

        static int GetSelected(SerializedProperty list)
        {
            return SelectedByList.TryGetValue((list.serializedObject.targetObject, list.propertyPath), out int index) ? index : -1;
        }

        static void SetSelected(SerializedProperty list, int index)
        {
            SelectedByList[(list.serializedObject.targetObject, list.propertyPath)] = index;
        }

        // The styles of the lists of Unity
        class ListStyles
        {
            static ListStyles _instance;
            public static ListStyles instance => _instance ?? (_instance = new ListStyles());

            public readonly GUIStyle header = "RL Header";
            public readonly Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.14f, 0.14f, 0.14f) : new Color(0.6f, 0.6f, 0.6f);
            public readonly GUIStyle background = "RL Background";
            public readonly GUIStyle element = "RL Element";
            public readonly GUIStyle footer = "RL Footer";
            public readonly GUIStyle footerButton = "RL FooterButton";
            public readonly GUIContent addContent = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to the list");
            public readonly GUIContent createContent = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Create a new asset");
            public readonly GUIContent removeContent = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove the selected element from the list");
        }

        // No index: an object or a value alone, a class by its type (its foldout needs a label)
        static GUIContent GetElementLabel(SerializedProperty element)
        {
            if (element.propertyType == SerializedPropertyType.Generic)
            {
                Type type = PropertyReflection.Find(element).type;
                return new GUIContent(type != null ? DataAssets.GetNiceName(type) : "Element");
            }
            return GUIContent.none;
        }

        // A foldout arrow alone when there is no label, the label clickable otherwise
        static bool DrawFoldout(bool isExpanded, GUIContent label)
        {
            if (label == null || label == GUIContent.none || string.IsNullOrEmpty(label.text))
            {
                // Unity draws the arrow shifted by the indent but clicks it at its rect: the indent is put in the
                // rect, the arrow drawn without indent
                float indent = EditorGUI.indentLevel * 15f;
                Rect rect = GUILayoutUtility.GetRect(indent + 14f, EditorGUIUtility.singleLineHeight, GUILayout.Width(indent + 14f));
                rect.xMin += indent;
                int indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                bool expanded = EditorGUI.Foldout(rect, isExpanded, GUIContent.none, true);
                EditorGUI.indentLevel = indentLevel;
                return expanded;
            }
            return EditorGUILayout.Foldout(isExpanded, label, true);
        }

        // A field without label after the arrow, which already holds the indent
        static void DrawFieldAfterFoldout(SerializedProperty property)
        {
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUILayout.PropertyField(property, GUIContent.none);
            EditorGUI.indentLevel = indentLevel;
        }

        // An entry of a dictionary: a key, simple, and a value
        static bool IsKeyValue(SerializedProperty element)
        {
            if (element.propertyType != SerializedPropertyType.Generic)
            {
                return false;
            }
            SerializedProperty key = element.FindPropertyRelative("key");
            SerializedProperty value = element.FindPropertyRelative("value");
            return key != null && value != null && !(key.hasVisibleChildren && key.propertyType == SerializedPropertyType.Generic);
        }

        // The entries of a dictionary on one line: the key then the value
        bool DrawKeyValue(SerializedProperty element)
        {
            if (!IsKeyValue(element))
            {
                return false;
            }
            SerializedProperty key = element.FindPropertyRelative("key");
            SerializedProperty value = element.FindPropertyRelative("value");
            if (value.propertyType == SerializedPropertyType.Generic)
            {
                EditorGUILayout.PropertyField(key, GUIContent.none);
                EditorGUI.indentLevel++;
                DrawChildren(value, new GUIContent("Value"));
                EditorGUI.indentLevel--;
                return true;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(key, GUIContent.none);
            EditorGUILayout.PropertyField(value, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            return true;
        }

        // A new element at the end: empty, not a copy of the last one
        static void AddElement(SerializedProperty list)
        {
            list.arraySize++;
            SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);
            if (element.propertyType == SerializedPropertyType.ObjectReference)
            {
                element.objectReferenceValue = null;
            }
            else if (element.propertyType == SerializedPropertyType.ManagedReference)
            {
                element.managedReferenceValue = null;
            }
        }

        static void RemoveElement(SerializedProperty list, int index)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            // An object reference is first set to null, then removed
            if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
            {
                element.objectReferenceValue = null;
            }
            list.DeleteArrayElementAtIndex(index);
        }

        // The menu of the types, under the button; the asset picked is created then added at the end of the list
        static void CreateIntoList(SerializedProperty list, Type elementType, Rect button)
        {
            string path = list.propertyPath;
            UnityEngine.Object host = list.serializedObject.targetObject;
            string prefix = System.IO.Path.GetFileName(DataAssets.GetFolder(host));
            DataAssets.ShowTypeMenu(elementType, type =>
            {
                ScriptableObject asset = DataAssets.Create(type, host, prefix);
                // A new SerializedObject: creating the asset can rebuild the inspectors, disposing the one of the list
                SerializedObject owner = new SerializedObject(host);
                SerializedProperty target = owner.FindProperty(path);
                target.arraySize++;
                target.GetArrayElementAtIndex(target.arraySize - 1).objectReferenceValue = asset;
                target.isExpanded = true;
                owner.ApplyModifiedProperties();
            }, button);
        }

        void DrawObjectField(SerializedProperty property, GUIContent label, FieldInfo field, Type objectType)
        {
            DrawDecorators(field);
            if (!typeof(UnityEngine.Object).IsAssignableFrom(objectType))
            {
                objectType = typeof(UnityEngine.Object);
            }
            PreviewAttribute preview = field?.GetCustomAttribute<PreviewAttribute>(true);
            if (preview != null)
            {
                DrawPreviewField(property, label, objectType, preview.size);
                return;
            }
            UnityEngine.Object value = property.objectReferenceValue;
            bool canInline = value != null && (field != null && field.IsDefined(typeof(InlineEditorAttribute), true) || value.GetType().IsDefined(typeof(InlineEditorAttribute), true));
            bool canCreate = field != null && field.IsDefined(typeof(CreateDataButtonAttribute), true);

            EditorGUILayout.BeginHorizontal();
            if (canInline)
            {
                property.isExpanded = DrawFoldout(property.isExpanded, label);
                if (label == GUIContent.none || string.IsNullOrEmpty(label.text))
                {
                    DrawFieldAfterFoldout(property);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, GUIContent.none);
                }
            }
            else
            {
                EditorGUILayout.ObjectField(property, objectType, label);
            }

            if (canCreate)
            {
                if (value == null && GUILayout.Button(new GUIContent("+", "Create a new asset"), EditorStyles.miniButton, GUILayout.Width(24f)))
                {
                    string path = property.propertyPath;
                    UnityEngine.Object host = property.serializedObject.targetObject;
                    DataAssets.ShowTypeMenu(objectType, type =>
                    {
                        ScriptableObject asset = DataAssets.Create(type, host);
                        // A new SerializedObject: creating the asset can rebuild the inspectors, disposing the one of the field
                        SerializedObject owner = new SerializedObject(host);
                        SerializedProperty target = owner.FindProperty(path);
                        target.objectReferenceValue = asset;
                        target.isExpanded = true;
                        owner.ApplyModifiedProperties();
                    });
                }
                else if (value != null && GUILayout.Button(new GUIContent("✕", "Delete the asset"), EditorStyles.miniButton, GUILayout.Width(24f)))
                {
                    UnityEngine.Object deleted = value;
                    property.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                    if (!DataAssets.Delete(deleted))
                    {
                        property.objectReferenceValue = deleted;
                    }
                    EditorGUILayout.EndHorizontal();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();


            if (canInline && property.isExpanded)
            {
                DrawInline(value);
            }
        }

        // The field is its preview, after its label
        static void DrawPreviewField(SerializedProperty property, GUIContent label, Type objectType, float size)
        {
            EditorGUILayout.BeginHorizontal();
            if (label != GUIContent.none && !string.IsNullOrEmpty(label.text))
            {
                EditorGUILayout.PrefixLabel(label);
            }
            else
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
            }
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            PreviewField.Draw(rect, property, objectType, GetPreviewTexture);
        }

        void DrawInline(UnityEngine.Object value)
        {
            if (InlineStack.Count >= MaxInlineDepth || InlineStack.Contains(value) || value == target)
            {
                EditorGUILayout.HelpBox("Already shown above", MessageType.None);
                return;
            }

            if (!_inlineEditors.TryGetValue(value, out UnityEditor.Editor editor) || editor == null)
            {
                editor = CreateEditor(value);
                _inlineEditors[value] = editor;
            }

            InlineStack.Add(value);
            int indent = BeginFrame();
            try
            {
                editor.OnInspectorGUI();
            }
            finally
            {
                EndFrame(indent);
                InlineStack.Remove(value);
            }
        }

        // A value of a [SerializeReference] field: a menu of the types it can take, then its fields
        void DrawManagedReference(SerializedProperty property, GUIContent label, FieldInfo field)
        {
            DrawDecorators(field);
            Type fieldType = field != null ? field.FieldType : null;
            if (fieldType != null && typeof(System.Collections.IList).IsAssignableFrom(fieldType))
            {
                fieldType = PropertyReflection.GetElementType(fieldType);
            }
            object value = property.managedReferenceValue;
            string current = value != null ? DataAssets.GetNiceName(value.GetType()) : "None";

            EditorGUILayout.BeginHorizontal();
            if (value != null)
            {
                property.isExpanded = DrawFoldout(property.isExpanded, label);
            }
            else if (label != GUIContent.none && !string.IsNullOrEmpty(label.text))
            {
                EditorGUILayout.PrefixLabel(label);
            }
            if (fieldType != null && GUILayout.Button(current, EditorStyles.popup))
            {
                ShowManagedTypeMenu(property, fieldType);
            }
            EditorGUILayout.EndHorizontal();

            if (value != null && property.isExpanded)
            {
                DrawFramedChildren(property);
            }
        }

        static void ShowManagedTypeMenu(SerializedProperty property, Type fieldType)
        {
            SerializedObject owner = property.serializedObject;
            string path = property.propertyPath;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), false, () => SetManagedReference(owner, path, null));
            IEnumerable<Type> types = TypeCache.GetTypesDerivedFrom(fieldType).Append(fieldType)
                .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && !typeof(UnityEngine.Object).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null)
                .Distinct()
                .OrderBy(type => type.Name, StringComparer.Ordinal);
            foreach (Type type in types)
            {
                menu.AddItem(new GUIContent(DataAssets.GetNiceName(type)), false, () => SetManagedReference(owner, path, Activator.CreateInstance(type)));
            }
            menu.ShowAsContext();
        }

        static void SetManagedReference(SerializedObject owner, string path, object value)
        {
            owner.Update();
            SerializedProperty property = owner.FindProperty(path);
            property.managedReferenceValue = value;
            property.isExpanded = value != null;
            owner.ApplyModifiedProperties();
        }

        void DrawGrid(SerializedProperty cells, GridAttribute grid, PropertyInfo info)
        {
            string parent = cells.propertyPath.Contains(".") ? cells.propertyPath.Substring(0, cells.propertyPath.LastIndexOf('.') + 1) : "";
            SerializedProperty width = cells.serializedObject.FindProperty(parent + grid.widthField);
            SerializedProperty height = cells.serializedObject.FindProperty(parent + grid.heightField);
            Type elementType = PropertyReflection.GetElementType(info.type) ?? typeof(UnityEngine.Object);
            if (width == null || height == null)
            {
                EditorGUILayout.HelpBox($"Grid: no field {grid.widthField} or {grid.heightField}", MessageType.Warning);
                return;
            }

            GridGUI.Draw(cells, width, height, elementType, new GridGUIOptions
            {
                cellSize = grid.cellSize,
                firstColumnLabel = grid.firstColumnLabel,
                getPreview = GetPreviewTexture,
                getLabel = obj => obj is IInspectorPreview preview && !string.IsNullOrEmpty(preview.previewLabel) ? preview.previewLabel : obj.name,
            });
        }

        static Texture GetPreviewTexture(UnityEngine.Object obj)
        {
            UnityEngine.Object shown = obj is IInspectorPreview preview && preview.previewObject != null ? preview.previewObject : obj;
            return AssetPreview.GetAssetPreview(shown);
        }

        // The [ShowInInspector] members that Unity doesn't serialize, read only
        void DrawShownMembers()
        {
            List<MemberInfo> members = GetShownMembers(target.GetType());
            if (members.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
            {
                foreach (MemberInfo member in members)
                {
                    object value = member is FieldInfo field ? field.GetValue(target) : ((System.Reflection.PropertyInfo)member).GetValue(target);
                    EditorGUILayout.TextField(ObjectNames.NicifyVariableName(member.Name), value != null ? value.ToString() : "null");
                }
            }
        }

        static List<MemberInfo> GetShownMembers(Type type)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            for (Type current = type; current != null && current != typeof(MonoBehaviour) && current != typeof(ScriptableObject); current = current.BaseType)
            {
                foreach (MemberInfo member in current.GetMembers(PropertyReflection.InstanceMembers | BindingFlags.DeclaredOnly))
                {
                    if ((member is FieldInfo || member is System.Reflection.PropertyInfo) && member.IsDefined(typeof(ShowInInspectorAttribute), true))
                    {
                        members.Add(member);
                    }
                }
            }
            return members;
        }

        void DrawButtons()
        {
            for (Type type = target.GetType(); type != null && type != typeof(MonoBehaviour) && type != typeof(ScriptableObject); type = type.BaseType)
            {
                foreach (MethodInfo method in type.GetMethods(PropertyReflection.InstanceMembers | BindingFlags.DeclaredOnly))
                {
                    ButtonAttribute button = method.GetCustomAttribute<ButtonAttribute>(true);
                    if (button == null || method.GetParameters().Length > 0)
                    {
                        continue;
                    }

                    using (new EditorGUI.DisabledScope(button.playModeOnly && !Application.isPlaying))
                    {
                        if (GUILayout.Button(button.label ?? ObjectNames.NicifyVariableName(method.Name)))
                        {
                            foreach (UnityEngine.Object edited in targets)
                            {
                                Undo.RecordObject(edited, method.Name);
                                method.Invoke(edited, null);
                                EditorUtility.SetDirty(edited);
                            }
                        }
                    }
                }
            }
        }
    }
}
