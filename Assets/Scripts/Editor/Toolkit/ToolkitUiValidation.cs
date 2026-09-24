using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Editor.Toolkit
{
    public static class ToolkitUiValidation
    {
        // Usable from -executeMethod after importing the copied project.
        public static void ValidateAssets()
        {
            var assets = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { "Assets/Resources/UI/Toolkit" });
            if (assets.Length == 0) throw new InvalidOperationException("UI Toolkit layout assets are missing.");
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                var root = tree.CloneTree();
                if (root.childCount == 0) throw new InvalidOperationException("Empty UI layout: " + path);
                Debug.Log("Validated UI Toolkit layout: " + path);
            }
            foreach (var path in new[] { ToolkitUiInstaller.MenuPath, ToolkitUiInstaller.GameplayPath })
            {
                if (!File.Exists(path)) throw new InvalidOperationException("Missing demo scene: " + path);
            }
            var catalog = Resources.Load<HealerLike.UI.Toolkit.Icons.DataIconCatalog>("UIToolkit/DataIconCatalog");
            if (catalog == null) throw new InvalidOperationException("Generated icon catalog is missing.");
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }))
                foreach (var source in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<ScriptableObject>())
                    if (HealerLike.UI.Toolkit.Editor.DataIconBaker.IsGameData(source))
                    {
                        count++;
                        if (catalog.Find(source) == null)
                            throw new InvalidOperationException("Missing generated icon: " + AssetDatabase.GetAssetPath(source));
                    }
            Debug.Log("UI Toolkit asset validation passed. Icons cover all " + count + " game data assets.");
        }
    }
}
