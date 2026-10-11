using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor
{
    // Draws the inspector of every asset and prefab component of the data folders once, a few per frame, and
    // logs the ones throwing: a quick check that the inspectors still work after a change
    public class InspectorCheckWindow : EditorWindow
    {
        const int ObjectsPerFrame = 250;

        readonly List<UnityEngine.Object> _objects = new List<UnityEngine.Object>();
        readonly List<string> _errors = new List<string>();
        int _next;
        Vector2 _scroll;

        public static bool isRunning { get; private set; }
        public static IReadOnlyList<string> lastErrors { get; private set; } = new List<string>();
        public static int lastChecked { get; private set; }
        // "120/990 SomeAsset": where the check is
        public static string progress { get; private set; } = "";

        [MenuItem("Tools/Oisif/Check Inspectors")]
        public static void Open()
        {
            InspectorCheckWindow window = GetWindow<InspectorCheckWindow>("Inspector Check");
            window.Collect();
            window.Show();
        }

        void Collect()
        {
            _objects.Clear();
            _errors.Clear();
            _failed.Clear();
            _next = 0;
            string[] folders = DataSnapshot.folders.Where(AssetDatabase.IsValidFolder).ToArray();
            foreach (string path in AssetDatabase.FindAssets("t:ScriptableObject", folders).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                _objects.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<ScriptableObject>());
            }
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", folders).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    _objects.AddRange(prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(component => component != null));
                }
            }
            isRunning = true;
        }

        void OnGUI()
        {
            if (_next < _objects.Count)
            {
                EditorGUILayout.LabelField($"Checking {_next}/{_objects.Count}...");
                // Drawn out of sight: only the exceptions matter. The same objects for every event of a frame (the
                // layout then the repaint), the next ones after the repaint
                GUILayout.BeginArea(new Rect(0f, 40f, position.width, 10000f));
                int end = Mathf.Min(_next + ObjectsPerFrame, _objects.Count);
                progress = $"{_next}/{_objects.Count} {(_objects[_next] != null ? _objects[_next].name : "")}";
                for (int i = _next; i < end; i++)
                {
                    Check(_objects[i], i);
                }
                GUILayout.EndArea();
                if (Event.current.type == EventType.Repaint)
                {
                    _next = end;
                }
                Repaint();
                if (_next >= _objects.Count)
                {
                    Finish();
                }
                return;
            }

            EditorGUILayout.LabelField($"{_objects.Count} inspectors drawn, {_errors.Count} errors");
            if (GUILayout.Button("Check again"))
            {
                Collect();
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (string error in _errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
            EditorGUILayout.EndScrollView();
        }

        // Editors kept from the layout to the repaint of the frame
        readonly Dictionary<int, UnityEditor.Editor> _editors = new Dictionary<int, UnityEditor.Editor>();
        // Objects already reported, once each
        readonly HashSet<int> _failed = new HashSet<int>();

        void Check(UnityEngine.Object obj, int index)
        {
            if (obj == null || _failed.Contains(index))
            {
                return;
            }

            UnityEditor.Editor editor = null;
            int groupDepth = 0;
            bool isLastEvent = Event.current.type == EventType.Repaint;
            try
            {
                if (!_editors.TryGetValue(index, out editor) || editor == null)
                {
                    editor = UnityEditor.Editor.CreateEditor(obj);
                    _editors[index] = editor;
                    ExpandAll(editor.serializedObject);
                }
                GUILayout.BeginVertical();
                groupDepth = 1;
                editor.OnInspectorGUI();
                GUILayout.EndVertical();
                groupDepth = 0;
            }
            catch (Exception exception)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                _errors.Add($"{path} | {obj.name} ({obj.GetType().Name}): {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
                _failed.Add(index);
                isLastEvent = true;
                if (groupDepth > 0)
                {
                    GUILayout.EndVertical();
                }
            }
            finally
            {
                if (isLastEvent && editor != null)
                {
                    _editors.Remove(index);
                    DestroyImmediate(editor);
                }
            }
        }

        // Every foldout open, the inline editors too, so every part of the inspector is drawn
        static void ExpandAll(SerializedObject serializedObject)
        {
            SerializedProperty property = serializedObject.GetIterator();
            while (property.Next(true))
            {
                property.isExpanded = true;
            }
        }

        void Finish()
        {
            isRunning = false;
            lastErrors = new List<string>(_errors);
            lastChecked = _objects.Count;
            Debug.Log($"[InspectorCheck] {_objects.Count} inspectors drawn, {_errors.Count} errors" + (_errors.Count > 0 ? "\n" + string.Join("\n\n", _errors.Take(10)) : ""));
        }
    }
}
