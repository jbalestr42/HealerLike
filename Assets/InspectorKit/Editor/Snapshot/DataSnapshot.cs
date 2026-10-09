using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InspectorKit.Editor
{
    // Photo of the data of the project: every ScriptableObject (main and sub-assets) and every component of the
    // prefabs under the folders, written as sorted "asset | object | field = value" lines. Taken before and after a
    // change of serializer, the two files must be the same
    public static class DataSnapshot
    {
        const string FoldersKey = "InspectorKit.DataSnapshot.Folders";
        public const string OutputFolder = "Logs/DataSnapshots";

        // Folders photographed, separated by ';', saved in the editor preferences
        public static string[] folders
        {
            get => EditorPrefs.GetString(FoldersKey, "Assets/Data;Assets/Prefabs").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            set => EditorPrefs.SetString(FoldersKey, string.Join(";", value));
        }

        [MenuItem("Tools/Inspector Kit/Data Snapshot/Take")]
        static void TakeFromMenu()
        {
            string path = Take();
            Debug.Log($"[DataSnapshot] written to {path}");
        }

        [MenuItem("Tools/Inspector Kit/Data Snapshot/Compare The Last Two")]
        static void CompareFromMenu()
        {
            List<string> files = Directory.Exists(OutputFolder)
                ? Directory.GetFiles(OutputFolder, "*.txt").OrderBy(file => file, StringComparer.Ordinal).ToList()
                : new List<string>();
            if (files.Count < 2)
            {
                Debug.LogWarning("[DataSnapshot] take two snapshots first");
                return;
            }
            Debug.Log(FormatDiff(Compare(files[files.Count - 2], files[files.Count - 1])));
        }

        // Writes the snapshot of the folders in OutputFolder, returns its path
        public static string Take()
        {
            List<string> lines = Collect(folders);
            Directory.CreateDirectory(OutputFolder);
            string path = Path.Combine(OutputFolder, $"snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllLines(path, lines);
            return path;
        }

        public static List<string> Collect(IEnumerable<string> searchFolders)
        {
            SnapshotWriter writer = new SnapshotWriter { describeReference = DescribeReference };
            List<string> lines = new List<string>();
            string[] existing = searchFolders.Where(AssetDatabase.IsValidFolder).ToArray();
            if (existing.Length == 0)
            {
                return lines;
            }

            foreach (string assetPath in AssetDatabase.FindAssets("t:ScriptableObject", existing).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (asset is ScriptableObject)
                    {
                        AddLines(lines, writer, asset, $"{assetPath} | {asset.name} ({asset.GetType().Name})");
                    }
                }
            }

            foreach (string prefabPath in AssetDatabase.FindAssets("t:Prefab", existing).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }
                foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component != null)
                    {
                        AddLines(lines, writer, component, $"{prefabPath} | {GetHierarchyPath(component.transform)} ({component.GetType().Name})");
                    }
                }
            }

            lines.Sort(StringComparer.Ordinal);
            return lines;
        }

        static void AddLines(List<string> lines, SnapshotWriter writer, UnityEngine.Object obj, string prefix)
        {
            foreach (string line in writer.Write(obj))
            {
                lines.Add($"{prefix} | {line}");
            }
        }

        // An asset by its path, a sub-asset or a prefab part also by its name or place
        public static string DescribeReference(UnityEngine.Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                return obj is Component component ? GetHierarchyPath(component.transform) : obj.name;
            }
            if (obj is Component part)
            {
                return $"{path}/{GetHierarchyPath(part.transform)}";
            }
            if (obj is GameObject gameObject && !AssetDatabase.IsMainAsset(obj))
            {
                return $"{path}/{GetHierarchyPath(gameObject.transform)}";
            }
            return AssetDatabase.IsMainAsset(obj) ? path : $"{path}#{obj.name}";
        }

        static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }
            return path;
        }

        // Lines only in the first file ("- ") then only in the second one ("+ ")
        public static List<string> Compare(string beforePath, string afterPath)
        {
            return Diff(File.ReadAllLines(beforePath), File.ReadAllLines(afterPath));
        }

        public static List<string> Diff(IEnumerable<string> before, IEnumerable<string> after)
        {
            HashSet<string> beforeSet = new HashSet<string>(before);
            HashSet<string> afterSet = new HashSet<string>(after);
            List<string> diff = beforeSet.Where(line => !afterSet.Contains(line)).OrderBy(line => line, StringComparer.Ordinal).Select(line => "- " + line).ToList();
            diff.AddRange(afterSet.Where(line => !beforeSet.Contains(line)).OrderBy(line => line, StringComparer.Ordinal).Select(line => "+ " + line));
            return diff;
        }

        static string FormatDiff(List<string> diff)
        {
            if (diff.Count == 0)
            {
                return "[DataSnapshot] the two snapshots are the same";
            }
            return $"[DataSnapshot] {diff.Count} different lines:\n" + string.Join("\n", diff.Take(200));
        }
    }
}
