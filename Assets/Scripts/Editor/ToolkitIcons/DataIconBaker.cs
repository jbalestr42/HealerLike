using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealerLike.UI.Toolkit.Icons;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HealerLike.UI.Toolkit.Editor
{
    /// <summary>Automatically discovers game data including future ScriptableObject data types.</summary>
    [InitializeOnLoad]
    public static class DataIconBaker
    {
        public const string OutputFolder = "Assets/Resources/UIToolkit/GeneratedIcons";
        public const string CatalogPath = "Assets/Resources/UIToolkit/DataIconCatalog.asset";
        const string RendererVersion = "3";
        static bool pending;
        static bool baking;
        static DataIconBaker() { Schedule(); }

        public static void Schedule()
        {
            if (pending || baking) return;
            pending = true;
            EditorApplication.delayCall += RunScheduled;
        }

        static void RunScheduled()
        {
            pending = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Bake(false);
        }

        [MenuItem("HealerLike/UI Toolkit/Rebuild All Data Icons")]
        public static void Rebuild() { Bake(true); }

        public static bool IsGameData(ScriptableObject asset)
        {
            if (!asset || asset is DataIconCatalog) return false;
            // Include future project-owned runtime assemblies without depending on them.
            // Editor/test assets and third-party configuration stay outside this catalog.
            string assemblyName = asset.GetType().Assembly.GetName().Name;
            if (assemblyName != "HealerLike" && !assemblyName.StartsWith("HealerLike.", StringComparison.Ordinal))
                return false;
            return !assemblyName.Split('.').Any(segment =>
                segment.Equals("Editor", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("Tests", StringComparison.OrdinalIgnoreCase));
        }

        public static void Bake(bool force)
        {
            if (baking) return;
            baking = true;
            try
            {
                Directory.CreateDirectory(OutputFolder);
                AssetDatabase.Refresh();
                var catalog = AssetDatabase.LoadAssetAtPath<DataIconCatalog>(CatalogPath);
                if (!catalog)
                {
                    catalog = ScriptableObject.CreateInstance<DataIconCatalog>();
                    AssetDatabase.CreateAsset(catalog, CatalogPath);
                }
                bool changed = false;
                var sources = new HashSet<UnityEngine.Object>();
                foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }).OrderBy(value => value))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.StartsWith("Assets/Resources/UIToolkit/", StringComparison.Ordinal)) continue;
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path).OfType<ScriptableObject>())
                    {
                        if (!IsGameData(asset)) continue;
                        sources.Add(asset);
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long localId);
                        string outputPath = OutputFolder + "/" + assetGuid + "_" + localId + ".png";
                        var descriptor = DataIconDescriptor.From(asset);
                        string fingerprint = RendererVersion + ":" + descriptor.Kind + ":" + descriptor.Key;
                        var entry = catalog.entries.Find(value => value != null && value.source == asset);
                        if (entry == null)
                        {
                            entry = new DataIconCatalog.Entry { source = asset };
                            catalog.entries.Add(entry);
                            changed = true;
                        }
                        if (!force && entry.generated && entry.fingerprint == fingerprint && File.Exists(outputPath)) continue;
                        Texture2D texture = ProceduralDataIcon.Create(descriptor);
                        try { File.WriteAllBytes(outputPath, texture.EncodeToPNG()); }
                        finally { UnityEngine.Object.DestroyImmediate(texture); }
                        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(outputPath);
                        importer.textureType = TextureImporterType.Default;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        importer.wrapMode = TextureWrapMode.Clamp;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                        entry.generated = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
                        entry.fingerprint = fingerprint;
                        changed = true;
                    }
                }
                changed |= catalog.entries.RemoveAll(entry => entry == null || !entry.source || !sources.Contains(entry.source)) > 0;
                if (changed) { EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog); }
                DataIconService.Clear();
            }
            finally { baking = false; }
        }
    }

    public sealed class DataIconImportWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool Relevant(string path) => path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("Assets/Resources/UIToolkit/", StringComparison.Ordinal);
            if (imported.Any(Relevant) || deleted.Any(Relevant) || moved.Any(Relevant) || movedFrom.Any(Relevant))
                DataIconBaker.Schedule();
        }
    }

    public sealed class DataIconBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) { DataIconBaker.Bake(false); }
    }
}
