using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor
{
    // The assets of a type in a folder, listed together in an AssetBrowserWindow, and how a new one is made
    public class AssetSection
    {
        public string title;
        public string folder;
        public Type assetType;
        // The assets of the sub-folders too
        public bool isRecursive = true;
        public bool canCreate = true;
        // A new asset goes in a folder of its own, named after it
        public bool createFolder = true;
        // A new asset is created empty, of a type derived from assetType picked in a menu, instead of from a draft
        public bool pickDerivedType;
        // File name of a new asset from its draft, the name of the draft when null
        public Func<ScriptableObject, string> getAssetName;
        // Fills each new draft (e.g. the tags every asset of the section has)
        public Action<ScriptableObject> initDraft;
        // The assets listed, all of them when null
        public Func<ScriptableObject, bool> filter;

        public AssetSection(string title, string folder, Type assetType)
        {
            this.title = title;
            this.folder = folder.TrimEnd('/');
            this.assetType = assetType;
        }

        // The assets of the section accepted by its filter, by file name
        public List<ScriptableObject> FindAssets()
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return new List<ScriptableObject>();
            }

            List<ScriptableObject> assets = new List<ScriptableObject>();
            foreach (string path in AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                if (!isRecursive && Path.GetDirectoryName(path).Replace('\\', '/') != folder)
                {
                    continue;
                }
                ScriptableObject asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
                if (asset != null && assetType.IsInstanceOfType(asset) && (filter == null || filter(asset)))
                {
                    assets.Add(asset);
                }
            }
            return assets.OrderBy(asset => asset.name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // A new draft of the section type, filled by initDraft
        public ScriptableObject CreateDraft()
        {
            ScriptableObject draft = ScriptableObject.CreateInstance(assetType);
            draft.name = DataAssets.GetNiceName(assetType);
            initDraft?.Invoke(draft);
            return draft;
        }

        // Where an asset of this name is saved: folder/name/name.asset, or folder/name.asset without its own folder
        public string GetSavePath(string assetName)
        {
            string fileName = assetName.Replace("/", " ").Trim();
            return createFolder ? $"{folder}/{fileName}/{fileName}.asset" : $"{folder}/{fileName}.asset";
        }

        // Saves the asset at the path of its name, a free one when it's taken
        public ScriptableObject Save(ScriptableObject asset, string assetName)
        {
            string path = GetSavePath(assetName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }
    }
}
