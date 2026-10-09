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
                        EditorGUILayout.HelpBox(text, MessageType.None);
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

            EditorGUI.indentLevel++;
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                DrawProperty(child.Copy());
            }
            EditorGUI.indentLevel--;
        }

        void DrawList(SerializedProperty list, GUIContent label, FieldInfo field, PropertyInfo info)
        {
            DrawDecorators(field);
            bool canCreate = field != null && field.IsDefined(typeof(CreateDataButtonAttribute), true);
            Type elementType = PropertyReflection.GetElementType(info.type);

            EditorGUILayout.BeginHorizontal();
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, $"{label.text} ({list.arraySize})", true);
            if (canCreate && elementType != null && GUILayout.Button(new GUIContent("+", "Create a new asset"), EditorStyles.miniButton, GUILayout.Width(24f)))
            {
                CreateIntoList(list, elementType);
            }
            else if (!canCreate && GUILayout.Button(new GUIContent("+", "Add an element"), EditorStyles.miniButton, GUILayout.Width(24f)))
            {
                AddElement(list);
                list.isExpanded = true;
            }
            EditorGUILayout.EndHorizontal();

            if (!list.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                if (!DrawKeyValue(element))
                {
                    DrawProperty(element, new GUIContent(GetElementLabel(element, i)));
                }
                EditorGUILayout.EndVertical();
                if (DrawElementButtons(list, i))
                {
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        static string GetElementLabel(SerializedProperty element, int index)
        {
            return element.propertyType == SerializedPropertyType.ObjectReference ? $"{index}" : $"Element {index}";
        }

        // The entries of a dictionary on one line: the key then the value
        bool DrawKeyValue(SerializedProperty element)
        {
            if (element.propertyType != SerializedPropertyType.Generic)
            {
                return false;
            }
            SerializedProperty key = element.FindPropertyRelative("key");
            SerializedProperty value = element.FindPropertyRelative("value");
            if (key == null || value == null || key.hasVisibleChildren && key.propertyType == SerializedPropertyType.Generic)
            {
                return false;
            }
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

        // Up, down and remove; true when the list changed
        static bool DrawElementButtons(SerializedProperty list, int index)
        {
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(20f)))
                {
                    list.MoveArrayElement(index, index - 1);
                    return true;
                }
            }
            using (new EditorGUI.DisabledScope(index == list.arraySize - 1))
            {
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(20f)))
                {
                    list.MoveArrayElement(index, index + 1);
                    return true;
                }
            }
            if (GUILayout.Button(new GUIContent("✕", "Remove from the list"), EditorStyles.miniButtonRight, GUILayout.Width(20f)))
            {
                RemoveElement(list, index);
                return true;
            }
            return false;
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

        void CreateIntoList(SerializedProperty list, Type elementType)
        {
            SerializedObject owner = list.serializedObject;
            string path = list.propertyPath;
            UnityEngine.Object host = owner.targetObject;
            string prefix = System.IO.Path.GetFileName(DataAssets.GetFolder(host));
            DataAssets.ShowTypeMenu(elementType, type =>
            {
                ScriptableObject asset = DataAssets.Create(type, host, prefix);
                owner.Update();
                SerializedProperty target = owner.FindProperty(path);
                target.arraySize++;
                target.GetArrayElementAtIndex(target.arraySize - 1).objectReferenceValue = asset;
                target.isExpanded = true;
                owner.ApplyModifiedProperties();
            });
        }

        void DrawObjectField(SerializedProperty property, GUIContent label, FieldInfo field, Type objectType)
        {
            DrawDecorators(field);
            if (!typeof(UnityEngine.Object).IsAssignableFrom(objectType))
            {
                objectType = typeof(UnityEngine.Object);
            }
            UnityEngine.Object value = property.objectReferenceValue;
            bool canInline = value != null && (field != null && field.IsDefined(typeof(InlineEditorAttribute), true) || value.GetType().IsDefined(typeof(InlineEditorAttribute), true));
            bool canCreate = field != null && field.IsDefined(typeof(CreateDataButtonAttribute), true);

            EditorGUILayout.BeginHorizontal();
            if (canInline)
            {
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
                EditorGUILayout.PropertyField(property, GUIContent.none);
            }
            else
            {
                EditorGUILayout.ObjectField(property, objectType, label);
            }

            if (canCreate)
            {
                if (value == null && GUILayout.Button(new GUIContent("+", "Create a new asset"), EditorStyles.miniButton, GUILayout.Width(24f)))
                {
                    SerializedObject owner = property.serializedObject;
                    string path = property.propertyPath;
                    UnityEngine.Object host = owner.targetObject;
                    DataAssets.ShowTypeMenu(objectType, type =>
                    {
                        ScriptableObject asset = DataAssets.Create(type, host);
                        owner.Update();
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

            PreviewAttribute preview = field?.GetCustomAttribute<PreviewAttribute>(true);
            if (preview != null && value != null)
            {
                DrawPreview(value, preview.size);
            }

            if (canInline && property.isExpanded)
            {
                DrawInline(value);
            }
        }

        static void DrawPreview(UnityEngine.Object value, float size)
        {
            UnityEngine.Object shown = value is IInspectorPreview preview && preview.previewObject != null ? preview.previewObject : value;
            Texture texture = AssetPreview.GetAssetPreview(shown) ?? AssetPreview.GetMiniThumbnail(shown);
            Rect rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false)));
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            }
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
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            try
            {
                editor.OnInspectorGUI();
            }
            finally
            {
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
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
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
            }
            else
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
                EditorGUI.indentLevel++;
                SerializedProperty child = property.Copy();
                SerializedProperty end = property.GetEndProperty();
                bool enterChildren = true;
                while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
                {
                    enterChildren = false;
                    DrawProperty(child.Copy());
                }
                EditorGUI.indentLevel--;
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
