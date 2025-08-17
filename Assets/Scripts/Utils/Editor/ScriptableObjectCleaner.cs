using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ScriptableObjectCleaner : EditorWindow
{
    private class SOEntry
    {
        public string path;
        public bool selected = true;
        public int refCount; // number of assets referencing this SO
        public bool hasMissingScript;
    }

    private readonly List<SOEntry> _candidates = new List<SOEntry>();
    private Vector2 _scroll;

    [MenuItem("Tools/ScriptableObject Cleaner")]
    public static void ShowWindow()
    {
        GetWindow<ScriptableObjectCleaner>("SO Cleaner");
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan & List"))
        {
            ScanProject();
        }
        GUI.enabled = _candidates.Count > 0;
        if (GUILayout.Button("Select All")) SetAll(true);
        if (GUILayout.Button("Deselect All")) SetAll(false);
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        if (_candidates.Count == 0)
        {
            EditorGUILayout.HelpBox("Click 'Scan & List' to find .asset ScriptableObjects that are unused or have missing scripts inside Assets/Data (files in 'Packages' and .uxml/.uss are ignored).", MessageType.Info);
            return;
        }

        GUILayout.Label($"{_candidates.Count} potentially unused/corrupted ScriptableObjects found:", EditorStyles.boldLabel);

        _scroll = GUILayout.BeginScrollView(_scroll);

        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("✔", GUILayout.Width(20));
        GUILayout.Label("Path", GUILayout.MinWidth(300), GUILayout.ExpandWidth(true));
        GUILayout.Label("Action", GUILayout.Width(70));
        GUILayout.Label("Info", GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();

        foreach (var entry in _candidates)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.textField);

            entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(20));

            EditorGUILayout.LabelField(entry.path, GUILayout.MinWidth(300), GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(entry.path);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }

            string label = $"refs: {entry.refCount}";
            if (entry.hasMissingScript) label += " | MISSING SCRIPT";
            GUILayout.Label(label, GUILayout.Width(180));

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.Space(8);

        GUI.enabled = _candidates.Any(c => c.selected);
        if (GUILayout.Button("Delete Selected ScriptableObjects"))
        {
            if (EditorUtility.DisplayDialog("Confirm Deletion",
                $"Permanently delete { _candidates.Count(c=>c.selected) } asset(s)?\nThis cannot be undone with Ctrl+Z.",
                "Delete", "Cancel"))
            {
                DeleteSelected();
            }
        }
        GUI.enabled = true;
    }

    private void SetAll(bool value)
    {
        foreach (var c in _candidates) c.selected = value;
    }

    private void ScanProject()
    {
        _candidates.Clear();

        string[] allGuids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets/Data" });
        var soPaths = new List<string>();
        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".asset"))
            {
                continue;
            }

            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj == null)
            {
                _candidates.Add(new SOEntry { path = path, selected = true, refCount = 0, hasMissingScript = true });
            }
            else
            {
                SerializedObject so = new SerializedObject(obj);
                SerializedProperty scriptProp = so.FindProperty("m_Script");
                if (scriptProp == null || scriptProp.objectReferenceValue == null)
                {
                    _candidates.Add(new SOEntry { path = path, selected = true, refCount = 0, hasMissingScript = true });
                }
                else
                {
                    soPaths.Add(path);
                }
            }
        }

        var reverse = new Dictionary<string, HashSet<string>>();
        for (int i = 0; i < allGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(allGuids[i]);
            if (string.IsNullOrEmpty(assetPath)
                || assetPath.EndsWith(".meta")
                || assetPath.EndsWith(".cs")
                || assetPath.EndsWith(".js")
                || assetPath.EndsWith(".boo"))
                continue;

            if (i % 200 == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Indexing References", $"Analyzing: {i}/{allGuids.Length}", i / (float)allGuids.Length))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }

            string[] deps;
            try
            {
                deps = AssetDatabase.GetDependencies(assetPath, true);
            }
            catch
            {
                continue;
            }

            foreach (var dep in deps)
            {
                if (!reverse.TryGetValue(dep, out var set))
                {
                    set = new HashSet<string>();
                    reverse[dep] = set;
                }
                set.Add(assetPath);
            }
        }
        EditorUtility.ClearProgressBar();

        foreach (var soPath in soPaths)
        {
            reverse.TryGetValue(soPath, out var referrers);
            int count = 0;
            if (referrers != null)
            {
                count = referrers.Contains(soPath) ? referrers.Count - 1 : referrers.Count;
            }

            if (count == 0)
            {
                _candidates.Add(new SOEntry { path = soPath, selected = true, refCount = 0, hasMissingScript = false });
            }
        }

        _candidates.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
    }

    private void DeleteSelected()
    {
        var toDelete = _candidates.Where(c => c.selected).Select(c => c.path).ToList();
        int fail = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < toDelete.Count; i++)
            {
                string p = toDelete[i];
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Deleting Assets", $"{i}/{toDelete.Count}", i / (float)toDelete.Count);
                }
                if (!AssetDatabase.DeleteAsset(p))
                {
                    fail++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        _candidates.RemoveAll(c => toDelete.Contains(c.path));

        if (fail > 0)
        {
            EditorUtility.DisplayDialog("Cleanup Completed", $"Deletion finished with {fail} failure(s).", "OK");
        }
    }
}
