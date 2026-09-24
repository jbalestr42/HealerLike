using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class ToolkitUiValidation
{
    // Usable from -executeMethod: a failed check ends batch mode with exit code 1
    public static void ValidateAssets()
    {
        if (!CheckAssets() && Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    public static bool CheckAssets()
    {
        return CheckLayouts() && CheckDemoScenes() && CheckIcons();
    }

    static bool CheckLayouts()
    {
        string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset", new string[] { "Assets/Resources/UI/Toolkit" });
        if (guids.Length == 0)
        {
            Debug.LogError("[ToolkitUiValidation] UI Toolkit layout assets are missing");
            return false;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (tree.CloneTree().childCount == 0)
            {
                Debug.LogError($"[ToolkitUiValidation] Empty UI layout: {path}");
                return false;
            }
        }

        return true;
    }

    static bool CheckDemoScenes()
    {
        foreach (string path in new string[] { ToolkitSceneNavigation.MenuPath, ToolkitSceneNavigation.GameplayPath })
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[ToolkitUiValidation] Missing demo scene: {path}");
                return false;
            }
        }

        return true;
    }

    static bool CheckIcons()
    {
        DataIconCatalog catalog = Resources.Load<DataIconCatalog>(DataIconService.CatalogResourcePath);
        if (catalog == null)
        {
            Debug.LogError("[ToolkitUiValidation] The generated icon catalog is missing");
            return false;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new string[] { "Assets" }))
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                ScriptableObject source = asset as ScriptableObject;
                if (DataIconBaker.IsGameData(source) && catalog.Find(source) == null)
                {
                    string path = AssetDatabase.GetAssetPath(source);
                    Debug.LogError($"[ToolkitUiValidation] Missing generated icon: {path}");
                    return false;
                }
            }
        }

        return true;
    }
}
