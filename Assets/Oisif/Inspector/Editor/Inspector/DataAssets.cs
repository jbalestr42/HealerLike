using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor
{
    // Creates and deletes the assets of the CreateDataButton fields
    public static class DataAssets
    {
        // The types an asset of the field type can be created with: itself when it can, and the types derived from
        // it, without the abstract and generic ones, by name
        public static List<Type> GetCreatableTypes(Type fieldType)
        {
            IEnumerable<Type> types = TypeCache.GetTypesDerivedFrom(fieldType).Append(fieldType);
            return types.Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && !type.ContainsGenericParameters && typeof(ScriptableObject).IsAssignableFrom(type))
                .Distinct()
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .ToList();
        }

        // "ApplyConsumerBuffFactory" -> "Apply Consumer Buff Factory"
        public static string GetNiceName(Type type)
        {
            return Regex.Replace(type.Name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        }

        // The folder of the asset being edited, of the selection without it, Assets at last
        public static string GetFolder(UnityEngine.Object host)
        {
            string path = host != null ? AssetDatabase.GetAssetPath(host) : "";
            if (string.IsNullOrEmpty(path) && Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
            }
            if (string.IsNullOrEmpty(path))
            {
                return "Assets";
            }
            return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path).Replace('\\', '/');
        }

        // A new asset of the type in the folder of the host, named after the type (prefixed when given), never
        // overwriting another one
        public static ScriptableObject Create(Type type, UnityEngine.Object host, string prefix = "")
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            string name = string.IsNullOrEmpty(prefix) ? type.Name : $"{prefix}_{type.Name}";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{GetFolder(host)}/{name}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        // Asks first: the asset may be used elsewhere
        public static bool Delete(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !EditorUtility.DisplayDialog("Delete asset", $"Delete {path}?", "Delete", "Cancel"))
            {
                return false;
            }
            return AssetDatabase.DeleteAsset(path);
        }

        // A menu of the creatable types, onPicked called with the one picked: under the button when given (the menu
        // of a dropdown button, even for a single type), under the mouse otherwise, a single type created at once
        public static void ShowTypeMenu(Type fieldType, Action<Type> onPicked, Rect? button = null)
        {
            List<Type> types = GetCreatableTypes(fieldType);
            if (types.Count == 1 && button == null)
            {
                onPicked(types[0]);
                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (Type type in types)
            {
                menu.AddItem(new GUIContent(GetNiceName(type)), false, () => onPicked(type));
            }
            if (types.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent($"No type to create for {fieldType.Name}"));
            }
            if (button != null)
            {
                menu.DropDown(button.Value);
            }
            else
            {
                menu.ShowAsContext();
            }
        }
    }
}
