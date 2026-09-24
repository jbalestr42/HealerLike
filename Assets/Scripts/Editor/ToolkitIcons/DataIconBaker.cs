using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Finds the game data, future ScriptableObject data types included, and bakes one icon per asset
[InitializeOnLoad]
public static class DataIconBaker
{
    public static readonly string OutputFolder = "Assets/Resources/UIToolkit/GeneratedIcons";
    public static readonly string CatalogPath = "Assets/Resources/UIToolkit/DataIconCatalog.asset";
    public static readonly string CatalogFolder = "Assets/Resources/UIToolkit/";

    static readonly string rendererVersion = "3";

    static DataIconBaker()
    {
        Schedule();
    }

    // One pending bake at most, however many imports ask for it
    public static void Schedule()
    {
        EditorApplication.delayCall -= RunScheduled;
        EditorApplication.delayCall += RunScheduled;
    }

    static void RunScheduled()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Schedule();
            return;
        }

        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Bake(false);
        }
    }

    [MenuItem("Tools/UI Toolkit/Rebuild All Data Icons")]
    public static void Rebuild()
    {
        Bake(true);
    }

    // Includes future project runtime assemblies without depending on them.
    // Editor and test assets and third-party configuration stay out of the catalog
    public static bool IsGameData(ScriptableObject asset)
    {
        if (!asset || asset is DataIconCatalog)
        {
            return false;
        }

        string assemblyName = asset.GetType().Assembly.GetName().Name;
        if (assemblyName != "HealerLike" && !assemblyName.StartsWith("HealerLike.", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string segment in assemblyName.Split('.'))
        {
            if (segment.Equals("Editor", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("Tests", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    // The icons and the catalog it writes sit in the catalog folder, which the import watcher ignores, so a bake
    // never schedules another
    public static void Bake(bool force)
    {
        Directory.CreateDirectory(OutputFolder);
        AssetDatabase.Refresh();
        DataIconCatalog catalog = AssetDatabase.LoadAssetAtPath<DataIconCatalog>(CatalogPath);
        if (!catalog)
        {
            catalog = ScriptableObject.CreateInstance<DataIconCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        bool hasChanged = false;
        HashSet<UnityEngine.Object> sources = new HashSet<UnityEngine.Object>();
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new string[] { "Assets" });
        Array.Sort(guids);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(CatalogFolder, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                ScriptableObject data = asset as ScriptableObject;
                if (!IsGameData(data))
                {
                    continue;
                }

                sources.Add(data);
                hasChanged |= BakeIcon(catalog, data, force);
            }
        }

        hasChanged |= RemoveStaleEntries(catalog, sources);
        if (hasChanged)
        {
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }

    static bool BakeIcon(DataIconCatalog catalog, ScriptableObject data, bool force)
    {
        bool hasChanged = false;
        string assetGuid;
        long localId;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data, out assetGuid, out localId);
        string outputPath = OutputFolder + "/" + assetGuid + "_" + localId + ".png";
        DataIconDescriptor descriptor = DataIconDescriptor.From(data);
        string fingerprint = rendererVersion + ":" + descriptor.kind + ":" + descriptor.key;
        DataIconCatalogEntry entry = FindEntry(catalog, data);
        if (entry == null)
        {
            entry = new DataIconCatalogEntry();
            entry.source = data;
            catalog.entries.Add(entry);
            hasChanged = true;
        }

        if (!force && entry.generated && entry.fingerprint == fingerprint && File.Exists(outputPath))
        {
            return hasChanged;
        }

        Texture2D texture = ProceduralDataIcon.Create(descriptor);
        File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(outputPath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        entry.generated = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        entry.fingerprint = fingerprint;
        return true;
    }

    static DataIconCatalogEntry FindEntry(DataIconCatalog catalog, ScriptableObject data)
    {
        foreach (DataIconCatalogEntry entry in catalog.entries)
        {
            if (entry != null && entry.source == data)
            {
                return entry;
            }
        }

        return null;
    }

    static bool RemoveStaleEntries(DataIconCatalog catalog, HashSet<UnityEngine.Object> sources)
    {
        bool hasRemoved = false;
        for (int i = catalog.entries.Count - 1; i >= 0; i--)
        {
            DataIconCatalogEntry entry = catalog.entries[i];
            if (entry == null || !entry.source || !sources.Contains(entry.source))
            {
                catalog.entries.RemoveAt(i);
                hasRemoved = true;
            }
        }

        return hasRemoved;
    }
}
